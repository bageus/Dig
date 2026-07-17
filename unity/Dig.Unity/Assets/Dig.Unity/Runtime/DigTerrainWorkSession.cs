using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Navigation;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using Dig.Presentation.Navigation;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private const int OutputQuantity = 12;
        private readonly AdvanceJobHandler _advanceHandler;
        private readonly CompleteTerrainWorkCommandHandler _completionHandler;
        private readonly JobOverlayPresenter _jobPresenter;
        private readonly InMemoryExecutionJournal _journal;
        private readonly InventoryWorldPresenter _inventoryPresenter;
        private readonly NavigationRoutePresenter _routePresenter;
        private readonly InMemoryJobRepository _jobRepository;
        private readonly InMemoryInventoryRepository _inventoryRepository;
        private readonly InMemoryNavigationRepository _navigationRepository;
        private readonly DigWorldSession _worldSession;
        private readonly TerrainWorkRoutePlanner _routePlanner;
        private readonly TraversalProfile _profile;
        private readonly ItemId _outputItemId;
        private readonly Dictionary<EntityId, EntityId> _outputStackIds;
        private readonly Dictionary<EntityId, TerrainWorkRoutePlan> _routePlans =
            new Dictionary<EntityId, TerrainWorkRoutePlan>();
        private bool _worldChanged;

        private DigTerrainWorkSession(
            AdvanceJobHandler advanceHandler,
            CompleteTerrainWorkCommandHandler completionHandler,
            JobOverlayPresenter jobPresenter,
            InMemoryExecutionJournal journal,
            InventoryWorldPresenter inventoryPresenter,
            NavigationRoutePresenter routePresenter,
            InMemoryJobRepository jobRepository,
            InMemoryInventoryRepository inventoryRepository,
            InMemoryNavigationRepository navigationRepository,
            DigWorldSession worldSession,
            TerrainWorkRoutePlanner routePlanner,
            TraversalProfile profile,
            ItemId outputItemId,
            Dictionary<EntityId, EntityId> outputStackIds)
        {
            _advanceHandler = advanceHandler;
            _completionHandler = completionHandler;
            _jobPresenter = jobPresenter;
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _inventoryPresenter = inventoryPresenter;
            _routePresenter = routePresenter;
            _jobRepository = jobRepository;
            _inventoryRepository = inventoryRepository;
            _navigationRepository = navigationRepository;
            _worldSession = worldSession;
            _routePlanner = routePlanner;
            _profile = profile;
            _outputItemId = outputItemId;
            _outputStackIds = outputStackIds;
        }

        public static DigTerrainWorkSession CreateDemo(
            DigWorldSession worldSession,
            IReadOnlyList<AgentViewModel> agents,
            InMemoryExecutionJournal journal)
        {
            if (worldSession == null)
            {
                throw new ArgumentNullException(nameof(worldSession));
            }

            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            WorldCellViewModel[] targets = worldSession.LoadView().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => cell.IsDesignated)
                .OrderBy(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .ToArray();
            if (targets.Length == 0 || agents.Count == 0)
            {
                throw new InvalidOperationException(
                    "The terrain work demo requires targets and residents.");
            }

            InMemoryJobRepository jobs = new InMemoryJobRepository();
            InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
            Dictionary<EntityId, EntityId> outputStackIds =
                new Dictionary<EntityId, EntityId>();
            CreateDigJobHandler create = new CreateDigJobHandler(jobs, journal);
            for (int index = 0; index < targets.Length; index++)
            {
                EntityId jobId = EntityId.Parse(
                    $"400000000000000000000000000000{index + 1:D2}");
                EntityId stackId = EntityId.Parse(
                    $"500000000000000000000000000000{index + 1:D2}");
                WorldCellViewModel target = targets[index];
                DigJobDefinition definition = new DigJobDefinition(
                    jobId,
                    new DigJobTarget(new Dig.Domain.World.CellId(target.X, target.Y)),
                    priority: 800 - (index * 50),
                    createdTick: 0,
                    new JobRetryPolicy(maximumRetries: 2, retryDelayTicks: 3));
                Require(create.Handle(new CreateDigJobCommand(
                    definition,
                    makeAvailable: true)));
                outputStackIds.Add(jobId, stackId);
                candidates.SetCandidates(jobId, CreateCandidates(agents, target));
            }

            new AssignAvailableJobsHandler(
                jobs,
                candidates,
                journal,
                assignmentReportSink: journal)
                .Handle(new AssignAvailableJobsCommand(tick: 0));
            AdvanceJobHandler advance = new AdvanceJobHandler(
                jobs,
                journal,
                new SuggestedToolJobExecutionReadinessPolicy(journal));
            foreach (JobSnapshot job in jobs.Get().GetAll())
            {
                if (job.Status == JobStatus.Claimed)
                {
                    Require(advance.Handle(new AdvanceJobCommand(job.Id, tick: 0)));
                }
            }

            ItemId outputItemId = new ItemId("demo.rock.chunk");
            InventoryState inventory = new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(
                    outputItemId,
                    "Rock chunk",
                    maximumStackSize: 100,
                    isTool: false,
                    new[] { new ItemCategoryId("raw.stone") }),
            }));
            InMemoryInventoryRepository inventoryRepository =
                new InMemoryInventoryRepository(inventory);
            TraversalProfile profile = TraversalProfile.CreateFreeMover();
            InMemoryNavigationRepository navigation = new InMemoryNavigationRepository();
            Result<NavigationUpdateDiagnostics> rebuild =
                new RebuildNavigationCommandHandler(navigation).Handle(
                    new RebuildNavigationCommand(
                        profile,
                        worldSession.LoadSnapshot(),
                        Array.Empty<TraversalLink>()));
            if (rebuild.IsFailure)
            {
                throw new InvalidOperationException(rebuild.Error!.ToString());
            }

            worldSession.DrainDirtyChunks();
            return new DigTerrainWorkSession(
                advance,
                new CompleteTerrainWorkCommandHandler(
                    jobs,
                    worldSession.Repository,
                    inventoryRepository,
                    journal),
                new JobOverlayPresenter(
                    new GetJobsHandler(jobs),
                    new GetJobReservationsHandler(jobs)),
                journal,
                new InventoryWorldPresenter(
                    new GetInventorySnapshotQueryHandler(inventoryRepository),
                    WorldItemInteractionKind.Pickup),
                new NavigationRoutePresenter(),
                jobs,
                inventoryRepository,
                navigation,
                worldSession,
                new TerrainWorkRoutePlanner(new NavigationPathfinder()),
                profile,
                outputItemId,
                outputStackIds);
        }

        public IReadOnlyList<JobOverlayViewModel> LoadJobs()
        {
            return _jobPresenter.LoadIndexed(_journal.JobAssignmentReports);
        }

        public IReadOnlyList<WorldItemViewModel> LoadItems()
        {
            return _inventoryPresenter.Load();
        }

        public Result Advance(long tick, IReadOnlyList<AgentViewModel> agents)
        {
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job)
                    || !job.AssignedAgentId.HasValue
                    || !agentsById.TryGetValue(
                        job.AssignedAgentId.Value.ToString(),
                        out AgentViewModel? agent))
                {
                    continue;
                }

                Result result;
                if (job.Definition is HaulJobDefinition)
                {
                    result = AdvanceHaulingAtTarget(job, agent, tick);
                }
                else if (_routePlans.TryGetValue(
                        job.Id,
                        out TerrainWorkRoutePlan? route)
                    && route.Succeeded
                    && route.WorkCell.HasValue
                    && agent.CellX == route.WorkCell.Value.X
                    && agent.CellY == route.WorkCell.Value.Y)
                {
                    result = AdvanceAtWorkCell(job, tick);
                }
                else
                {
                    continue;
                }

                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }

        public bool ConsumeWorldChanged()
        {
            bool changed = _worldChanged;
            _worldChanged = false;
            return changed;
        }

        private Result AdvanceAtWorkCell(JobSnapshot job, long tick)
        {
            if (job.Status == JobStatus.Claimed
                || job.Stage == JobStageKind.TravelToTarget)
            {
                return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }

            if (tick % 3 != 0)
            {
                return Result.Success();
            }

            if (job.Stage == JobStageKind.PerformWork)
            {
                return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }

            if (job.Stage != JobStageKind.Finalize)
            {
                return Result.Success();
            }

            Result<TerrainWorkCompletionResult> completion = _completionHandler.Handle(
                new CompleteTerrainWorkCommand(
                    job.Id,
                    _outputStackIds[job.Id],
                    _outputItemId,
                    OutputQuantity,
                    _worldSession.EmptyMaterialId,
                    tick));
            if (completion.IsFailure)
            {
                return Result.Failure(completion.Error!);
            }

            _routePlans.Remove(job.Id);
            Result refresh = RefreshNavigation();
            if (refresh.IsFailure)
            {
                return refresh;
            }

            _worldChanged = true;
            return Result.Success();
        }

        private static bool IsActive(JobSnapshot job)
        {
            return job.Status == JobStatus.Claimed
                || job.Status == JobStatus.InProgress;
        }

        private static IReadOnlyList<JobCandidate> CreateCandidates(
            IReadOnlyList<AgentViewModel> agents,
            WorldCellViewModel target)
        {
            JobCandidate[] values = new JobCandidate[agents.Count];
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                int distance = Math.Abs(agent.CellX - target.X)
                    + Math.Abs(agent.CellY - target.Y);
                values[index] = new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 5_000 - (index * 250),
                    distanceCost: distance,
                    isAvailable: agent.IsAlive);
            }

            return values;
        }

        private static void Require(Result result)
        {
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error!.ToString());
            }
        }
    }
}
