using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Ecology;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class MushroomChopApplicationTests
{
    private static readonly MushroomDefinitionId DefinitionId =
        new MushroomDefinitionId("ecology.mushroom.common");
    private static readonly ItemId Cap = new ItemId("material.mushroom_cap");
    private static readonly ItemId Leg = new ItemId("material.mushroom_leg");
    private static readonly EntityId SiteId = Id("b1000000000000000000000000000001");
    private static readonly EntityId FirstJobId = Id("b2000000000000000000000000000001");
    private static readonly EntityId SecondJobId = Id("b2000000000000000000000000000002");
    private static readonly EntityId FirstWorkerId = Id("b3000000000000000000000000000001");
    private static readonly EntityId SecondWorkerId = Id("b3000000000000000000000000000002");
    private static readonly CellId Target = new CellId(5, 6, 0);
    private static readonly CellId Work = new CellId(4, 6, 0);

    [Fact]
    public void Direct_workflow_creates_exact_units_and_grants_woodworking_once()
    {
        Harness harness = CreateHarness(MushroomStage.Large, firstRequiredSwings: 2);
        Result<MushroomChopStartedResult> started = harness.Start.Handle(
            new StartDirectMushroomChopCommand(
                FirstJobId,
                SiteId,
                FirstWorkerId,
                Work,
                priority: 900,
                tick: 1));
        Assert.True(started.IsSuccess, started.Error?.ToString());
        Assert.Equal(2, started.Value.RequiredSwings);
        Assert.True(harness.Arrive.Handle(new ArriveAtMushroomCommand(FirstJobId, 2)).IsSuccess);
        Assert.False(harness.Swing.Handle(new CompleteMushroomSwingCommand(FirstJobId, 3)).Value);
        Assert.True(harness.Swing.Handle(new CompleteMushroomSwingCommand(FirstJobId, 4)).Value);

        Result<MushroomChopCompletionResult> completed = harness.Complete.Handle(
            new CompleteMushroomChopCommand(
                FirstJobId,
                Id("b4000000000000000000000000000001"),
                tick: 5));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(MushroomStage.Large, completed.Value.ChoppedStage);
        Assert.Equal(2, completed.Value.CapUnitIds.Count);
        Assert.Single(completed.Value.LegUnitIds);
        Assert.All(completed.Value.CapUnitIds, id =>
        {
            ItemStackSnapshot unit = harness.Inventory.GetStack(id)!;
            Assert.Equal(Cap, unit.ItemId);
            Assert.Equal(1, unit.Quantity);
            Assert.Equal(ItemLocation.InWorld(Target), unit.Location);
        });
        Assert.Equal(Leg, harness.Inventory.GetStack(completed.Value.LegUnitIds.Single())!.ItemId);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Equal(MushroomStage.AbsentRegrowing, harness.Mushrooms.Get(SiteId)!.Stage);
        Assert.Equal(MushroomDefinition.WoodworkingGrantUnits, harness.Agents
            .Get(FirstWorkerId)!
            .CreateSkillProgressionSnapshot()
            .GetLevel(AgentSkillCatalog.Woodworking));

        Result<MushroomChopCompletionResult> duplicate = harness.Complete.Handle(
            new CompleteMushroomChopCommand(
                FirstJobId,
                Id("b4000000000000000000000000000011"),
                tick: 6));
        Assert.True(duplicate.IsFailure);
        Assert.Equal(3, harness.Inventory.CreateSnapshot().Stacks.Count);
        Assert.Equal(MushroomDefinition.WoodworkingGrantUnits, harness.Agents
            .Get(FirstWorkerId)!
            .CreateSkillProgressionSnapshot()
            .GetLevel(AgentSkillCatalog.Woodworking));
    }

    [Fact]
    public void Second_direct_order_cancels_first_job_and_resets_progress()
    {
        Harness harness = CreateHarness(
            MushroomStage.Medium,
            firstRequiredSwings: 5,
            secondRequiredSwings: 3);
        Assert.True(harness.Start.Handle(new StartDirectMushroomChopCommand(
            FirstJobId,
            SiteId,
            FirstWorkerId,
            Work,
            900,
            1)).IsSuccess);
        Assert.True(harness.Arrive.Handle(new ArriveAtMushroomCommand(FirstJobId, 2)).IsSuccess);
        Assert.False(harness.Swing.Handle(new CompleteMushroomSwingCommand(FirstJobId, 3)).Value);
        Assert.Equal(1, harness.Mushrooms.Get(SiteId)!.CompletedSwings);

        Result<MushroomChopStartedResult> replacement = harness.Start.Handle(
            new StartDirectMushroomChopCommand(
                SecondJobId,
                SiteId,
                SecondWorkerId,
                new CellId(6, 6, 0),
                900,
                4));

        Assert.True(replacement.IsSuccess, replacement.Error?.ToString());
        Assert.Equal(FirstJobId, replacement.Value.ReplacedJobId);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Equal(JobStatus.InProgress, harness.Jobs.Get(SecondJobId)!.Status);
        MushroomSiteSnapshot site = harness.Mushrooms.Get(SiteId)!;
        Assert.Equal(0, site.CompletedSwings);
        Assert.Equal(3, site.RequiredSwings);
        Assert.Equal(SecondWorkerId, site.ActiveWorkerId);
        Assert.DoesNotContain(
            harness.Jobs.GetReservations(),
            reservation => reservation.JobId == FirstJobId);
    }

    [Fact]
    public void Cancel_releases_target_and_resumes_remaining_growth_duration()
    {
        Harness harness = CreateHarness(MushroomStage.Tiny, firstRequiredSwings: 6);
        Assert.True(harness.Start.Handle(new StartDirectMushroomChopCommand(
            FirstJobId,
            SiteId,
            FirstWorkerId,
            Work,
            900,
            4)).IsSuccess);
        Assert.True(harness.Grow.Handle(new AdvanceMushroomGrowthCommand(100)).IsSuccess);
        Assert.Equal(MushroomStage.Tiny, harness.Mushrooms.Get(SiteId)!.Stage);

        Assert.True(harness.Cancel.Handle(new CancelMushroomChopCommand(
            FirstJobId,
            "player_cancelled",
            tick: 20)).IsSuccess);

        Assert.Equal(26, harness.Mushrooms.Get(SiteId)!.NextStageTick);
        Assert.True(harness.Grow.Handle(new AdvanceMushroomGrowthCommand(25)).IsSuccess);
        Assert.Equal(MushroomStage.Tiny, harness.Mushrooms.Get(SiteId)!.Stage);
        Assert.True(harness.Grow.Handle(new AdvanceMushroomGrowthCommand(26)).IsSuccess);
        Assert.Equal(MushroomStage.Small, harness.Mushrooms.Get(SiteId)!.Stage);
    }

    private static Harness CreateHarness(
        MushroomStage stage,
        int firstRequiredSwings,
        int? secondRequiredSwings = null)
    {
        MushroomState mushrooms = new MushroomState(new MushroomCatalog(new[]
        {
            new MushroomDefinition(DefinitionId, 10, Cap, Leg),
        }));
        Assert.True(mushrooms.AddSite(SiteId, DefinitionId, Target, stage, tick: 0).IsSuccess);
        JobSystem jobs = new JobSystem();
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Cap, "Mushroom cap", 100, isTool: false),
            new ItemDefinition(Leg, "Mushroom leg", 100, isTool: false),
        }));
        InMemoryExecutionJournal events = new InMemoryExecutionJournal();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(id: FirstWorkerId)).IsSuccess);
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(id: SecondWorkerId)).IsSuccess);
        AgentSkillGrantService skills = new AgentSkillGrantService(agents, events);
        InMemoryMushroomRepository mushroomRepository = new InMemoryMushroomRepository(mushrooms);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository(jobs);
        InMemoryInventoryRepository inventoryRepository = new InMemoryInventoryRepository(inventory);
        SequenceSwingRandom random = new SequenceSwingRandom(
            firstRequiredSwings,
            secondRequiredSwings ?? firstRequiredSwings);
        return new Harness(
            mushrooms,
            jobs,
            inventory,
            agents,
            new StartDirectMushroomChopCommandHandler(
                mushroomRepository,
                jobRepository,
                skills,
                random,
                events),
            new ArriveAtMushroomCommandHandler(jobRepository, events),
            new CompleteMushroomSwingCommandHandler(mushroomRepository, jobRepository, events),
            new CompleteMushroomChopCommandHandler(
                mushroomRepository,
                jobRepository,
                inventoryRepository,
                skills,
                events),
            new CancelMushroomChopCommandHandler(mushroomRepository, jobRepository, events),
            new AdvanceMushroomGrowthCommandHandler(mushroomRepository, events));
    }

    private static EntityId Id(string value) => EntityId.Parse(value);

    private sealed class SequenceSwingRandom : IMushroomSwingRandom
    {
        private readonly int[] _values;
        private int _index;

        public SequenceSwingRandom(params int[] values)
        {
            _values = values;
        }

        public int SelectRequiredSwings(
            EntityId siteId,
            EntityId workerId,
            int minimum,
            int maximum)
        {
            int value = _values[Math.Min(_index, _values.Length - 1)];
            _index++;
            Assert.InRange(value, minimum, maximum);
            return value;
        }
    }

    private sealed class Harness
    {
        public Harness(
            MushroomState mushrooms,
            JobSystem jobs,
            InventoryState inventory,
            InMemoryAgentRepository agents,
            StartDirectMushroomChopCommandHandler start,
            ArriveAtMushroomCommandHandler arrive,
            CompleteMushroomSwingCommandHandler swing,
            CompleteMushroomChopCommandHandler complete,
            CancelMushroomChopCommandHandler cancel,
            AdvanceMushroomGrowthCommandHandler grow)
        {
            Mushrooms = mushrooms;
            Jobs = jobs;
            Inventory = inventory;
            Agents = agents;
            Start = start;
            Arrive = arrive;
            Swing = swing;
            Complete = complete;
            Cancel = cancel;
            Grow = grow;
        }

        public MushroomState Mushrooms { get; }
        public JobSystem Jobs { get; }
        public InventoryState Inventory { get; }
        public InMemoryAgentRepository Agents { get; }
        public StartDirectMushroomChopCommandHandler Start { get; }
        public ArriveAtMushroomCommandHandler Arrive { get; }
        public CompleteMushroomSwingCommandHandler Swing { get; }
        public CompleteMushroomChopCommandHandler Complete { get; }
        public CancelMushroomChopCommandHandler Cancel { get; }
        public AdvanceMushroomGrowthCommandHandler Grow { get; }
    }
}

}
