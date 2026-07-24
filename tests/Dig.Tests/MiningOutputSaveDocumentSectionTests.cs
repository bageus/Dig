using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputSaveDocumentSectionTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly EntityId StackId =
        EntityId.Parse("75000000000000000000000000000001");

    [Fact]
    public void Capture_and_restore_reuse_existing_integrity_and_data_contract_boundaries()
    {
        CellId cell = new CellId(3, 2, 1);
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            StackId,
            Stone,
            3,
            ItemLocation.InWorld(cell),
            tick: 4).IsSuccess);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell, quantity: 3), StackId);

        MiningOutputSaveDocumentSection section = new MiningOutputSaveDocumentSection();
        Result<MiningOutputCommitsSaveData> captured = section.Capture(
            commits,
            inventory,
            new WorldSize(8, 8, 4));

        Assert.True(captured.IsSuccess);
        Assert.Single(captured.Value.Commits);
        Assert.Equal(1, captured.Value.Commits[0].Z);

        Result<RestoredMiningOutputState> restored = section.Restore(
            captured.Value,
            inventory,
            new WorldSize(8, 8, 4));

        Assert.True(restored.IsSuccess);
        Assert.True(restored.Value.Commits.IsCommitted(cell));
        Assert.True(restored.Value.Integrity.IsValid);
    }

    [Fact]
    public void Capture_rejects_cells_outside_authoritative_world()
    {
        CellId cell = new CellId(4, 1, 0);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell, quantity: 0));

        Result<MiningOutputCommitsSaveData> captured =
            new MiningOutputSaveDocumentSection().Capture(
                commits,
                CreateInventory(),
                new WorldSize(4, 4, 4));

        Assert.True(captured.IsFailure);
        Assert.Equal(
            MiningOutputSaveDocumentSectionErrors.CellOutOfBounds,
            captured.Error);
    }

    [Fact]
    public void Restore_maps_malformed_data_to_existing_invalid_snapshot_error()
    {
        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData();
        data.Commits.Add(new MiningOutputCommitSaveData
        {
            X = 1,
            Y = 1,
            Z = 0,
            SourceKind = 99,
            ItemId = "material.stone",
            Quantity = 1,
            StackId = "75000000000000000000000000000002",
            HasStack = true,
        });

        Result<RestoredMiningOutputState> restored =
            new MiningOutputSaveDocumentSection().Restore(
                data,
                CreateInventory(),
                new WorldSize(4, 4, 4));

        Assert.True(restored.IsFailure);
        Assert.Equal(MiningOutputSaveErrors.InvalidSnapshot, restored.Error);
    }

    private static MiningOutputPlan ResolveStone(CellId cell, int quantity)
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 41,
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
                    entries: quantity == 0
                        ? System.Array.Empty<TerrainOutputEntry>()
                        : new[]
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
