using System;
using System.Collections.Generic;
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
        BuildingSnapshot? farm = _buildingsRepository?.Get().Get(farmId);
        if (farm == null || snapshot == null)
        {
            return Result.Failure(FarmApplicationErrors.MissingFarm);
        }

        if (snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms <= 0)
        {
            return Result.Failure(FarmApplicationErrors.ProductUnavailable);
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
            farm.WorkPosition,
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
        long tick,
        out Result result)
    {
        result = Result.Success();
        if (!_farmMushroomHarvests.TryGetValue(job.Id, out EntityId farmId))
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
}

}
