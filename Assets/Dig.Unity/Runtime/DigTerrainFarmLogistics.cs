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
    private readonly FarmLogisticsReservations _farmLogisticsReservations =
        new FarmLogisticsReservations();
    private InMemoryJobCandidateProvider? _farmDeliveryCandidates;
    private AssignAvailableJobsHandler? _farmAssignment;
    private AcquireHaulingItemHandler? _farmAcquisition;
    private CompleteFarmDeliveryHandler? _farmDeliveryCompletion;
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

            ItemStackSnapshot? source =
                _inventoryRepository.Get().GetStack(hauling.SourceStackId);
            if (source?.Location.HasCell != true) continue;
            CellId sourceCell = source.Location.CellId;
            _farmDeliveryCandidates!.SetCandidates(
                job.Id,
                agents.Select((agent, index) => new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 4_000 - (index * 200),
                    distanceCost: Math.Abs(agent.CellX - sourceCell.X)
                        + Math.Abs(agent.CellY - sourceCell.Y)
                        + Math.Abs(agent.CellZ - sourceCell.Z),
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
            ItemStackSnapshot? source =
                _inventoryRepository.Get().GetStack(hauling.SourceStackId);
            return source?.Location.HasCell == true
                ? source.Location.CellId
                : (CellId?)null;
        }

        BuildingSnapshot? farm = _buildingsRepository?.Get().Get(
            hauling.Destination.OwnerId);
        return farm?.WorkPosition;
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
    }
}

}
