using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositWorldLifecycleTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.stone_rock");
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly TerrainDepositDefinition Iron = new TerrainDepositDefinition(
        "deposit.iron_ore",
        "Iron ore",
        new ItemId("ore.iron"),
        maximumYield: 8,
        generationWeight: 1,
        allowedHostMaterialIds: new[] { Rock });

    [Fact]
    public void Excavation_atomically_depletes_target_and_reveals_six_axis_neighbors()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2, 1);
        CellId depthNeighbor = new CellId(2, 2, 2);
        CellId sideNeighbor = new CellId(3, 2, 1);
        CellId diagonal = new CellId(3, 3, 1);
        world.ReplaceTerrainDeposits(new[]
        {
            Deposit("target", target),
            Deposit("depth", depthNeighbor),
            Deposit("side", sideNeighbor),
            Deposit("diagonal", diagonal),
        }, generatorVersion: 7);
        Assert.True(world.SetDigDesignation(target, designated: true, tick: 1).IsSuccess);
        world.DequeueUncommittedEvents();

        Result<WorldMutationResult> result = world.Excavate(target, Air, tick: 2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.False(world.GetCell(target).Value.IsSolid);
        Assert.True(world.TerrainDeposits.TryGet(target, out TerrainDepositInstance depleted));
        Assert.True(depleted.IsDepleted);
        Assert.False(depleted.IsRevealed);
        Assert.True(world.TerrainDeposits.TryGet(
            depthNeighbor,
            out TerrainDepositInstance depthRevealed));
        Assert.True(depthRevealed.IsRevealed);
        Assert.Equal(Iron.MaximumYield, depthRevealed.RemainingYield);
        Assert.True(world.TerrainDeposits.TryGet(
            sideNeighbor,
            out TerrainDepositInstance sideRevealed));
        Assert.True(sideRevealed.IsRevealed);
        Assert.True(world.TerrainDeposits.TryGet(
            diagonal,
            out TerrainDepositInstance diagonalHidden));
        Assert.False(diagonalHidden.IsRevealed);

        IDomainEvent[] events = world.PeekUncommittedEvents().ToArray();
        Assert.Single(events.OfType<TerrainDepositDepleted>());
        Assert.Equal(2, events.OfType<TerrainDepositRevealed>().Count());
        Assert.NotEmpty(events.OfType<ChunkInvalidated>());
    }

    [Fact]
    public void Depleting_one_cell_does_not_change_adjacent_yield_or_identity()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(1, 1, 0);
        CellId neighbor = new CellId(2, 1, 0);
        world.ReplaceTerrainDeposits(new[]
        {
            Deposit("target", target),
            Deposit("neighbor", neighbor),
        }, generatorVersion: 2);

        Assert.True(world.Excavate(target, Air, tick: 1).IsSuccess);

        Assert.True(world.TerrainDeposits.TryGet(
            neighbor,
            out TerrainDepositInstance unchanged));
        Assert.Equal("neighbor", unchanged.InstanceId);
        Assert.Equal(Iron.MaximumYield, unchanged.RemainingYield);
        Assert.False(unchanged.IsDepleted);
        Assert.True(unchanged.IsRevealed);
    }


    [Fact]
    public void Stale_deposit_identity_or_yield_rejects_before_world_mutation()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2, 1);
        world.ReplaceTerrainDeposits(
            new[] { Deposit("expected", target) },
            generatorVersion: 3);
        long before = world.Version;

        Result<WorldMutationResult> wrongIdentity = world.Excavate(
            target,
            Air,
            tick: 1,
            expectedDepositInstanceId: "stale",
            expectedDepositYield: Iron.MaximumYield);
        Result<WorldMutationResult> wrongYield = world.Excavate(
            target,
            Air,
            tick: 2,
            expectedDepositInstanceId: "expected",
            expectedDepositYield: Iron.MaximumYield - 1);

        Assert.Equal(WorldErrors.TerrainDepositStale, wrongIdentity.Error);
        Assert.Equal(WorldErrors.TerrainDepositStale, wrongYield.Error);
        Assert.Equal(before, world.Version);
        Assert.True(world.GetCell(target).Value.IsSolid);
        Assert.True(world.TerrainDeposits.TryGet(target, out TerrainDepositInstance deposit));
        Assert.False(deposit.IsDepleted);
    }

    [Fact]
    public void Navigation_rebuild_sees_depleted_deposit_cell_as_open_at_exact_depth()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2, 3);
        world.ReplaceTerrainDeposits(
            new[] { Deposit("deep", target) },
            generatorVersion: 4);
        Assert.True(world.Excavate(target, Air, tick: 1).IsSuccess);
        NavigationMap map = new NavigationMap(TraversalProfile.CreateFreeMover());

        Assert.True(map.Rebuild(
            world.CreateSnapshot(),
            Array.Empty<TraversalLink>()).IsSuccess);

        NavigationSnapshot snapshot = map.GetSnapshot().Value;
        Assert.True(snapshot.IsWalkable(target));
        Assert.False(snapshot.IsWalkable(new CellId(target.X, target.Y, 2)));
    }

    [Fact]
    public void Explicit_reveal_is_idempotent_and_invalidates_only_authoritative_world()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(1, 1, 2);
        world.ReplaceTerrainDeposits(
            new[] { Deposit("hidden", target) },
            generatorVersion: 9);
        long before = world.Version;

        Result<bool> first = world.RevealTerrainDeposit(target, tick: 3);
        Result<bool> second = world.RevealTerrainDeposit(target, tick: 4);

        Assert.True(first.IsSuccess && first.Value);
        Assert.True(second.IsSuccess && !second.Value);
        Assert.Equal(before + 1, world.Version);
        Assert.Single(world.PeekUncommittedEvents().OfType<TerrainDepositRevealed>());
        Assert.True(world.TerrainDeposits.TryGet(target, out TerrainDepositInstance revealed));
        Assert.True(revealed.IsRevealed);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(5, 5, 4),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
    }

    private static TerrainDepositInstance Deposit(string id, CellId cell)
    {
        return new TerrainDepositInstance(
            id,
            cell,
            Iron,
            isRevealed: false,
            remainingYield: Iron.MaximumYield,
            version: 1);
    }
}

}
