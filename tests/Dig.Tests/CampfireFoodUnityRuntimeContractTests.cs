using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class CampfireFoodUnityRuntimeContractTests
    {
        [Fact]
        public void Runtime_composes_harvest_supply_real_time_and_cook_owned_output()
        {
            string runtime = RuntimeRoot();
            string execution = Read(runtime, "DigBuildingProductionExecution.cs");
            string synchronization = Read(runtime, "DigBuildingProductionSynchronization.cs");
            string dependencies = Read(runtime, "DigBuildingProductionFoodDependencies.cs");
            string mushrooms = Read(runtime, "DigTerrainWorkSession.Mushrooms.cs");
            string productionRuntime = Read(runtime, "DigBuildingProductionRuntime.cs");

            Assert.Contains("CampfireProductionContent.ProductionMaterialTicks", execution);
            Assert.DoesNotContain("CampfireProductionContent.TestProductionMaterialTicks", execution);
            Assert.Contains("CreateEligibleFoodDependencyJobs(tick, agents)", synchronization);
            Assert.Contains("MushroomStage.Large", dependencies);
            Assert.Contains("eligibleWorldCap", dependencies);
            Assert.Contains("CampfireProductionContent.MushroomCapItemId", dependencies);
            Assert.Contains("_buildingInventoryRepository", mushrooms);
            Assert.Contains("CompleteProductionOrderCommand", productionRuntime);
        }

        [Fact]
        public void Food_input_uses_pickup_arrow_or_green_animated_mouth()
        {
            string runtime = RuntimeRoot();
            string cursor = Read(runtime, "DigWorldInteraction.DirectCommandCursor.cs");
            string textures = Read(runtime, "DigWorldInteraction.FoodCursorTextures.cs");
            string priority = Read(runtime, "DigWorldInteraction.ResidentCommandPriority.cs");
            string food = Read(runtime, "DigWorldInteraction.WorldFood.cs");

            Assert.Contains("DirectCommandCursorKind.Eat", cursor);
            Assert.Contains("DirectCommandCursorKind.Pickup", cursor);
            Assert.Contains("TryResolveFoodItemHoverTarget", cursor);
            Assert.Contains("IsAltPressed()", cursor);
            Assert.Contains("new Color32(55, 205, 87, 255)", textures);
            Assert.Contains("ResolveWorldItemTargetKind", priority);
            Assert.Contains("eatAfterPickup: true", food);
        }

        [Fact]
        public void Successful_food_pickup_uses_persisted_job_action_and_shared_meal()
        {
            string runtime = RuntimeRoot();
            string session = Read(runtime, "DigWorldItemPickupSession.cs");
            string execution = Read(runtime, "DigWorldItemPickupExecution.cs");
            string directCommands = Read(runtime, "DigTerrainWorkSession.DirectCommands.cs");
            string codec = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "src",
                "Dig.Application",
                "Saving",
                "WorldItemPickupJobSaveCodec.cs"));

            Assert.Contains("WorldItemPickupCompletionAction.UseConsumable", session);
            Assert.DoesNotContain("DirectWorldFoodIntent", session);
            Assert.Contains("pickup.CompletionAction", execution);
            Assert.Contains("StartResidentFoodMealCommand", execution);
            Assert.Contains("completion_action", codec);
            Assert.Contains("InterruptFoodMeal", directCommands);
            Assert.DoesNotContain("_directWorldFoodIntents", directCommands);
        }

        private static string Read(string root, string file)
        {
            return File.ReadAllText(Path.Combine(root, file));
        }

        private static string RuntimeRoot()
        {
            return Path.Combine(
                FindRepositoryRoot(),
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime");
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "src"))
                    && Directory.Exists(Path.Combine(current.FullName, "unity")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Repository root was not found.");
        }
    }
}
