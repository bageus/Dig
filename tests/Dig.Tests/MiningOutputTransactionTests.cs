using System;
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
    public void Terrain_plan_reuses_existing_deterministic_resolver()
    {
        CellId cell = new CellId(4, 5, 2);
        TerrainOutputProfile profile = new TerrainOutputProfile(
            "terrain-output.test-stone",
            version: 3,
            new[] { new TerrainOutputEntry(Stone, 1_000, 2, 2) });
        TerrainDepositState deposits = new TerrainDepositState();

        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            worldSeed: 17,
            generatorVersion: 4,
            cell,
            MineableTerrain(profile),
            deposits);
        TerrainOutputRoll existingRoll = new TerrainOutputResolver().Resolve(
            17,
            4,
            cell,
            profile);

        Assert.Equal(MiningOutputSourceKind.Terrain, plan.SourceKind);
        Assert.Equal(existingRoll.ItemId, plan.ItemId);
        Assert.Equal(existingRoll.Quantity, plan.Quantity);
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

        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            1,
            1,
            cell,
            MineableTerrain(terrainProfile),
            deposits);

        Assert.Equal(MiningOutputSourceKind.Deposit, plan.SourceKind);
        Assert.Equal(IronOre, plan.ItemId);
        Assert.Equal(5, plan.Quantity);
    }

    [Fact]
    public void Ledger_validation_does_not_mutate_inventory_or_deposit_state()
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
        InventoryState inventory = CreateInventory(maximumStackSize: 20);
        MiningOutputCommitState commits = new MiningOutputCommitState();

        commits.Validate(plan, FirstStack, inventory, deposits);

        Assert.Empty(commits.Snapshot());
        Assert.Empty(inventory.CreateSnapshot().Stacks);
        Assert.True(deposits.TryGet(cell, out TerrainDepositInstance unchanged));
        Assert.Equal(6, unchanged.RemainingYield);
        Assert.Equal(4, unchanged.Version);
    }

    [Fact]
    public void Failed_preflight_leaves_all_authoritative_owners_unchanged()
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
            commits.Validate(plan, FirstStack, inventory, deposits));

        Assert.Empty(commits.Snapshot());
        Assert.Null(inventory.GetStack(FirstStack));
        Assert.True(deposits.TryGet(cell, out TerrainDepositInstance unchanged));
        Assert.Equal(6, unchanged.RemainingYield);
        Assert.Equal(4, unchanged.Version);
    }

    [Fact]
    public void Same_cell_cannot_be_recorded_twice()
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
        MiningOutputCommitState commits = new MiningOutputCommitState();

        commits.Record(plan, FirstStack);

        Assert.Throws<InvalidOperationException>(() =>
            commits.Record(plan, SecondStack));
        Assert.Single(commits.Snapshot());
    }

    [Fact]
    public void Empty_terrain_roll_is_recorded_without_a_stack()
    {
        CellId cell = new CellId(1, 9, 0);
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            22,
            2,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.empty",
                1,
                Array.Empty<TerrainOutputEntry>())),
            new TerrainDepositState());
        MiningOutputCommitState commits = new MiningOutputCommitState();

        MiningOutputCommit commit = commits.Record(plan, default);

        Assert.True(plan.IsEmpty);
        Assert.False(commit.HasStack);
        Assert.True(commits.IsCommitted(cell));
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
