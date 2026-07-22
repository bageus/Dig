using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Navigation;
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
    public static DigTerrainWorkSession CreateDemo(
        DigWorldSession worldSession,
        IReadOnlyList<AgentViewModel> agents,
        InMemoryExecutionJournal journal,
        IAgentSkillGrantService skillGrants)
    {
        if (worldSession == null || agents == null)
        {
            throw new ArgumentNullException(nameof(worldSession));
        }

        WorldCellViewModel[] targets = worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsDesignated)
            .OrderBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .ToArray();
        if (agents.Count == 0)
        {
            throw new InvalidOperationException(
                "The terrain work demo requires at least one resident.");
        }

        InMemoryJobRepository jobs = new InMemoryJobRepository();
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        Dictionary<EntityId, EntityId> outputStackIds =
            CreateInitialDigJobs(
                worldSession,
                targets,
                agents,
                jobs,
                candidates,
                journal);
        AssignInitialDigJobs(jobs, candidates, journal);
        AdvanceJobHandler advance = CreateAdvanceHandler(jobs, journal);
        InventoryState inventory = CreateDemoResidentInventory(
            worldSession.TerrainDepositDefinitions);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        TraversalProfile profile = TraversalProfile.CreateFreeMover();
        InMemoryNavigationRepository navigation = BuildNavigation(
            worldSession,
            profile);
        worldSession.DrainDirtyChunks();
        return Compose(
            worldSession,
            journal,
            skillGrants,
            jobs,
            advance,
            inventoryRepository,
            navigation,
            profile,
            outputStackIds);
    }

    private static Dictionary<EntityId, EntityId> CreateInitialDigJobs(
        DigWorldSession worldSession,
        IReadOnlyList<WorldCellViewModel> targets,
        IReadOnlyList<AgentViewModel> agents,
        InMemoryJobRepository jobs,
        InMemoryJobCandidateProvider candidates,
        InMemoryExecutionJournal journal)
    {
        Dictionary<EntityId, EntityId> outputIds =
            new Dictionary<EntityId, EntityId>();
        CreateDigJobHandler create = new CreateDigJobHandler(jobs, journal);
        for (int index = 0; index < targets.Count; index++)
        {
            EntityId jobId = EntityId.Parse(
                $"400000000000000000000000000000{index + 1:D2}");
            EntityId stackId = EntityId.Parse(
                $"500000000000000000000000000000{index + 1:D2}");
            WorldCellViewModel target = targets[index];
            CellId targetCell = new CellId(target.X, target.Y, target.Z);
            DigJobDefinition definition = new DigJobDefinition(
                jobId,
                new DigJobTarget(targetCell),
                DefaultExcavationPriority,
                createdTick: 0,
                new JobRetryPolicy(2, 3),
                skillGrantProfile: worldSession
                    .ResolveExcavationSkillGrantProfile(targetCell));
            Require(create.Handle(new CreateDigJobCommand(definition, makeAvailable: true)));
            outputIds.Add(jobId, stackId);
            candidates.SetCandidates(jobId, CreateCandidates(agents, target));
        }

        return outputIds;
    }

    private static void AssignInitialDigJobs(
        InMemoryJobRepository jobs,
        InMemoryJobCandidateProvider candidates,
        InMemoryExecutionJournal journal)
    {
        new AssignAvailableJobsHandler(
            jobs,
            candidates,
            journal,
            assignmentReportSink: journal)
            .Handle(new AssignAvailableJobsCommand(tick: 0));
    }

    private static AdvanceJobHandler CreateAdvanceHandler(
        InMemoryJobRepository jobs,
        InMemoryExecutionJournal journal)
    {
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

        return advance;
    }

    private static InMemoryNavigationRepository BuildNavigation(
        DigWorldSession worldSession,
        TraversalProfile profile)
    {
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

        return navigation;
    }

    private static DigTerrainWorkSession Compose(
        DigWorldSession world,
        InMemoryExecutionJournal journal,
        IAgentSkillGrantService skills,
        InMemoryJobRepository jobs,
        AdvanceJobHandler advance,
        InMemoryInventoryRepository inventory,
        InMemoryNavigationRepository navigation,
        TraversalProfile profile,
        Dictionary<EntityId, EntityId> outputIds)
    {
        return new DigTerrainWorkSession(
            advance,
            new CompleteTerrainWorkCommandHandler(
                jobs, world.Repository, inventory, journal, skills),
            new JobOverlayPresenter(
                new GetJobsHandler(jobs),
                new GetJobReservationsHandler(jobs)),
            journal,
            new InventoryWorldPresenter(
                new GetInventorySnapshotQueryHandler(inventory),
                WorldItemInteractionKind.Pickup),
            new NavigationRoutePresenter(),
            jobs,
            inventory,
            navigation,
            world,
            new TerrainWorkRoutePlanner(new NavigationPathfinder()),
            profile,
            outputIds,
            skills);
    }
}

}
