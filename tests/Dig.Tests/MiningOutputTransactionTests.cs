using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputTransactionTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly ItemId IronOre = new ItemId("ore.iron");
    private static readonly EntityId FirstStack =
        EntityId.Parse("71000000000000000000000000000001");
    private static readonly EntityId SecondStack =
        EntityId.Parse("71000000000000000000000000000002");

    [Fact]
    public void Terrain_plan_reuses_deterministic_terrain_resolver_and_commits_world_stack_at_exact_xyz()
    {
        CellId cell = new CellId(4, 5, 2);
        TerrainOutputProfile profile = new TerrainOutputProfile(
            "terrain-output.test-stone",
            version: 3,
            new[] { new TerrainOutputEntry(Stone, 1_000, 2, 2) });
        MaterialDefinition terrain = MineableTerrain(profile);
        TerrainDepositState deposits = new TerrainDepositState();
        InventoryState inventory = CreateInventory(maximumStackSize: 20);

        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            worldSeed: 17,
            generatorVersion: 4,
            cell,
            terrain,
            deposits);
        MiningOutputCommit commit = new MiningOutputCommitState().Commit(
            plan,
            FirstStack,
            inventory,
            deposits,
            tick: 8);

        TerrainOutputRoll existingRoll = new TerrainOutputResolver().Resolve(
            17,
            4,
            cell,
            profile);
        Assert.Equal(MiningOutputSourceKind.Terrain, plan.SourceKind);
        Assert.Equal(existingRoll.ItemId, plan.ItemId);
        Assert.Equal(existingRoll.Quantity, plan.Quantity);
        Assert.True(commit.HasStack);
        ItemStackSnapshot stack = inventory.GetStack(FirstStack)!;
        Assert.Equal(ItemLocation.InWorld(cell), stack.Location);
        Assert.Equal(new CellId(4, 5, 2), stack.Location.CellId);
    }

    [Fact]
    public void Deposit_output_is_exclusive_and_never_rolls_terrain_loot()
    {
        CellId cell = new CellId(7, 3, 1);
        TerrainDepositState deposits = Deposits(
            new TerrainDepositInstance(
                "deposit.instance.iron",
                cell,
                IronDeposit(maximumYield: 5),
                isRevealed: true,
                remainingYield: 5,
                version: 2));
        TerrainOutputProfile terrainProfile = new TerrainOutputProfile(
            "terrain-output.always-stone",
            1,
            new[] { new TerrainOutputEntry(Stone, 1_000, 9, 9) });
        InventoryState inventory = CreateInventory(maximumStackSize: 20);

        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            1,
            1,
            cell,
            MineableTerrain(terrainProfile),
            deposits);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Commit(plan, FirstStack, inventory, deposits, tick: 3);

        Assert.Equal(MiningOutputSourceKind.Deposit, plan.SourceKind);
        Assert.Equal(IronOre, plan.ItemId);
        Assert.Equal(5, plan.Quantity);
        Assert.Equal(0, inventory.GetTotal(Stone));
        Assert.Equal(5, inventory.GetTotal(IronOre));
        Assert.True(deposits.TryGet(cell, out TerrainDepositInstance depleted));
        Assert.True(depleted.IsDepleted);
    }

    [Fact]
    public void Same_cell_cannot_issue_output_twice()
    {
        CellId cell = new CellId(2, 2, 0);
        TerrainDepositState deposits = new TerrainDepositState();
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            5,
            1,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.once",
                1,
                new[] { new TerrainOutputEntry(Stone, 1_000, 1, 1) })),
            deposits);
        InventoryState inventory = CreateInventory(maximumStackSize: 20);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Commit(plan, FirstStack, inventory, deposits, tick: 1);

        Assert.Throws<InvalidOperationException>(() =>
            commits.Commit(plan, SecondStack, inventory, deposits, tick: 2));
        Assert.Single(commits.Snapshot());
        Assert.Equal(1, inventory.GetTotal(Stone));
    }

    [Fact]
    public void Failed_world_stack_preflight_leaves_deposit_and_commit_state_unchanged()
    {
        CellId cell = new CellId(8, 8, 3);
        TerrainDepositInstance deposit = new TerrainDepositInstance(
            "deposit.instance.large",
            cell,
            IronDeposit(maximumYield: 6),
            isRevealed: true,
            remainingYield: 6,
            version: 4);
        TerrainDepositState deposits = Deposits(deposit);
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            1,
            1,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.unused",
                1,
                Array.Empty<TerrainOutputEntry>())),
            deposits);
        InventoryState inventory = CreateInventory(maximumStackSize: 5);
        MiningOutputCommitState commits = new MiningOutputCommitState();

        Assert.Throws<InvalidOperationException>(() =>
            commits.Commit(plan, FirstStack, inventory, deposits, tick: 5));

        Assert.Empty(commits.Snapshot());
        Assert.Null(inventory.GetStack(FirstStack));
        Assert.True(deposits.TryGet(cell, out TerrainDepositInstance unchanged));
        Assert.Equal(6, unchanged.RemainingYield);
        Assert.Equal(4, unchanged.Version);
    }

    [Fact]
    public void Empty_terrain_roll_is_committed_once_without_creating_a_stack()
    {
        CellId cell = new CellId(1, 9, 0);
        TerrainDepositState deposits = new TerrainDepositState();
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            22,
            2,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.empty",
                1,
                Array.Empty<TerrainOutputEntry>())),
            deposits);
        InventoryState inventory = CreateInventory(maximumStackSize: 20);
        MiningOutputCommitState commits = new MiningOutputCommitState();

        MiningOutputCommit commit = commits.Commit(
            plan,
            default,
            inventory,
            deposits,
            tick: 1);

        Assert.True(plan.IsEmpty);
        Assert.False(commit.HasStack);
        Assert.True(commits.IsCommitted(cell));
        Assert.Empty(inventory.CreateSnapshot().Stacks);
        Assert.Throws<InvalidOperationException>(() =>
            commits.Commit(plan, default, inventory, deposits, tick: 2));
    }

    [Fact]
    public void Output_is_never_placed_in_the_miner_inventory()
    {
        CellId cell = new CellId(3, 6, 1);
        TerrainDepositState deposits = new TerrainDepositState();
        InventoryState inventory = CreateInventory(maximumStackSize: 20);
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            4,
            1,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.world-only",
                1,
                new[] { new TerrainOutputEntry(Stone, 1_000, 3, 3) })),
            deposits);

        new MiningOutputCommitState().Commit(
            plan,
            FirstStack,
            inventory,
            deposits,
            tick: 1);

        Assert.All(
            inventory.CreateSnapshot().Stacks,
            stack => Assert.Equal(ItemLocationKind.World, stack.Location.Kind));
    }

    private static InventoryState CreateInventory(int maximumStackSize)
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Stone, "Stone", maximumStackSize, isTool: false),
            new ItemDefinition(IronOre, "Iron ore", maximumStackSize, isTool: false),
        }));
    }

    private static MaterialDefinition MineableTerrain(TerrainOutputProfile profile)
    {
        return new MaterialDefinition(
            new MaterialId("terrain.test"),
            "Test terrain",
            isSolid: true,
            hardness: 10,
            isMineable: true,
            outputProfile: profile);
    }

    private static TerrainDepositDefinition IronDeposit(int maximumYield)
    {
        return new TerrainDepositDefinition(
            "deposit.iron_ore",
            "Iron ore",
            IronOre,
            maximumYield,
            generationWeight: 1);
    }

    private static TerrainDepositState Deposits(params TerrainDepositInstance[] values)
    {
        TerrainDepositState state = new TerrainDepositState();
        state.ReplaceAll(values);
        return state;
    }
}

}
