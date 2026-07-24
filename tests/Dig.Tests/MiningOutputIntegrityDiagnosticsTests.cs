using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputIntegrityDiagnosticsTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly ItemId Iron = new ItemId("ore.iron");
    private static readonly EntityId FirstStack =
        EntityId.Parse("72000000000000000000000000000001");

    [Fact]
    public void Matching_ledger_and_world_stacks_produce_stable_quantity_diagnostics()
    {
        CellId cell = new CellId(4, 6, 2);
        InventoryState inventory = CreateInventory();
        MiningOutputCommitState commits = new MiningOutputCommitState();
        MiningOutputPlan plan = ResolveStone(cell, quantity: 3);
        commits.Record(plan, FirstStack);
        Assert.True(inventory.AddStack(
            FirstStack,
            Stone,
            3,
            ItemLocation.InWorld(cell),
            tick: 5).IsSuccess);

        MiningOutputIntegrityReport report =
            new MiningOutputIntegrityDiagnostics().Inspect(commits, inventory);

        Assert.True(report.IsValid);
        Assert.Empty(report.Issues);
        Assert.Equal(3, report.CommittedQuantity);
        Assert.Equal(3, report.TrackedWorldQuantity);
        Assert.Equal(0, report.ReservedQuantity);
    }

    [Fact]
    public void Empty_output_commit_requires_no_inventory_stack()
    {
        CellId cell = new CellId(1, 2, 0);
        InventoryState inventory = CreateInventory();
        MiningOutputCommitState commits = new MiningOutputCommitState();
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            worldSeed: 7,
            generatorVersion: 1,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.empty",
                version: 1,
                entries: System.Array.Empty<TerrainOutputEntry>())),
            new TerrainDepositState());
        commits.Record(plan, default);

        MiningOutputIntegrityReport report =
            new MiningOutputIntegrityDiagnostics().Inspect(commits, inventory);

        Assert.True(report.IsValid);
        Assert.Equal(0, report.CommittedQuantity);
        Assert.Equal(0, report.TrackedWorldQuantity);
    }

    [Fact]
    public void Missing_or_changed_world_stack_is_reported_without_mutating_state()
    {
        CellId cell = new CellId(8, 3, 1);
        InventoryState inventory = CreateInventory();
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell, quantity: 4), FirstStack);
        Assert.True(inventory.AddStack(
            FirstStack,
            Iron,
            2,
            ItemLocation.InWorld(new CellId(8, 4, 1)),
            tick: 2).IsSuccess);
        long inventoryVersion = inventory.Version;

        MiningOutputIntegrityReport report =
            new MiningOutputIntegrityDiagnostics().Inspect(commits, inventory);

        Assert.False(report.IsValid);
        Assert.Equal(
            new[]
            {
                MiningOutputIntegrityCodes.ItemMismatch,
                MiningOutputIntegrityCodes.LocationMismatch,
                MiningOutputIntegrityCodes.QuantityMismatch,
            },
            report.Issues.Select(value => value.Code).OrderBy(value => value).ToArray());
        Assert.Equal(inventoryVersion, inventory.Version);
        Assert.Single(commits.Snapshot());
    }

    private static MiningOutputPlan ResolveStone(CellId cell, int quantity)
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 11,
            generatorVersion: 2,
            cell,
            MineableTerrain(new TerrainOutputProfile(
                "terrain-output.stone",
                version: 1,
                entries: new[]
                {
                    new TerrainOutputEntry(Stone, probabilityPermille: 1_000, quantity, quantity),
                })),
            new TerrainDepositState());
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

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Stone, "Stone", maximumStackSize: 20, isTool: false),
            new ItemDefinition(Iron, "Iron", maximumStackSize: 20, isTool: false),
        }));
    }
}

}
