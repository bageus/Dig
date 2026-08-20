using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Rooms;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private const int RoomUpgradePriority = 600;
        private const int MaximumRoomDeliveryJobs = 8;
        private readonly RoomInfrastructureProvenanceProjector _roomProjector =
            new RoomInfrastructureProvenanceProjector();
        private readonly Dictionary<string, CompletedRoomInfrastructureProvenance>
            _roomProvenance =
                new Dictionary<string, CompletedRoomInfrastructureProvenance>(
                    StringComparer.Ordinal);
        private InMemoryRoomInfrastructureRepository? _roomInfrastructure;
        private SynchronizeCompletedRoomInfrastructureHandler? _roomCompletionSync;
        private SynchronizeRoomTemporaryStockCellHandler? _roomStockSync;
        private SynchronizeRoomUpgradeJobsHandler? _roomJobSync;
        private CompleteRoomUpgradeDeliveryHandler? _roomDeliveryCompletion;
        private CommitRoomUpgradeWorkIntervalHandler? _roomWorkInterval;
        private CompleteRoomUpgradeWorkHandler? _roomWorkCompletion;
        private CancelRoomUpgradeOperationHandler? _roomCancellation;
        private InMemoryJobCandidateProvider? _roomDeliveryCandidates;
        private AssignAvailableJobsHandler? _roomAssignment;
        private AcquireHaulingItemHandler? _roomDeliveryAcquisition;
        private HaulingResidentSlotClaimService? _roomDeliverySlotClaims;
        private ulong _roomRuntimeSequence = 1UL;

        internal Result SynchronizeRoomInfrastructureRuntime(
            long tick,
            IReadOnlyList<AgentViewModel> agents,
            IReadOnlyCollection<CellId> reachableCells)
        {
            if (agents == null || reachableCells == null)
            {
                throw new ArgumentNullException(
                    agents == null ? nameof(agents) : nameof(reachableCells));
            }

            EnsureRoomInfrastructureRuntime();
            Result merged = MergeCompletedRoomProvenance();
            if (merged.IsFailure)
            {
                return merged;
            }

            Result<RoomInfrastructureSynchronizationResult> synchronized =
                _roomCompletionSync!.Handle(
                    new SynchronizeCompletedRoomInfrastructureCommand(
                        _roomProvenance.Values,
                        tick));
            if (synchronized.IsFailure)
            {
                return Result.Failure(synchronized.Error!);
            }

            WorldSnapshot world = _worldSession.LoadSnapshot();
            CellId[] reachable = reachableCells.Distinct().OrderBy(value => value).ToArray();
            Result stock = SynchronizeRoomStockCells(world, reachable, agents, tick);
            if (stock.IsFailure)
            {
                return stock;
            }

            CellId[] revealed = world.Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => cell.State.IsExplored)
                .Select(cell => cell.Id)
                .OrderBy(cell => cell)
                .ToArray();
            Result<RoomUpgradeJobSynchronizationReport> jobs = _roomJobSync!.Handle(
                new SynchronizeRoomUpgradeJobsCommand(
                    revealed,
                    reachable,
                    RoomUpgradePriority,
                    MaximumRoomDeliveryJobs,
                    tick));
            if (jobs.IsFailure)
            {
                return Result.Failure(jobs.Error!);
            }

            SynchronizeRoomJobCandidates(agents, tick);
            return Result.Success();
        }

        internal RoomInfrastructureSnapshot LoadRoomInfrastructureRuntime()
        {
            EnsureRoomInfrastructureRuntime();
            return _roomInfrastructure!.Get().CaptureSnapshot();
        }

        private Result SynchronizeRoomStockCells(
            WorldSnapshot world,
            IReadOnlyCollection<CellId> reachable,
            IReadOnlyList<AgentViewModel> agents,
            long tick)
        {
            RoomInfrastructureSnapshot rooms =
                _roomInfrastructure!.Get().CaptureSnapshot();
            HashSet<CellId> stockCells = rooms.Rooms
                .Where(room => room.TemporaryStockCell.HasValue)
                .Select(room => room.TemporaryStockCell!.Value)
                .ToHashSet();
            HashSet<CellId> occupied = LoadBuildings()
                .Where(building => building.Status == BuildingStatus.Completed)
                .SelectMany(building => building.Footprint)
                .Select(cell => new CellId(cell.X, cell.Y, cell.Z))
                .Concat(_inventoryRepository.Get().CreateSnapshot().Stacks
                    .Where(stack => stack.Location.HasCell
                        && !stockCells.Contains(stack.Location.CellId))
                    .Select(stack => stack.Location.CellId))
                .Concat(agents.Select(agent =>
                    new CellId(agent.CellX, agent.CellY, agent.CellZ)))
                .ToHashSet();
            Dictionary<EntityId, CompletedRoomInfrastructureProvenance> sources =
                _roomProvenance.Values.ToDictionary(
                    value => value.RoomInfrastructureId);
            foreach (RoomInfrastructureProjectSnapshot room in rooms.Rooms
                .Where(value => value.UpgradeOrderCount == 1
                    && value.Status != RoomImprovementStatus.Improved
                    && !value.TemporaryStockCell.HasValue))
            {
                if (!sources.TryGetValue(
                        room.RoomInfrastructureId,
                        out CompletedRoomInfrastructureProvenance? source))
                {
                    return Result.Failure(
                        RoomInfrastructureApplicationErrors.ProvenanceIdentityConflict);
                }

                Result<RoomTemporaryStockCellPlan> planned = _roomStockSync!.Handle(
                    new SynchronizeRoomTemporaryStockCellCommand(
                        source,
                        world,
                        reachable,
                        occupied,
                        tick));
                if (planned.IsFailure)
                {
                    return Result.Failure(planned.Error!);
                }
            }

            return Result.Success();
        }

        private Result MergeCompletedRoomProvenance()
        {
            IReadOnlyList<CompletedRoomInfrastructureProvenance> projected =
                _roomProjector.Project(_templateInstances.Values);
            for (int index = 0; index < projected.Count; index++)
            {
                CompletedRoomInfrastructureProvenance source = projected[index];
                if (_roomProvenance.TryGetValue(
                        source.TemplateInstanceId,
                        out CompletedRoomInfrastructureProvenance? existing))
                {
                    if (existing.RoomInfrastructureId != source.RoomInfrastructureId
                        || existing.TemplateKind != source.TemplateKind
                        || !existing.OrderedRoomCells.SequenceEqual(
                            source.OrderedRoomCells))
                    {
                        return Result.Failure(
                            RoomInfrastructureApplicationErrors.ProvenanceIdentityConflict);
                    }

                    continue;
                }

                _roomProvenance.Add(source.TemplateInstanceId, source);
            }

            return Result.Success();
        }

        private void SynchronizeRoomJobCandidates(
            IReadOnlyList<AgentViewModel> agents,
            long tick)
        {
            if (_roomDeliveryCandidates == null || _roomAssignment == null)
            {
                throw new InvalidOperationException(
                    "Room assignment dependencies are not initialized.");
            }

            HashSet<EntityId> active = _roomInfrastructure!.Get()
                .CaptureSnapshot().Rooms
                .SelectMany(room => room.ActiveJobIds)
                .ToHashSet();
            foreach (JobSnapshot job in _jobRepository.Get().GetAll()
                .Where(job => active.Contains(job.Id) && !job.IsTerminal))
            {
                if (job.Definition is RoomUpgradeWorkJobDefinition work)
                {
                    _roomDeliveryCandidates.SetCandidates(
                        job.Id,
                        CreateDynamicCandidates(agents, work.WorkCell));
                }
                else if (job.Definition is HaulJobDefinition haul)
                {
                    ItemStackSnapshot? source =
                        _inventoryRepository.Get().GetStack(haul.SourceStackId);
                    if (source?.Location.HasCell == true)
                    {
                        _roomDeliveryCandidates.SetCandidates(
                            job.Id,
                            CreateRoomDeliveryCandidates(agents, source.Location.CellId));
                    }
                }
            }

            _roomAssignment.Handle(new AssignAvailableJobsCommand(tick));
        }

        private bool IsRoomUpgradeJob(EntityId jobId)
        {
            return _roomInfrastructure?.Get().GetByActiveJob(jobId) != null;
        }

        private EntityId NextRoomUpgradeJobId() =>
            RoomUpgradeRuntimeIdentity.CreateJobId(_roomRuntimeSequence++);

        private EntityId NextRoomTransitStackId() =>
            RoomUpgradeRuntimeIdentity.CreateTransitStackId(_roomRuntimeSequence++);

        private void EnsureRoomInfrastructureRuntime()
        {
            if (_roomInfrastructure == null)
            {
                _roomInfrastructure = new InMemoryRoomInfrastructureRepository();
            }

            ComposeRoomInfrastructureHandlers();
        }

        private void ComposeRoomInfrastructureHandlers()
        {
            _roomCompletionSync = new SynchronizeCompletedRoomInfrastructureHandler(
                _roomInfrastructure!, _journal);
            _roomStockSync = new SynchronizeRoomTemporaryStockCellHandler(
                _roomInfrastructure!, _journal);
            _roomJobSync = new SynchronizeRoomUpgradeJobsHandler(
                _roomInfrastructure!,
                _inventoryRepository,
                _jobRepository,
                new RuntimeRoomJobIds(this),
                _journal);
            _roomDeliveryCompletion = new CompleteRoomUpgradeDeliveryHandler(
                _roomInfrastructure!,
                _inventoryRepository,
                _jobRepository,
                _journal,
                _skillGrants);
            _roomWorkInterval = new CommitRoomUpgradeWorkIntervalHandler(
                _roomInfrastructure!,
                _inventoryRepository,
                _jobRepository,
                _journal,
                _skillGrants);
            _roomWorkCompletion = new CompleteRoomUpgradeWorkHandler(
                _roomInfrastructure!, _jobRepository, _journal);
            _roomCancellation = new CancelRoomUpgradeOperationHandler(
                _roomInfrastructure!,
                _inventoryRepository,
                _jobRepository,
                _journal);
            _roomDeliveryCandidates ??= new InMemoryJobCandidateProvider();
            _roomDeliverySlotClaims ??= new HaulingResidentSlotClaimService(
                _inventoryRepository,
                _journal);
            _roomDeliveryAcquisition ??= new AcquireHaulingItemHandler(
                _inventoryRepository,
                _jobRepository,
                _journal);
            _roomAssignment = new AssignAvailableJobsHandler(
                _jobRepository,
                new InventoryTravelCostJobCandidateProvider(
                    _roomDeliveryCandidates!,
                    _inventoryRepository),
                _journal,
                haulingResidentSlotClaims: _roomDeliverySlotClaims);
        }

        private static IReadOnlyList<JobCandidate> CreateRoomDeliveryCandidates(
            IReadOnlyList<AgentViewModel> agents,
            CellId source)
        {
            return agents.Select((agent, index) => new JobCandidate(
                EntityId.Parse(agent.Id),
                skillLevel: 4_000 - (index * 200),
                distanceCost: Math.Abs(agent.CellX - source.X)
                    + Math.Abs(agent.CellY - source.Y)
                    + Math.Abs(agent.CellZ - source.Z),
                isAvailable: agent.IsAvailableForAutomaticPlanning)).ToArray();
        }

        private sealed class RuntimeRoomJobIds : IRoomUpgradeJobIdSource
        {
            private readonly DigTerrainWorkSession _owner;

            public RuntimeRoomJobIds(DigTerrainWorkSession owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public EntityId NextJobId() => _owner.NextRoomUpgradeJobId();
        }
    }
}
