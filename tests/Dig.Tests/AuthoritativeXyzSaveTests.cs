using System;
using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class AuthoritativeXyzSaveTests
{
    private static readonly MaterialId Air = new MaterialId("xyz-save.air");
    private static readonly ItemId Plank = new ItemId("xyz-save.plank");
    private static readonly EntityId AgentId = EntityId.Parse(
        "a1000000000000000000000000000001");
    private static readonly EntityId BuildingId = EntityId.Parse(
        "b1000000000000000000000000000001");

    [Fact]
    public void Agent_position_round_trip_preserves_exact_depth()
    {
        AgentState agent = new AgentState(
            AgentId,
            "Deep Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            skills: null,
            traits: null,
            initialPosition: new CellId(3, 4, 2));
        SaveGameDocument document = CreateBuilder().Build(CreateContext(
            new BuildingsState(),
            new[] { agent }));

        Result<LoadedGameState> loaded = CreateLoader().Load(
            document,
            CreateMaterials(),
            CreateItems());

        Assert.True(loaded.IsSuccess);
        Assert.Equal(new CellId(3, 4, 2), loaded.Value.AgentPositions[AgentId]);
        AgentPositionSaveData saved = Assert.Single(document.AgentPositions.Agents);
        Assert.Equal(2, saved.Z);
    }

    [Fact]
    public void Building_origin_footprint_and_work_position_round_trip_on_depth_two()
    {
        BuildingDefinition definition = CreateBuildingDefinition();
        CellId origin = new CellId(4, 4, 2);
        CellId workPosition = new CellId(4, 3, 2);
        BuildingsState buildings = new BuildingsState();
        Assert.True(buildings.Place(
            BuildingId,
            definition,
            origin,
            BuildingOrientation.North,
            BuildingPlacementResult.Success(
                definition.ResolveFootprint(origin, BuildingOrientation.North),
                workPosition),
            tick: 1).IsSuccess);
        SaveGameDocument document = CreateBuilder().Build(CreateContext(
            buildings,
            Array.Empty<AgentState>()));

        Result<LoadedGameState> loaded = CreateLoader().Load(
            document,
            CreateMaterials(),
            CreateItems(),
            new BuildingCatalog(new[] { definition }));

        Assert.True(loaded.IsSuccess);
        BuildingSnapshot restored = Assert.Single(loaded.Value.Buildings.GetAll());
        Assert.Equal(origin, restored.Origin);
        Assert.Equal(workPosition, restored.WorkPosition);
        Assert.All(restored.Footprint, cell => Assert.Equal(2, cell.Z));
        BuildingSaveData saved = Assert.Single(document.Buildings.Buildings);
        Assert.Equal(2, saved.OriginZ);
        Assert.Equal(2, saved.WorkPositionZ);
    }

    private static SaveGameContext CreateContext(
        BuildingsState buildings,
        AgentState[] agents)
    {
        WorldState world = WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            CreateMaterials(),
            Air,
            explored: true).Value;
        return new SaveGameContext(
            new SaveMetadataData
            {
                SlotId = "xyz-save",
                DisplayName = "XYZ save",
                SavedAtUtc = "2026-07-22T10:00:00Z",
                SimulationTick = 2,
                WorldSeed = 88,
                GeneratorVersion = 1,
            },
            world,
            new InventoryState(CreateItems()),
            new JobSystem(),
            buildings,
            agents);
    }

    private static BuildingDefinition CreateBuildingDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("xyz-save.workshop"),
            "XYZ Workshop",
            new[] { new CellOffset(0, 0), new CellOffset(1, 0) },
            new[] { new CellOffset(0, -1) },
            new[] { new BuildingMaterialRequirement(Plank, 1) },
            requiredWork: 1,
            maximumDurability: 100);
    }

    private static SaveGameBuilder CreateBuilder()
    {
        return new SaveGameBuilder(new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
            {
                new DigJobDefinitionSaveCodec(),
            }));
    }

    private static SaveGameLoader CreateLoader()
    {
        return new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
            {
                new DigJobDefinitionSaveCodec(),
            }));
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
    }

    private static ItemCatalog CreateItems()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(Plank, "Plank", maximumStackSize: 100, isTool: false),
        });
    }
}
}
