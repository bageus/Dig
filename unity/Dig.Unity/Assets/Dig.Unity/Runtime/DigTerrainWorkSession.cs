using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Navigation;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
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
    private const int DefaultExcavationPriority = 750;
    private readonly AdvanceJobHandler _advanceHandler;
    private readonly CompleteTerrainWorkCommandHandler _completionHandler;
    private readonly CompletePartialTerrainWorkCommandHandler _partialCompletionHandler;
    private readonly JobOverlayPresenter _jobPresenter;
    private readonly InMemoryExecutionJournal _journal;
    private readonly InventoryWorldPresenter _inventoryPresenter;
    private readonly NavigationRoutePresenter _routePresenter;
    private readonly InMemoryJobRepository _jobRepository;
    private readonly InMemoryInventoryRepository _inventoryRepository;
    private readonly InMemoryNavigationRepository _navigationRepository;
    private readonly DigWorldSession _worldSession;
    private readonly IAgentSkillGrantService _skillGrants;
    private readonly TerrainWorkRoutePlanner _routePlanner;
    private readonly UnsupportedResidentRecoveryPlanner _supportRecoveryPlanner;
    private readonly TraversalProfile _profile;
    private readonly Dictionary<EntityId, EntityId> _outputStackIds;
    private readonly MiningOutputResolver _miningOutputResolver = new MiningOutputResolver();
    private readonly MiningOutputCommitState _miningOutputCommits;
    private readonly Dictionary<EntityId, TerrainWorkRoutePlan> _routePlans =
        new Dictionary<EntityId, TerrainWorkRoutePlan>();
    private bool _worldChanged;

    private DigTerrainWorkSession(
        AdvanceJobHandler advanceHandler,
        CompleteTerrainWorkCommandHandler completionHandler,
        CompletePartialTerrainWorkCommandHandler partialCompletionHandler,
        JobOverlayPresenter jobPresenter,
        InMemoryExecutionJournal journal,
        InventoryWorldPresenter inventoryPresenter,
        NavigationRoutePresenter routePresenter,
        InMemoryJobRepository jobRepository,
        InMemoryInventoryRepository inventoryRepository,
        InMemoryNavigationRepository navigationRepository,
        DigWorldSession worldSession,
        TerrainWorkRoutePlanner routePlanner,
        UnsupportedResidentRecoveryPlanner supportRecoveryPlanner,
        TraversalProfile profile,
        Dictionary<EntityId, EntityId> outputStackIds,
        IAgentSkillGrantService skillGrants,
        MiningOutputCommitState? miningOutputCommits)
    {
        _advanceHandler = advanceHandler;
        _completionHandler = completionHandler;
        _partialCompletionHandler = partialCompletionHandler;
        _jobPresenter = jobPresenter;
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _inventoryPresenter = inventoryPresenter;
        _routePresenter = routePresenter;
        _jobRepository = jobRepository;
        _inventoryRepository = inventoryRepository;
        _navigationRepository = navigationRepository;
        _worldSession = worldSession;
        _routePlanner = routePlanner;
        _supportRecoveryPlanner = supportRecoveryPlanner;
        _profile = profile;
        _outputStackIds = outputStackIds;
        _skillGrants = skillGrants ?? throw new ArgumentNullException(nameof(skillGrants));
        _quarterCommitHandler = new CommitExcavationQuarterCommandHandler(worldSession.Repository, _skillGrants, _journal);
        _miningOutputCommits = miningOutputCommits ?? new MiningOutputCommitState();
    }

    internal MiningOutputCommitState MiningOutputCommits => _miningOutputCommits;

    internal bool HasWorldChanged => _worldChanged;

    internal void MarkAuthoritativeWorldChanged()
    {
        _worldChanged = true;
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

            if (job.Definition is MushroomChopJobDefinition
                || job.Definition is BarrelAttackJobDefinition)
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
                && agent.CellY == route.WorkCell.Value.Y
                && agent.CellZ == route.WorkCell.Value.Z)
            {
                result = AdvanceAtWorkCell(job, agent, route, tick);
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

    internal void ReconcileChangedTerrain(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        if (_worldChanged)
        {
            // Rebuild frontier jobs once after every resident has advanced. Doing this
            // from per-resident Advance calls used a one-agent candidate set and could
            // leave the remaining tunnel cells without a worker after the first dig.
            SynchronizeDesignations(tick, agents, DefaultExcavationPriority);
        }
    }

    public bool ConsumeWorldChanged()
    {
        bool changed = _worldChanged;
        _worldChanged = false;
        return changed;
    }

    private Result AdvanceAtWorkCell(
        JobSnapshot job,
        AgentViewModel agent,
        TerrainWorkRoutePlan route,
        long tick)
    {
        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.TravelToTarget)
        {
            return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Stage == JobStageKind.Finalize)
        {
            return CompleteTerrainJobAtWorkCell(job, tick);
        }

        if (job.Stage != JobStageKind.PerformWork)
        {
            return Result.Success();
        }

        DigJobDefinition definition = (DigJobDefinition)job.Definition;
        EntityId workerId = job.AssignedAgentId!.Value;
        CellId residentCell = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        bool quartersComplete = AdvanceExcavationQuarterWork(
            workerId,
            new ExcavationWorkTarget(
                definition.Target.CellId,
                definition.Target.CellId.Z),
            residentCell,
            route.Posture,
            tick);
        if (!quartersComplete)
        {
            return Result.Success();
        }

        Result advanced = _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
        if (advanced.IsFailure)
        {
            return advanced;
        }

        JobSnapshot? updated = _jobRepository.Get().Get(job.Id);
        if (updated == null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        return updated.Stage == JobStageKind.Finalize
            ? CompleteTerrainJobAtWorkCell(updated, tick)
            : Result.Success();
    }

    private Result CompleteTerrainJobAtWorkCell(JobSnapshot job, long tick)
    {
        DigJobDefinition terrainJob = (DigJobDefinition)job.Definition;
        CellId targetCell = terrainJob.Target.CellId;
        if (_worldSession.TryGetCaveRoomExcavationTarget(
                targetCell,
                out CaveRoomExcavationTarget roomTarget)
            && !roomTarget.IsFullCell)
        {
            return CompletePartialTerrainJobAtWorkCell(
                job,
                roomTarget,
                tick);
        }

        MiningOutputPlan output;
        try
        {
            output = _miningOutputResolver.Resolve(
                _worldSession.MiningOutputWorldSeed,
                _worldSession.MiningOutputGeneratorVersion,
                targetCell,
                _worldSession.ResolveTerrainMaterial(targetCell),
                _worldSession.TerrainDeposits);
        }
        catch (Exception error)
        {
            return Result.Failure(new DomainError(
                "terrain_work.mining_output_invalid",
                error.Message));
        }

        CompleteTerrainWorkCommand command = CompleteTerrainWorkCommand.FromPlan(
            job.Id,
            _outputStackIds[job.Id],
            output,
            _worldSession.EmptyMaterialId,
            tick);
        Result<TerrainWorkCompletionResult> completion = _completionHandler.Handle(command);
        if (completion.IsFailure)
        {
            return Result.Failure(completion.Error!);
        }

        // The terrain commit above is already authoritative. Mark the world dirty
        // before any derived navigation refresh so Presentation cannot miss the open
        // cell when navigation fails and the tick returns a recoverable warning.
        MarkAuthoritativeWorldChanged();
        CompleteExcavationQuarterTarget(targetCell);
        _routePlans.Remove(job.Id);
        MarkTemplateCellExcavated(targetCell);
        PublishTerrainCompletionEffects(job.Id, targetCell, tick, !output.IsEmpty);

        Result refresh = RefreshNavigation();
        return refresh.IsFailure ? refresh : Result.Success();
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
                isAvailable: agent.IsAvailableForAutomaticPlanning);
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
