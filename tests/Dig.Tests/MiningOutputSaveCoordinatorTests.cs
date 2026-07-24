using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputSaveCoordinatorTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly EntityId StackId =
        EntityId.Parse("73000000000000000000000000000001");

    [Fact]
    public void Capture_and_restore_preserve_exactly_once_ledger_against_inventory()
    {
        CellId cell = new CellId(5, 7, 2);
        InventoryState inventory = CreateInventory();
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell, quantity: 4), StackId);
        Assert.True(inventory.AddStack(
            StackId,
            Stone,
            4,
            ItemLocation.InWorld(cell),
            tick: 3).IsSuccess);

        MiningOutputSaveCoordinator coordinator = new MiningOutputSaveCoordinator();
        Result<MiningOutputCommitSaveSnapshot> captured =
            coordinator.Capture(commits, inventory);

        Assert.True(captured.IsSuccess);
        Result<RestoredMiningOutputState> restored =
            coordinator.Restore(captured.Value, inventory);
        Assert.True(restored.IsSuccess);
        Assert.True(restored.Value.Integrity.IsValid);
        Assert.True(restored.Value.Commits.IsCommitted(cell));
        Assert.Equal(4, restored.Value.Integrity.CommittedQuantity);
        Assert.Equal(4, restored.Value.Integrity.TrackedWorldQuantity);
    }

    [Fact]
    public void Restore_rejects_ledger_that_does_not_match_authoritative_inventory()
    {
        CellId cell = new CellId(2, 3, 1);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell, quantity: 2), StackId);
        MiningOutputCommitSaveSnapshot snapshot =
            MiningOutputCommitSaveSnapshot.Capture(commits);
        InventoryState inventory = CreateInventory();

        Result<RestoredMiningOutputState> restored =
            new MiningOutputSaveCoordinator().Restore(snapshot, inventory);

        Assert.True(restored.IsFailure);
        Assert.Equal(MiningOutputSaveErrors.IntegrityMismatch, restored.Error);
        Assert.Empty(inventory.CreateSnapshot().Stacks);
    }

    [Fact]
    public void Restore_maps_unsupported_snapshot_version_to_stable_error()
    {
        MiningOutputCommitSaveSnapshot snapshot = new MiningOutputCommitSaveSnapshot(
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion + 1,
            System.Array.Empty<MiningOutputCommitSaveEntry>());

        Result<RestoredMiningOutputState> restored =
            new MiningOutputSaveCoordinator().Restore(snapshot, CreateInventory());

        Assert.True(restored.IsFailure);
        Assert.Equal(MiningOutputSaveErrors.InvalidSnapshot, restored.Error);
    }

    private static MiningOutputPlan ResolveStone(CellId cell, int quantity)
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 31,
            generatorVersion: 2,
            cell,
            new MaterialDefinition(
                new MaterialId("terrain.stone"),
                "Stone",
                isSolid: true,
                hardness: 10,
                isMineable: true,
                outputProfile: new TerrainOutputProfile(
                    "terrain-output.stone",
                    version: 1,
                    entries: new[]
                    {
                        new TerrainOutputEntry(
                            Stone,
                            probabilityPermille: 1_000,
                            quantity,
                            quantity),
                    })),
            new TerrainDepositState());
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                Stone,
                "Stone",
                maximumStackSize: 20,
                isTool: false),
        }));
    }
}

}
