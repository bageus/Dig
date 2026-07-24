using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SaveGameMiningOutputWiringTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly EntityId StackId =
        EntityId.Parse("76000000000000000000000000000001");

    [Fact]
    public void Builder_wires_validated_mining_output_into_save_document()
    {
        CellId cell = new CellId(2, 1, 0);
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            StackId,
            Stone,
            2,
            ItemLocation.InWorld(cell),
            tick: 5).IsSuccess);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell), StackId);

        SaveGameContext context = new SaveGameContext(
            new SaveMetadataData
            {
                SlotId = "slot-1",
                DisplayName = "Slot 1",
                SavedAtUtc = "2026-07-24T00:00:00Z",
                SimulationTick = 5,
                WorldSeed = 17,
                GeneratorVersion = 2,
            },
            CreateWorld(),
            inventory,
            new JobSystem(),
            new BuildingsState());

        Result<SaveGameDocument> result =
            new SaveGameBuilder(new JobDefinitionSaveRegistry(
                System.Array.Empty<IJobDefinitionSaveCodec>()))
                .Build(context, commits);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.MiningOutput.Commits);
        Assert.Equal(cell.X, result.Value.MiningOutput.Commits[0].X);
        Assert.Equal(cell.Y, result.Value.MiningOutput.Commits[0].Y);
        Assert.Equal(cell.Z, result.Value.MiningOutput.Commits[0].Z);
        Assert.Equal(StackId.ToString(), result.Value.MiningOutput.Commits[0].StackId);
    }

    private static WorldState CreateWorld()
    {
        MaterialId stone = new MaterialId("terrain.stone");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(
                stone,
                "Stone",
                isSolid: true,
                hardness: 10,
                isMineable: true,
                outputProfile: null),
        });

        Result<WorldState> world = WorldState.CreateFilled(
            new WorldSize(4, 4, 4),
            chunkSize: 2,
            materials,
            stone);
        Assert.True(world.IsSuccess);
        return world.Value;
    }

    private static MiningOutputPlan ResolveStone(CellId cell)
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 17,
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
                            minimumQuantity: 2,
                            maximumQuantity: 2),
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
