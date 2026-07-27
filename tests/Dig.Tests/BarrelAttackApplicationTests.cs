using System.Linq;
using Dig.Application.WorldObjects;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class BarrelAttackApplicationTests
{
    private static readonly BarrelDefinitionId DefinitionId =
        new BarrelDefinitionId("world.barrel.wooden");
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly ItemId Ore = new ItemId("material.ore");
    private static readonly EntityId BarrelId = Id("c1000000000000000000000000000001");
    private static readonly EntityId FirstJobId = Id("c2000000000000000000000000000001");
    private static readonly EntityId SecondJobId = Id("c2000000000000000000000000000002");
    private static readonly EntityId FirstWorkerId = Id("c3000000000000000000000000000001");
    private static readonly EntityId SecondWorkerId = Id("c3000000000000000000000000000002");
    private static readonly CellId Target = new CellId(5, 6, 0);

    [Fact]
    public void One_hit_destroys_barrel_and_creates_one_world_unit_without_progression_side_effects()
    {
        Harness harness = CreateHarness(Stone);
        EntityId output = Id("c4000000000000000000000000000001");
        Assert.True(harness.Start.Handle(new StartDirectBarrelAttackCommand(
            FirstJobId,
            BarrelId,
            FirstWorkerId,
            new CellId(4, 6, 0),
            priority: 900,
            tick: 1)).IsSuccess);
        Assert.True(harness.Arrive.Handle(new ArriveAtBarrelCommand(FirstJobId, 2)).IsSuccess);
        Assert.True(harness.Hit.Handle(new CompleteBarrelHitCommand(FirstJobId, 3)).IsSuccess);

        Result<BarrelDestructionResult> completed = harness.Complete.Handle(
            new CompleteBarrelDestructionCommand(FirstJobId, output, tick: 4));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(FirstJobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Equal(BarrelLifecycle.Destroyed, harness.Barrels.Get(BarrelId)!.Lifecycle);
        ItemStackSnapshot unit = harness.Inventory.GetStack(output)!;
        Assert.Equal(Stone, unit.ItemId);
        Assert.Equal(1, unit.Quantity);
        Assert.Equal(ItemLocation.InWorld(Target), unit.Location);
        Assert.Single(harness.Inventory.CreateSnapshot().Stacks);
    }

    [Fact]
    public void Concurrent_attacks_are_allowed_but_only_first_commit_creates_contents()
    {
        CellId firstWorkPosition = new CellId(4, 6, 0);
        CellId secondWorkPosition = new CellId(6, 6, 0);
        Harness harness = CreateHarness(Ore);
        Assert.True(harness.Start.Handle(new StartDirectBarrelAttackCommand(
            FirstJobId,
            BarrelId,
            FirstWorkerId,
            firstWorkPosition,
            900,
            1)).IsSuccess);
        Assert.True(harness.Start.Handle(new StartDirectBarrelAttackCommand(
            SecondJobId,
            BarrelId,
            SecondWorkerId,
            secondWorkPosition,
            900,
            1)).IsSuccess);

        ReservationSnapshot[] reservations = harness.Jobs.GetReservations().ToArray();
        Assert.Equal(3, reservations.Count(value => value.JobId == FirstJobId));
        Assert.Equal(3, reservations.Count(value => value.JobId == SecondJobId));
        Assert.Contains(reservations, value =>
            value.JobId == FirstJobId
            && value.Key == ReservationKey.ForJob(FirstJobId));
        Assert.Contains(reservations, value =>
            value.JobId == FirstJobId
            && value.Key == ReservationKey.ForAgent(FirstWorkerId));
        Assert.Contains(reservations, value =>
            value.JobId == FirstJobId
            && value.Key == ReservationKey.ForPosition(firstWorkPosition));
        Assert.Contains(reservations, value =>
            value.JobId == SecondJobId
            && value.Key == ReservationKey.ForJob(SecondJobId));
        Assert.Contains(reservations, value =>
            value.JobId == SecondJobId
            && value.Key == ReservationKey.ForAgent(SecondWorkerId));
        Assert.Contains(reservations, value =>
            value.JobId == SecondJobId
            && value.Key == ReservationKey.ForPosition(secondWorkPosition));
        Assert.DoesNotContain(reservations, value =>
            value.Key == ReservationKey.ForEcologyTarget(BarrelId));

        Assert.True(harness.Arrive.Handle(new ArriveAtBarrelCommand(FirstJobId, 2)).IsSuccess);
        Assert.True(harness.Arrive.Handle(new ArriveAtBarrelCommand(SecondJobId, 2)).IsSuccess);
        Assert.True(harness.Hit.Handle(new CompleteBarrelHitCommand(FirstJobId, 3)).IsSuccess);
        Assert.True(harness.Hit.Handle(new CompleteBarrelHitCommand(SecondJobId, 3)).IsSuccess);

        Result<BarrelDestructionResult> first = harness.Complete.Handle(
            new CompleteBarrelDestructionCommand(
                FirstJobId,
                Id("c4000000000000000000000000000001"),
                4));
        Result<BarrelDestructionResult> second = harness.Complete.Handle(
            new CompleteBarrelDestructionCommand(
                SecondJobId,
                Id("c4000000000000000000000000000002"),
                4));

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(BarrelApplicationErrors.GenerationConflict, second.Error);
        Assert.Single(harness.Inventory.CreateSnapshot().Stacks);
        Assert.Equal(Ore, harness.Inventory.CreateSnapshot().Stacks.Single().ItemId);
    }

    [Fact]
    public void Cancel_before_hit_preserves_supported_barrel_and_contents()
    {
        Harness harness = CreateHarness(Stone);
        Assert.True(harness.Start.Handle(new StartDirectBarrelAttackCommand(
            FirstJobId,
            BarrelId,
            FirstWorkerId,
            new CellId(4, 6, 0),
            900,
            1)).IsSuccess);

        Assert.True(harness.Cancel.Handle(new CancelBarrelAttackCommand(
            FirstJobId,
            "player_cancelled",
            tick: 2)).IsSuccess);

        BarrelSnapshot barrel = harness.Barrels.Get(BarrelId)!;
        Assert.Equal(BarrelLifecycle.Supported, barrel.Lifecycle);
        Assert.Equal(Stone, barrel.ContentsItemId);
        Assert.False(barrel.ContentsMaterialized);
        Assert.Empty(harness.Inventory.CreateSnapshot().Stacks);
    }

    private static Harness CreateHarness(ItemId contents)
    {
        BarrelState barrels = new BarrelState(new BarrelCatalog(new[]
        {
            new BarrelDefinition(DefinitionId, new[] { Stone, Ore }),
        }));
        Assert.True(barrels.Add(BarrelId, DefinitionId, Target, contents, tick: 0).IsSuccess);
        JobSystem jobs = new JobSystem();
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Stone, "Stone", 100, isTool: false),
            new ItemDefinition(Ore, "Ore", 100, isTool: false),
        }));
        InMemoryExecutionJournal events = new InMemoryExecutionJournal();
        InMemoryBarrelRepository barrelRepository = new InMemoryBarrelRepository(barrels);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository(jobs);
        InMemoryInventoryRepository inventoryRepository = new InMemoryInventoryRepository(inventory);
        return new Harness(
            barrels,
            jobs,
            inventory,
            new StartDirectBarrelAttackCommandHandler(barrelRepository, jobRepository, events),
            new ArriveAtBarrelCommandHandler(jobRepository, events),
            new CompleteBarrelHitCommandHandler(jobRepository, events),
            new CompleteBarrelDestructionCommandHandler(
                barrelRepository,
                jobRepository,
                inventoryRepository,
                events),
            new CancelBarrelAttackCommandHandler(jobRepository, events));
    }

    private static EntityId Id(string value) => EntityId.Parse(value);

    private sealed class Harness
    {
        public Harness(
            BarrelState barrels,
            JobSystem jobs,
            InventoryState inventory,
            StartDirectBarrelAttackCommandHandler start,
            ArriveAtBarrelCommandHandler arrive,
            CompleteBarrelHitCommandHandler hit,
            CompleteBarrelDestructionCommandHandler complete,
            CancelBarrelAttackCommandHandler cancel)
        {
            Barrels = barrels;
            Jobs = jobs;
            Inventory = inventory;
            Start = start;
            Arrive = arrive;
            Hit = hit;
            Complete = complete;
            Cancel = cancel;
        }

        public BarrelState Barrels { get; }
        public JobSystem Jobs { get; }
        public InventoryState Inventory { get; }
        public StartDirectBarrelAttackCommandHandler Start { get; }
        public ArriveAtBarrelCommandHandler Arrive { get; }
        public CompleteBarrelHitCommandHandler Hit { get; }
        public CompleteBarrelDestructionCommandHandler Complete { get; }
        public CancelBarrelAttackCommandHandler Cancel { get; }
    }
}

}