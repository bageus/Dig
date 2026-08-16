using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Farming;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private const int FarmLogisticsPriority = 650;
    private const int MaximumFarmDeliveryJobs = 8;
    private readonly FarmLogisticsReservations _farmLogisticsReservations;

    internal FarmLogisticsReservations FarmLogisticsReservations =>
        _farmLogisticsReservations;
    private InMemoryJobCandidateProvider? _farmDeliveryCandidates;
    private AssignAvailableJobsHandler? _farmAssignment;
    private AcquireHaulingItemHandler? _farmAcquisition;
    private CompleteFarmDeliveryHandler? _farmDeliveryCompletion;
    private CompleteFarmOutputHandler? _farmOutputCompletion;
    private HaulingResidentSlotClaimService? _farmSlotClaims;
    private ulong _farmRuntimeSequence = 1UL;

    private Result SynchronizeFarmLogisticsRuntime(
        long tick,
        IReadOnlyList<AgentViewModel> agents,
        IReadOnlyCollection<CellId> reachableCells)
    {
        if (agents == null || reachableCells == null)
        {
            throw new ArgumentNullException(
                agents == null ? nameof(agents) : nameof(reachableCells));
        }

        EnsureFarmLogisticsRuntime();
        Result<FarmLogisticsSynchronizationReport> synchronized =
            new SynchronizeFarmLogisticsHandler(
                _farmRepository,
                _inventoryRepository,
                _jobRepository,
                _farmItems,
                _farmLogisticsReservations,
                new RuntimeFarmJobIds(this),
                _journal).Handle(new SynchronizeFarmLogisticsCommand(
                    reachableCells,
                    FarmLogisticsPriority,
                    MaximumFarmDeliveryJobs,
                    tick));
        if (synchronized.IsFailure)
        {
            return Result.Failure(synchronized.Error!);
        }

        FarmLogisticsSite[] sites = LoadFarmLogisticsSites();
        Result<FarmLogisticsSynchronizationReport> outputs =
            new SynchronizeFarmOutputsHandler(
                _farmRepository,
                _inventoryRepository,
                _jobRepository,
                _farmItems,
                _farmLogisticsReservations,
                new RuntimeFarmJobIds(this),
                _journal).Handle(new SynchronizeFarmOutputsCommand(
                    sites,
                    FarmLogisticsPriority,
                    MaximumFarmDeliveryJobs,
                    tick));
        if (outputs.IsFailure)
        {
            return Result.Failure(outputs.Error!);
        }

        SynchronizeFarmCandidates(agents, tick);
        return Result.Success();
    }

    private void SynchronizeFarmCandidates(
        IReadOnlyList<AgentViewModel> agents,
        long tick)
    {
        foreach (FarmLogisticsReservation reservation in
            _farmLogisticsReservations.GetAll())
        {
            JobSnapshot? job = _jobRepository.Get().Get(reservation.JobId);
            if (job?.Definition is not HaulJobDefinition hauling || job.IsTerminal)
            {
                continue;
            }

            CellId? sourceCell = ResolveFarmLogisticsSourceCell(job, hauling);
            if (!sourceCell.HasValue) continue;
            _farmDeliveryCandidates!.SetCandidates(
                job.Id,
                agents.Select((agent, index) => new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 4_000 - (index * 200),
                    distanceCost: Math.Abs(agent.CellX - sourceCell.Value.X)
                        + Math.Abs(agent.CellY - sourceCell.Value.Y)
                        + Math.Abs(agent.CellZ - sourceCell.Value.Z),
                    isAvailable: agent.IsAvailableForAutomaticPlanning)).ToArray());
        }

        _farmAssignment!.Handle(new AssignAvailableJobsCommand(tick));
    }

    private void EnsureFarmLogisticsRuntime()
    {
        _farmDeliveryCandidates ??= new InMemoryJobCandidateProvider();
        _farmSlotClaims ??= new HaulingResidentSlotClaimService(
            _inventoryRepository, _journal);
        _farmAcquisition ??= new AcquireHaulingItemHandler(
            _inventoryRepository, _jobRepository, _journal);
        _farmDeliveryCompletion = new CompleteFarmDeliveryHandler(
            _farmRepository,
            _inventoryRepository,
            _jobRepository,
            _farmItems,
            _farmLogisticsReservations,
            _journal);
        _farmOutputCompletion = new CompleteFarmOutputHandler(
            _inventoryRepository,
            _jobRepository,
            _farmLogisticsReservations,
            _journal);
        _farmAssignment = new AssignAvailableJobsHandler(
            _jobRepository,
            new InventoryTravelCostJobCandidateProvider(
                _farmDeliveryCandidates,
                _inventoryRepository),
            _journal,
            haulingResidentSlotClaims: _farmSlotClaims);
    }

    private bool IsFarmLogisticsJob(EntityId jobId) =>
        _farmLogisticsReservations.TryGet(jobId, out _);

    private CellId? ResolveFarmLogisticsDestination(JobSnapshot job)
    {
        if (!IsFarmLogisticsJob(job.Id)
            || job.Definition is not HaulJobDefinition hauling)
        {
            return null;
        }

        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.AcquireItem)
        {
            return ResolveFarmLogisticsSourceCell(job, hauling);
        }

        if (hauling.Destination.HasCell) return hauling.Destination.CellId;
        BuildingSnapshot? farm = _buildingsRepository?.Get().Get(
            hauling.Destination.OwnerId);
        return farm?.WorkPosition;
    }

    private CellId? ResolveFarmLogisticsSourceCell(
        JobSnapshot job,
        HaulJobDefinition hauling)
    {
        ItemStackSnapshot? source =
            _inventoryRepository.Get().GetStack(hauling.SourceStackId);
        if (source?.Location.HasCell == true) return source.Location.CellId;
        if (!_farmLogisticsReservations.TryGet(
                job.Id,
                out FarmLogisticsReservation reservation))
        {
            return null;
        }

        return _buildingsRepository?.Get().Get(reservation.BuildingId)?.WorkPosition;
    }

    private FarmLogisticsSite[] LoadFarmLogisticsSites()
    {
        if (_buildingsRepository == null) return Array.Empty<FarmLogisticsSite>();
        return _buildingsRepository.Get().GetAll()
            .Where(value => value.Status == BuildingStatus.Completed
                && value.Definition.Id == WorkshopProductionContent.FarmBuildingId)
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .Select(value => new FarmLogisticsSite(
                value.Id,
                value.WorkPosition,
                value.Origin))
            .ToArray();
    }

    private Result MaterializeReleasedFarmFeed(
        EntityId farmId,
        int quantity,
        long tick)
    {
        if (quantity <= 0) return Result.Success();
        BuildingSnapshot? farm = _buildingsRepository?.Get().Get(farmId);
        if (farm == null) return Result.Failure(FarmApplicationErrors.MissingFarm);
        InventoryState inventory = _inventoryRepository.Get();
        Result added = inventory.AddStack(
            NextFarmRuntimeId("stack"),
            _farmItems.MushroomCap,
            quantity,
            ItemLocation.InWorld(farm.Origin),
            tick);
        if (added.IsFailure) return added;
        _inventoryRepository.Save(inventory);
        _journal.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }

    private Result MaterializeEscapedFarmAnimals(
        EntityId farmId,
        FarmAdvanceResult advance,
        long tick)
    {
        int total = checked(advance.HamstersEscaped + advance.GrubsEscaped);
        if (total <= 0) return Result.Success();
        BuildingSnapshot? farm = _buildingsRepository?.Get().Get(farmId);
        if (farm == null) return Result.Failure(FarmApplicationErrors.MissingFarm);
        InventoryState inventory = _inventoryRepository.Get();
        for (int index = 0; index < advance.HamstersEscaped; index++)
        {
            Result added = inventory.AddUnit(
                NextFarmRuntimeId("stack"),
                _farmItems.Hamster,
                ItemLocation.InWorld(farm.Origin),
                tick);
            if (added.IsFailure) return added;
        }

        for (int index = 0; index < advance.GrubsEscaped; index++)
        {
            Result added = inventory.AddUnit(
                NextFarmRuntimeId("stack"),
                _farmItems.Grub,
                ItemLocation.InWorld(farm.Origin),
                tick);
            if (added.IsFailure) return added;
        }

        _inventoryRepository.Save(inventory);
        _journal.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }

    private EntityId NextFarmRuntimeId(string family)
    {
        string suffix = (_farmRuntimeSequence++).ToString("x16");
        string prefix = family == "job" ? "7310000000000000" : "7320000000000000";
        return EntityId.Parse(prefix + suffix);
    }

    private sealed class RuntimeFarmJobIds : IFarmLogisticsJobIdSource
    {
        private readonly DigTerrainWorkSession _owner;

        public RuntimeFarmJobIds(DigTerrainWorkSession owner)
        {
            _owner = owner;
        }

        public EntityId NextJobId() => _owner.NextFarmRuntimeId("job");

        public EntityId NextStackId() => _owner.NextFarmRuntimeId("stack");
    }
}

}
