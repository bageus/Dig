using System;
using Dig.Application.Agents;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireFoodSaveTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId StackId = Id(2);
    private static readonly EntityId JobId = Id(3);

    [Fact]
    public void Builder_loader_round_trip_preserves_active_meal_and_applied_bite()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            StackId,
            CampfireProductionContent.GrilledMushroomItemId,
            1,
            ItemLocation.InAgent(ResidentId),
            tick: 1).IsSuccess);
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        AgentState resident = CreateResident(nutrition: 1_000);
        Assert.True(agents.Add(resident).IsSuccess);
        Assert.True(new StartResidentFoodMealHandler(
            agents,
            new InMemoryInventoryRepository(inventory),
            new FixedResidentStandingSupportQuery(supported: true),
            new InMemoryExecutionJournal()).Handle(
                new StartResidentFoodMealCommand(ResidentId, StackId, tick: 10)).IsSuccess);
        Assert.False(resident.AdvanceFoodMealBite(11).Value);
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 4),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
        JobDefinitionSaveRegistry registry = new JobDefinitionSaveRegistry(
            new IJobDefinitionSaveCodec[] { new WorldItemPickupJobSaveCodec() });
        SaveGameDocument document = new SaveGameBuilder(registry).Build(
            new SaveGameContext(
                Metadata(),
                world,
                inventory,
                new JobSystem(),
                new BuildingsState(),
                new[] { resident }));

        Result<LoadedGameState> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            registry).Load(document, materials, items);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        AgentRuntimeSnapshot runtime = loaded.Value.AgentRuntime[ResidentId];
        Assert.Equal(1_500, runtime.Needs.Nutrition.Points);
        Assert.Equal(10, runtime.MealStartedTick);
        Assert.Equal(1, runtime.ActiveMeal!.CompletedBites);
        Assert.Equal(2, runtime.ActiveMeal.RemainingBites);
        Assert.Empty(loaded.Value.Inventory.CreateSnapshot().Stacks);
    }

    [Fact]
    public void Pickup_codec_round_trips_use_action_and_old_payload_defaults_to_none()
    {
        WorldItemPickupJobSaveCodec codec = new WorldItemPickupJobSaveCodec();
        WorldItemPickupJobDefinition definition = new WorldItemPickupJobDefinition(
            JobId,
            StackId,
            quantity: 1,
            new CellId(2, 2, 0),
            ItemLocation.InWorld(new CellId(2, 2, 0)),
            destinationStackId: default,
            priority: 700,
            createdTick: 5,
            retryPolicy: JobRetryPolicy.Default,
            completionAction: WorldItemPickupCompletionAction.UseConsumable);

        JobDefinitionSaveData encoded = codec.Encode(definition);
        WorldItemPickupJobDefinition restored =
            (WorldItemPickupJobDefinition)codec.Decode(encoded);
        Assert.Equal(
            WorldItemPickupCompletionAction.UseConsumable,
            restored.CompletionAction);

        encoded.Properties.RemoveAll(value => value.Key == "completion_action");
        WorldItemPickupJobDefinition legacy =
            (WorldItemPickupJobDefinition)codec.Decode(encoded);
        Assert.Equal(WorldItemPickupCompletionAction.None, legacy.CompletionAction);
    }

    private static AgentState CreateResident(int nutrition)
    {
        return new AgentState(
            ResidentId,
            "Saved cook",
            new AgentNeedsSnapshot(
                new NeedValue(nutrition),
                new NeedValue(10_000),
                new NeedValue(10_000),
                new NeedValue(10_000)),
            new DailySchedule(
                ticksPerDay: 24,
                new[] { new ScheduleSegment(0, 24, ScheduleActivity.Work) }));
    }

    private static SaveMetadataData Metadata()
    {
        return new SaveMetadataData
        {
            SlotId = "campfire-food",
            DisplayName = "Campfire food",
            SavedAtUtc = "2026-07-28T16:00:00Z",
            SimulationTick = 11,
            WorldSeed = 7,
            GeneratorVersion = 1,
        };
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
        });
    }

    private static ItemCatalog CreateItems()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(
                CampfireProductionContent.GrilledMushroomItemId,
                "Grilled mushroom",
                maximumStackSize: 100,
                isTool: false,
                new[] { CampfireProductionContent.FoodCategoryId },
                foodUse: new ItemFoodUseDefinition(
                    nutritionUnits: 1_500,
                    biteCount: 3)),
        });
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
