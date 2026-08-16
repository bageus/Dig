using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private const int FarmMushroomRequiredSwings = 3;
    private const int FarmMushroomPriority = 900;
    private readonly Dictionary<EntityId, EntityId> _farmMushroomHarvests =
        new Dictionary<EntityId, EntityId>();
    private readonly Dictionary<EntityId, int> _farmMushroomSwings =
        new Dictionary<EntityId, int>();

    internal bool CanDirectHarvestFarmMushroom(
        string buildingId,
        CellId workerCell,
        out CellId workPosition)
    {
        workPosition = default;
        if (string.IsNullOrWhiteSpace(buildingId)) return false;
        EntityId farmId = EntityId.Parse(buildingId);
        BuildingSnapshot? building = _buildingsRepository?.Get().Get(farmId);
        FarmSnapshot? snapshot = LoadFarmSnapshot(buildingId);
        if (building == null
            || building.Definition.Id !=
                Dig.Domain.Content.WorkshopProductionContent.FarmBuildingId)
        {
            return false;
        }

        return snapshot != null
            && snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms > 0
            && TryResolveMushroomWorkPosition(
                building.Origin,
                workerCell,
                out workPosition);
    }

    internal Result StartFarmMushroomHarvest(
        string buildingId,
        EntityId workerId,
        CellId workerCell,
        long tick)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        EntityId farmId = EntityId.Parse(buildingId);
        FarmSnapshot? snapshot = LoadFarmSnapshot(buildingId);
        ReconcileFarmMushroomHarvests();
        BuildingSnapshot? farm = _buildingsRepository?.Get().Get(farmId);
        if (farm == null || snapshot == null)
        {
            return Result.Failure(FarmApplicationErrors.MissingFarm);
        }

        if (snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms <= 0)
        {
            return Result.Failure(FarmApplicationErrors.ProductUnavailable);
        }

        if (!CanDirectHarvestFarmMushroom(
                buildingId,
                workerCell,
                out CellId workPosition))
        {
            return Result.Failure(new DomainError(
                "farm.harvest_route_unavailable",
                "The selected resident cannot reach a farm mushroom."));
        }

        if (_farmMushroomHarvests.ContainsValue(farmId))
        {
            return Result.Failure(new DomainError(
                "farm.harvest_already_planned",
                "A mushroom harvest is already assigned to this farm."));
        }

        Result prepared = PrepareResidentsForDirectCommand(
            new[] { workerId.ToString() },
            tick);
        if (prepared.IsFailure) return prepared;
        EntityId jobId = NextFarmRuntimeId("job");
        MushroomChopJobDefinition definition = new MushroomChopJobDefinition(
            jobId,
            farmId,
            farm.Origin,
            workPosition,
            growthGeneration: 0,
            requiredSwings: FarmMushroomRequiredSwings,
            priority: FarmMushroomPriority,
            createdTick: tick,
            retryPolicy: JobRetryPolicy.Default);
        JobSystem jobs = _jobRepository.Get();
        Result added = jobs.Add(definition);
        Result available = added.IsSuccess ? jobs.MakeAvailable(jobId, tick) : added;
        Result claimed = available.IsSuccess ? jobs.Claim(jobId, workerId, tick) : available;
        Result started = claimed.IsSuccess ? jobs.Start(jobId, tick) : claimed;
        if (started.IsFailure)
        {
            if (jobs.Get(jobId)?.IsTerminal == false)
            {
                jobs.Cancel(
                    jobId,
                    new JobBlockReason("farm_harvest_start_failed", started.Error!.Message),
                    tick);
            }
            _jobRepository.Save(jobs);
            _journal.Append(jobs.DequeueUncommittedEvents());
            return started;
        }

        _farmMushroomHarvests.Add(jobId, farmId);
        _farmMushroomSwings.Add(jobId, 0);
        _jobRepository.Save(jobs);
        _journal.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private bool TryAdvanceFarmMushroomJob(
        JobSnapshot job,
        MushroomChopJobDefinition definition,
        long tick,
        out Result result)
    {
        result = Result.Success();
        if (!_farmMushroomHarvests.TryGetValue(job.Id, out EntityId farmId)
            && !TryRestoreFarmMushroomHarvest(job, definition, out farmId))
        {
            return false;
        }

        if (job.Stage == JobStageKind.TravelToTarget)
        {
            result = _arriveAtMushroom!.Handle(new Dig.Application.Ecology.ArriveAtMushroomCommand(
                job.Id,
                tick));
            return true;
        }

        if (job.Stage == JobStageKind.PerformWork)
        {
            int swings = _farmMushroomSwings[job.Id] + 1;
            _farmMushroomSwings[job.Id] = swings;
            if (swings >= FarmMushroomRequiredSwings)
            {
                result = _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }
            return true;
        }

        if (job.Stage == JobStageKind.Finalize)
        {
            result = HarvestFarmMushroom(farmId.ToString(), tick);
            if (result.IsSuccess)
            {
                result = _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }
            if (result.IsSuccess)
            {
                _farmMushroomHarvests.Remove(job.Id);
                _farmMushroomSwings.Remove(job.Id);
            }
            return true;
        }

        return true;
    }

    private Result CancelFarmMushroomHarvest(JobSnapshot job, long tick)
    {
        JobSystem jobs = _jobRepository.Get();
        Result cancelled = jobs.Cancel(
            job.Id,
            new JobBlockReason(
                "farm_harvest_direct_command_replaced",
                "Farm harvest was cancelled by a direct resident command."),
            tick);
        if (cancelled.IsFailure) return cancelled;
        _farmMushroomHarvests.Remove(job.Id);
        _farmMushroomSwings.Remove(job.Id);
        _jobRepository.Save(jobs);
        _journal.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private bool IsFarmMushroomHarvest(JobSnapshot job)
    {
        if (_farmMushroomHarvests.ContainsKey(job.Id)) return true;
        return job.Definition is MushroomChopJobDefinition definition
            && _farmRepository.Get(definition.SiteId) != null;
    }

    private bool TryRestoreFarmMushroomHarvest(
        JobSnapshot job,
        MushroomChopJobDefinition definition,
        out EntityId farmId)
    {
        farmId = definition.SiteId;
        if (_farmRepository.Get(farmId) == null || job.IsTerminal)
        {
            return false;
        }

        _farmMushroomHarvests[job.Id] = farmId;
        _farmMushroomSwings[job.Id] = 0;
        return true;
    }

    private void ReconcileFarmMushroomHarvests()
    {
        foreach (EntityId jobId in _farmMushroomHarvests.Keys.ToArray())
        {
            JobSnapshot? job = _jobRepository.Get().Get(jobId);
            EntityId farmId = _farmMushroomHarvests[jobId];
            if (job != null && !job.IsTerminal && _farmRepository.Get(farmId) != null)
            {
                continue;
            }

            _farmMushroomHarvests.Remove(jobId);
            _farmMushroomSwings.Remove(jobId);
        }

        foreach (JobSnapshot job in _jobRepository.Get().GetAll()
            .Where(value => !value.IsTerminal
                && value.Definition is MushroomChopJobDefinition))
        {
            if (_farmMushroomHarvests.ContainsKey(job.Id)) continue;
            MushroomChopJobDefinition definition =
                (MushroomChopJobDefinition)job.Definition;
            TryRestoreFarmMushroomHarvest(job, definition, out _);
        }
    }
}

}
