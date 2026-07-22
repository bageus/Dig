using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Navigation;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly Dictionary<EntityId, HaulingRoutePlan> _haulingRoutes =
            new Dictionary<EntityId, HaulingRoutePlan>();
        private InMemoryStorageRepository? _storageRepository;
        private PlanHaulingHandler? _haulingPlanner;
        private CompleteHaulingJobHandler? _haulingCompletion;
        private AssignAvailableJobsHandler? _haulingAssignment;
        private InMemoryJobCandidateProvider? _haulingCandidates;
        private DemoHaulingJobIdSource? _haulingIds;
        private NavigationPathfinder? _haulingPathfinder;
        private MoveStorageZoneHandler? _moveStorageZone;
        private EntityId _storageId;

        internal void InitializeHauling(InMemoryExecutionJournal journal)
        {
            if (journal == null)
            {
                throw new ArgumentNullException(nameof(journal));
            }

            _storageId = EntityId.Parse("60000000000000000000000000000001");
            CellId storageCell = SelectStorageCell();
            ItemId[] acceptedOutputs = _worldSession.TerrainDepositDefinitions
                .Select(value => value.OutputItemId)
                .Distinct()
                .ToArray();
            StorageState storage = new StorageState();
            Require(storage.AddZone(new StorageZoneDefinition(
                _storageId,
                "Demo stone stockpile",
                priority: 900,
                capacity: 500,
                new StorageFilter(
                    acceptsAll: false,
                    allowedItems: acceptedOutputs),
                storageCell)));
            _storageRepository = new InMemoryStorageRepository(storage);
            _haulingIds = new DemoHaulingJobIdSource();
            _haulingCandidates = new InMemoryJobCandidateProvider();
            _haulingPlanner = new PlanHaulingHandler(
                _inventoryRepository,
                _storageRepository,
                _jobRepository,
                _haulingIds,
                journal);
            _haulingCompletion = new CompleteHaulingJobHandler(
                _inventoryRepository,
                _storageRepository,
                _jobRepository,
                journal,
                _skillGrants);
            _haulingAssignment = CreateHaulingAssignment(journal);
            _haulingPathfinder = new NavigationPathfinder();
            _moveStorageZone = new MoveStorageZoneHandler(
                _storageRepository,
                _inventoryRepository,
                _worldSession.Repository,
                journal);
        }

        internal Result MoveStorageZone(CellId cell, long tick)
        {
            EnsureHaulingInitialized();
            Result result = _moveStorageZone!.Handle(
                new MoveStorageZoneCommand(_storageId, cell, tick));
            if (result.IsSuccess)
            {
                _haulingRoutes.Clear();
            }

            return result;
        }

        public void SynchronizeHauling(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureHaulingInitialized();
            HaulingPlanningReport report = _haulingPlanner!.Handle(
                new PlanHaulingCommand(maximumJobs: 4, priority: 600, tick));
            foreach (PlannedHaulingJob planned in report.Created)
            {
                ItemStackSnapshot? stack = _inventoryRepository.Get().GetStack(planned.StackId);
                if (stack == null || !stack.Location.HasCell)
                {
                    continue;
                }

                _haulingCandidates!.SetCandidates(
                    planned.JobId,
                    CreateHaulingCandidates(agents, stack.Location.CellId));
            }

            _haulingAssignment!.Handle(new AssignAvailableJobsCommand(tick));
        }

        internal bool TryPlanHaulingMovement(
            JobSnapshot job,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            if (job.Definition is not HaulJobDefinition hauling)
            {
                return false;
            }

            EnsureHaulingInitialized();
            CellId? target = ResolveHaulingTarget(job, hauling);
            if (!target.HasValue)
            {
                return true;
            }

            CellId start = new CellId(agent.CellX, agent.CellY);
            PathResult path = _haulingPathfinder!.FindPath(
                navigation,
                new PathRequest(start, target.Value, navigation.NavigationVersion));
            _haulingRoutes[job.Id] = new HaulingRoutePlan(target.Value, path);
            if (path.Succeeded)
            {
                movement[agent.Id] = path.Path!.Cells.Count > 1
                    ? path.Path.Cells[1]
                    : target.Value;
            }

            return true;
        }

        internal Result AdvanceHaulingAtTarget(
            JobSnapshot job,
            AgentViewModel agent,
            long tick)
        {
            if (job.Definition is not HaulJobDefinition hauling)
            {
                return Result.Success();
            }

            CellId? target = ResolveHaulingTarget(job, hauling);
            if (!target.HasValue
                || agent.CellX != target.Value.X
                || agent.CellY != target.Value.Y)
            {
                return Result.Success();
            }

            if (job.Status == JobStatus.Claimed
                || job.Stage == JobStageKind.AcquireItem
                || job.Stage == JobStageKind.TravelToDestination)
            {
                return AdvanceHaulingTransitAtTarget(job, tick);
            }

            if (job.Stage != JobStageKind.DepositItem)
            {
                return Result.Success();
            }

            EnsureHaulingInitialized();
            Result completed = _haulingCompletion!.Handle(
                new CompleteHaulingJobCommand(
                    job.Id,
                    _haulingIds!.NextSplitStackId(),
                    tick));
            if (completed.IsSuccess)
            {
                _haulingRoutes.Remove(job.Id);
            }

            return completed;
        }

        internal IReadOnlyList<RouteViewModel> LoadHaulingRoutes()
        {
            List<RouteViewModel> routes = new List<RouteViewModel>();
            foreach (KeyValuePair<EntityId, HaulingRoutePlan> pair in _haulingRoutes
                .OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
            {
                JobSnapshot? job = _jobRepository.Get().Get(pair.Key);
                if (job == null || !job.AssignedAgentId.HasValue)
                {
                    continue;
                }

                PathResult path = pair.Value.Path;
                RouteCellViewModel[] cells = path.Path == null
                    ? Array.Empty<RouteCellViewModel>()
                    : path.Path.Cells
                        .Select(cell => new RouteCellViewModel(cell.X, cell.Y))
                        .ToArray();
                routes.Add(new RouteViewModel(
                    pair.Key.ToString(),
                    job.AssignedAgentId.Value.ToString(),
                    pair.Value.Target.X,
                    pair.Value.Target.Y,
                    path.Succeeded,
                    "Hauling: " + path.Diagnostics.Detail,
                    path.Path?.TotalCost ?? 0,
                    path.Diagnostics.SnapshotVersion,
                    cells));
            }

            return routes;
        }

        public DigStorageStatus GetStorageStatus()
        {
            EnsureHaulingInitialized();
            StorageZoneDefinition zone = _storageRepository!.Get().GetZone(_storageId)
                ?? throw new InvalidOperationException("Demo stockpile is missing.");
            int stored = _inventoryRepository.Get().GetTotalQuantityAt(
                ItemLocation.InStorage(_storageId));
            int reserved = _storageRepository.Get().GetReservations()
                .Where(value => value.ZoneId == _storageId)
                .Sum(value => value.Quantity);
            return new DigStorageStatus(zone.Cell, stored, reserved, zone.Capacity);
        }

        private CellId? ResolveHaulingTarget(JobSnapshot job, HaulJobDefinition hauling)
        {
            if (job.Status != JobStatus.Claimed
                && job.Stage != JobStageKind.AcquireItem)
            {
                return _storageRepository!.Get().GetZone(_storageId)?.Cell;
            }

            ItemStackSnapshot? stack = _inventoryRepository.Get().GetStack(
                hauling.SourceStackId);
            return stack?.Location.HasCell == true ? stack.Location.CellId : null;
        }

        private CellId SelectStorageCell()
        {
            WorldCellViewModel cell = _worldSession.LoadView().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(value => !value.IsSolid && value.X > 0 && value.Y > 0)
                .OrderBy(value => value.X + value.Y)
                .ThenBy(value => value.Y)
                .ThenBy(value => value.X)
                .First();
            return new CellId(cell.X, cell.Y);
        }

        private static IReadOnlyList<JobCandidate> CreateHaulingCandidates(
            IReadOnlyList<AgentViewModel> agents,
            CellId source)
        {
            JobCandidate[] result = new JobCandidate[agents.Count];
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                int distance = Math.Abs(agent.CellX - source.X)
                    + Math.Abs(agent.CellY - source.Y);
                result[index] = new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 4_000 - (index * 200),
                    distanceCost: distance,
                    isAvailable: agent.IsAvailableForAutomaticPlanning);
            }

            return result;
        }

        private void EnsureHaulingInitialized()
        {
            if (_storageRepository == null
                || _haulingPlanner == null
                || _haulingCompletion == null
                || _haulingAssignment == null
                || _haulingCandidates == null
                || _haulingIds == null
                || _haulingPathfinder == null
                || _moveStorageZone == null)
            {
                throw new InvalidOperationException("Hauling is not initialized.");
            }
        }

    }
}
