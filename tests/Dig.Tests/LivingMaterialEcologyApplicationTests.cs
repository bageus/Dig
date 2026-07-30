using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialEcologyApplicationTests
{
    [Fact]
    public void DailyCycleCreatesOneHamsterAndOneGrubOffspring()
    {
        Harness harness = new Harness();
        harness.AddWorldUnit(1, LivingMaterialEcologyProfiles.HamsterItemId, 5);
        harness.AddWorldUnit(2, LivingMaterialEcologyProfiles.HamsterItemId, 6);
        harness.AddWorldUnit(3, LivingMaterialEcologyProfiles.GrubItemId, 9);

        harness.AdvanceTicks(24);

        Assert.Equal(3, harness.Count(LivingMaterialSpecies.Hamster));
        Assert.Equal(2, harness.Count(LivingMaterialSpecies.Grub));
        LivingMaterialSnapshot hamsterParent = harness.State.Get(Id(1))!;
        LivingMaterialSnapshot hamsterPartner = harness.State.Get(Id(2))!;
        Assert.Equal(1, hamsterParent.ReproductionCyclesCompleted);
        Assert.Equal(0, hamsterPartner.ReproductionCyclesCompleted);
    }

    [Fact]
    public void OneHamsterCannotReproduce()
    {
        Harness harness = new Harness();
        harness.AddWorldUnit(1, LivingMaterialEcologyProfiles.HamsterItemId, 5);

        harness.AdvanceTicks(48);

        Assert.Equal(1, harness.Count(LivingMaterialSpecies.Hamster));
        Assert.Equal(0, harness.State.Get(Id(1))!.ReproductionCyclesCompleted);
    }

    [Fact]
    public void PopulationCapTenBlocksConcurrentReproductionWithoutSpendingCycles()
    {
        Harness harness = new Harness();
        for (int index = 1; index <= 10; index++)
        {
            harness.AddWorldUnit(index, LivingMaterialEcologyProfiles.GrubItemId, 2 + index);
        }

        harness.AdvanceTicks(24);

        Assert.Equal(10, harness.Count(LivingMaterialSpecies.Grub));
        Assert.All(
            harness.State.GetAll(),
            value => Assert.Equal(0, value.ReproductionCyclesCompleted));
    }

    [Fact]
    public void StoredCampfireHamstersDoNotMoveOrFormPair()
    {
        Harness harness = new Harness();
        EntityId campfire = Id(900);
        harness.AddStoredUnit(1, LivingMaterialEcologyProfiles.HamsterItemId, campfire);
        harness.AddStoredUnit(2, LivingMaterialEcologyProfiles.HamsterItemId, campfire);

        harness.AdvanceTicks(48);

        Assert.Equal(2, harness.Count(LivingMaterialSpecies.Hamster));
        Assert.All(harness.State.GetAll(), value =>
        {
            Assert.Equal(LivingMaterialContainment.Stored, value.Containment);
            Assert.Equal(0, value.ReproductionCyclesCompleted);
            Assert.Equal(0, value.MovementCredit);
        });
    }

    [Fact]
    public void PickupAndDropPreserveIdentityAndApplyHamsterDormancy()
    {
        Harness harness = new Harness();
        harness.AddWorldUnit(1, LivingMaterialEcologyProfiles.HamsterItemId, 5);
        harness.AdvanceTicks(1);
        EntityId hamster = Id(1);
        EntityId resident = Id(700);
        Assert.True(harness.Inventory.MoveAvailable(
            hamster,
            1,
            ItemLocation.InAgent(resident),
            default,
            tick: 2).IsSuccess);

        harness.AdvanceTicks(1, startingTick: 2);
        Assert.Equal(LivingMaterialContainment.Stored, harness.State.Get(hamster)!.Containment);
        CellId dropped = new CellId(12, Harness.CorridorY, 0);
        Assert.True(harness.Inventory.MoveAvailable(
            hamster,
            1,
            ItemLocation.InWorld(dropped),
            default,
            tick: 3).IsSuccess);

        harness.AdvanceTicks(1, startingTick: 3);

        LivingMaterialSnapshot snapshot = harness.State.Get(hamster)!;
        Assert.Equal(hamster, snapshot.ItemEntityId);
        Assert.Equal(LivingMaterialContainment.Free, snapshot.Containment);
        Assert.Equal(dropped.Y, snapshot.Cell!.Value.Y);
        Assert.Equal(dropped.Z, snapshot.Cell.Value.Z);
        Assert.Equal(LivingMaterialActivity.Moving, snapshot.Activity);
        Assert.Equal(2400, snapshot.MovementCredit);
    }

    [Fact]
    public void MovementNeverLeavesFlatPlaneOrApprovedRadius()
    {
        Harness harness = new Harness();
        harness.AddWorldUnit(1, LivingMaterialEcologyProfiles.GrubItemId, 10);
        harness.AdvanceTicks(80);

        LivingMaterialSnapshot grub = harness.State.Get(Id(1))!;
        Assert.Equal(Harness.CorridorY, grub.Cell!.Value.Y);
        Assert.Equal(0, grub.Cell.Value.Z);
        Assert.InRange(
            Math.Abs(grub.Cell.Value.X - grub.AnchorCell.X),
            0,
            LivingMaterialEcologyProfiles.Grub.WanderRadius);
        Assert.Equal(grub.Cell.Value, harness.Inventory.GetStack(Id(1))!.Location.CellId);
    }

    [Fact]
    public void HamsterSteersAwayFromNearbyResidentAtNextMovementDecision()
    {
        Harness harness = new Harness();
        harness.AddWorldUnit(1, LivingMaterialEcologyProfiles.HamsterItemId, 10);
        harness.AdvanceTicks(1);
        CellId before = harness.State.Get(Id(1))!.Cell!.Value;
        CellId resident = new CellId(before.X - 1, before.Y, before.Z);

        harness.AdvanceTicks(1, startingTick: 2, residents: new[] { resident });

        CellId after = harness.State.Get(Id(1))!.Cell!.Value;
        Assert.True(after.X >= before.X);
    }

    private sealed class Harness
    {
        public const int CorridorY = 3;
        private readonly AdvanceLivingMaterialEcologyCommandHandler _handler;

        public Harness()
        {
            TraversalProfile profile = TraversalProfile.CreateFreeMover();
            WorldState world = NavigationTestFactory.CreateGroundedCorridor(
                width: 24,
                height: 8,
                chunkSize: 4,
                corridorY: CorridorY);
            NavigationMap map = NavigationTestFactory.BuildMap(world, profile);
            InMemoryNavigationRepository navigation = new InMemoryNavigationRepository();
            navigation.Save(map);
            Inventory = new InventoryState(new ItemCatalog(LivingMaterialContent.CreateItems()));
            State = new LivingMaterialEcologyState(445566);
            InMemoryLivingMaterialEcologyRepository ecology =
                new InMemoryLivingMaterialEcologyRepository(State);
            InMemoryInventoryRepository inventory = new InMemoryInventoryRepository(Inventory);
            _handler = new AdvanceLivingMaterialEcologyCommandHandler(
                ecology,
                inventory,
                navigation,
                profile.Id,
                new InMemoryExecutionJournal());
        }

        public InventoryState Inventory { get; }

        public LivingMaterialEcologyState State { get; }

        public void AddWorldUnit(int suffix, ItemId itemId, int x)
        {
            Assert.True(Inventory.AddUnit(
                Id(suffix),
                itemId,
                ItemLocation.InWorld(new CellId(x, CorridorY, 0)),
                tick: 0).IsSuccess);
        }

        public void AddStoredUnit(int suffix, ItemId itemId, EntityId buildingId)
        {
            Assert.True(Inventory.AddUnit(
                Id(suffix),
                itemId,
                ItemLocation.InBuilding(buildingId),
                tick: 0).IsSuccess);
        }

        public int Count(LivingMaterialSpecies species) =>
            State.GetAll().Count(value => value.Species == species);

        public void AdvanceTicks(
            int count,
            long startingTick = 1,
            IReadOnlyCollection<CellId>? residents = null)
        {
            for (int index = 0; index < count; index++)
            {
                Result result = _handler.Handle(new AdvanceLivingMaterialEcologyCommand(
                    startingTick + index,
                    residents ?? Array.Empty<CellId>()));
                Assert.True(result.IsSuccess, result.Error?.ToString());
            }
        }
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "2000000000000000000000000000" + suffix.ToString("D4"));
}

}
