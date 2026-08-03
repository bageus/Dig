using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{
    public sealed class ResidentFoodMealTests
    {
        [Fact]
        public void Starting_meal_consumes_one_portion_and_applies_three_bites_with_cooldowns()
        {
            Harness harness = new Harness(foodQuantity: 2, nutrition: 1_000);

            Result started = harness.Start(tick: 10);

            Assert.True(started.IsSuccess, started.Error?.ToString());
            Assert.Equal(1, harness.Inventory.GetStack(harness.StackId)!.Quantity);
            FoodMealSnapshot active = Assert.IsType<FoodMealSnapshot>(
                harness.Agent.CreateFoodMealSnapshot());
            Assert.Equal(3, active.BiteCount);
            Assert.Equal(0, active.CompletedBites);
            Assert.Equal(11, active.NextBiteTick);

            Assert.False(harness.Advance(11));
            Assert.Equal(1_500, harness.Nutrition(11));
            Assert.Equal(13, harness.Agent.CreateFoodMealSnapshot()!.NextBiteTick);
            Assert.False(harness.Advance(12));
            Assert.Equal(1_500, harness.Nutrition(12));
            Assert.False(harness.Advance(13));
            Assert.Equal(2_000, harness.Nutrition(13));
            Assert.False(harness.Advance(14));
            Assert.Equal(2_000, harness.Nutrition(14));
            Assert.True(harness.Advance(15));
            Assert.Equal(2_500, harness.Nutrition(15));
            Assert.False(harness.Agent.HasActiveFoodMeal);
        }

        [Fact]
        public void Runtime_snapshot_restores_completed_bites_and_cooldown_without_replay()
        {
            Harness harness = new Harness(foodQuantity: 1, nutrition: 1_000);
            Assert.True(harness.Start(10).IsSuccess);
            Assert.False(harness.Advance(11));
            AgentRuntimeSnapshot saved = harness.Agent.CreateRuntimeSnapshot();
            Assert.Equal(13, saved.ActiveMeal!.NextBiteTick);
            AgentState restored = new AgentState(
                harness.ResidentId,
                "Restored cook",
                new AgentNeedsSnapshot(
                    new NeedValue(9_000),
                    new NeedValue(9_000),
                    new NeedValue(9_000),
                    new NeedValue(10_000)),
                new DailySchedule(
                    ticksPerDay: 24,
                    new[] { new ScheduleSegment(0, 24, ScheduleActivity.Work) }));

            Result result = restored.RestoreRuntime(saved);

            Assert.True(result.IsSuccess, result.Error?.ToString());
            Assert.Equal(1_500, restored.CreateSnapshot(11).Needs.Nutrition.Points);
            Assert.Equal(1, restored.CreateFoodMealSnapshot()!.CompletedBites);
            Assert.Equal(13, restored.CreateFoodMealSnapshot()!.NextBiteTick);
            Assert.Equal(AgentIntentKind.Eat, restored.CreateSnapshot(11).ActiveAction!.Value.IntentKind);
            Assert.False(restored.AdvanceFoodMealBite(12).Value);
            Assert.Equal(1_500, restored.CreateSnapshot(12).Needs.Nutrition.Points);
            Assert.False(restored.AdvanceFoodMealBite(13).Value);
            Assert.False(restored.AdvanceFoodMealBite(14).Value);
            Assert.True(restored.AdvanceFoodMealBite(15).Value);
            Assert.Equal(2_500, restored.CreateSnapshot(15).Needs.Nutrition.Points);
            Assert.False(restored.HasActiveFoodMeal);
        }

        [Fact]
        public void Interrupted_meal_keeps_completed_bites_and_loses_consumed_remainder()
        {
            Harness harness = new Harness(foodQuantity: 1, nutrition: 1_000);
            Assert.True(harness.Start(10).IsSuccess);
            Assert.False(harness.Advance(11));

            Result interrupted = harness.Agent.InterruptFoodMeal(
                "direct_command_replaced",
                tick: 12);

            Assert.True(interrupted.IsSuccess, interrupted.Error?.ToString());
            Assert.Null(harness.Inventory.GetStack(harness.StackId));
            Assert.Equal(1_500, harness.Nutrition(12));
            Assert.False(harness.Agent.HasActiveFoodMeal);
            Result<bool> next = harness.Agent.AdvanceFoodMealBite(13);
            Assert.True(next.IsFailure);
            Assert.Equal(FoodMealErrors.NotActive, next.Error);
        }

        [Fact]
        public void Unsupported_standing_position_does_not_consume_or_start_meal()
        {
            Harness harness = new Harness(
                foodQuantity: 1,
                nutrition: 1_000,
                supported: false);

            Result result = harness.Start(10);

            Assert.True(result.IsFailure);
            Assert.Equal(
                ResidentFoodMealErrors.UnsupportedStandingPosition,
                result.Error);
            Assert.Equal(1, harness.Inventory.GetStack(harness.StackId)!.Quantity);
            Assert.False(harness.Agent.HasActiveFoodMeal);
        }

        [Fact]
        public void Unsupported_carried_item_is_not_consumed()
        {
            Harness harness = new Harness(foodQuantity: 1, nutrition: 1_000, useFood: false);

            Result result = harness.Start(10);

            Assert.True(result.IsFailure);
            Assert.Equal(ResidentFoodMealErrors.UnsupportedFood, result.Error);
            Assert.Equal(1, harness.Inventory.GetStack(harness.StackId)!.Quantity);
            Assert.False(harness.Agent.HasActiveFoodMeal);
        }

        private sealed class Harness
        {
            private static readonly ItemId Rock = new ItemId("material.rock");

            internal Harness(
                int foodQuantity,
                int nutrition,
                bool useFood = true,
                bool supported = true)
            {
                ResidentId = Id(1);
                StackId = Id(2);
                ItemCatalog catalog = new ItemCatalog(new[]
                {
                    new ItemDefinition(
                        CampfireProductionContent.GrilledMushroomItemId,
                        "Grilled mushroom",
                        100,
                        false,
                        new[] { CampfireProductionContent.FoodCategoryId },
                        foodUse: new ItemFoodUseDefinition(1_500, 3)),
                    new ItemDefinition(Rock, "Rock", 100, false),
                });
                Inventory = new InventoryState(catalog);
                Assert.True(Inventory.AddStack(
                    StackId,
                    useFood ? CampfireProductionContent.GrilledMushroomItemId : Rock,
                    foodQuantity,
                    ItemLocation.InAgent(ResidentId),
                    tick: 1).IsSuccess);
                InventoryRepository = new InMemoryInventoryRepository(Inventory);
                Agents = new InMemoryAgentRepository();
                Agent = new AgentState(
                    ResidentId,
                    "Cook",
                    new AgentNeedsSnapshot(
                        new NeedValue(nutrition),
                        new NeedValue(10_000),
                        new NeedValue(10_000),
                        new NeedValue(10_000)),
                    new DailySchedule(
                        ticksPerDay: 24,
                        new[] { new ScheduleSegment(0, 24, ScheduleActivity.Work) }));
                Assert.True(Agents.Add(Agent).IsSuccess);
                Journal = new InMemoryExecutionJournal();
                Handler = new StartResidentFoodMealHandler(
                    Agents,
                    InventoryRepository,
                    new FixedResidentStandingSupportQuery(supported),
                    Journal);
            }

            internal EntityId ResidentId { get; }
            internal EntityId StackId { get; }
            internal InventoryState Inventory { get; }
            internal InMemoryInventoryRepository InventoryRepository { get; }
            internal InMemoryAgentRepository Agents { get; }
            internal InMemoryExecutionJournal Journal { get; }
            internal AgentState Agent { get; }
            internal StartResidentFoodMealHandler Handler { get; }

            internal Result Start(long tick)
            {
                return Handler.Handle(new StartResidentFoodMealCommand(
                    ResidentId,
                    StackId,
                    tick));
            }

            internal bool Advance(long tick)
            {
                Result<bool> result = Agent.AdvanceFoodMealBite(tick);
                Assert.True(result.IsSuccess, result.Error?.ToString());
                return result.Value;
            }

            internal int Nutrition(long tick)
            {
                return Agent.CreateSnapshot(tick).Needs.Nutrition.Points;
            }
        }

        private static EntityId Id(int value)
        {
            return EntityId.Parse(value.ToString("x32"));
        }
    }
}
