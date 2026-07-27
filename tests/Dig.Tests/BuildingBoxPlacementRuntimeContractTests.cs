using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class BuildingBoxPlacementRuntimeContractTests
    {
        [Fact]
        public void Runtime_keeps_moving_cursor_and_layer_derived_intent_wired()
        {
            string runtime = RuntimeRoot();
            string interaction = Read(runtime, "DigWorldInteraction.BuildingBoxes.cs");
            string placement = Read(runtime, "DigBuildingBoxPlacement.cs");
            string representatives = Read(
                runtime,
                "DigBuildingBoxGhostRenderer.Representatives.cs");
            string relocation = Read(runtime, "DigBuildingBoxRelocationExecution.cs");
            string pickup = Read(runtime, "DigBuildingBoxPickupExecution.cs");
            string driver = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
            string inventory = Read(runtime, "DigGameHudCanvas.Inventory.cs");

            Assert.Contains("Cursor.visible=false", interaction);
            Assert.Contains("RestoreBuildingPlacementCursor()", interaction);
            Assert.Contains("ProjectPointerToLayer(currentLayer)", interaction);
            Assert.Contains("UpdateBuildingPlacement(_buildingPlacementMode.Value,origin)", interaction);
            Assert.Contains("BuildingBoxPlacementKind.RelocateBox", interaction);
            Assert.Contains("CreateBuildingBoxRelocation", placement);
            Assert.Contains("BuildingVisualState.BuildingBox", representatives);
            Assert.Contains("BuildingVisualState.Completed", representatives);
            Assert.Contains("SynchronizeBuildingBoxRelocation", driver);
            Assert.Contains("TryPlanBuildingBoxRelocationMovement", pickup);
            Assert.Contains("relocation.StartsHeld", relocation);
            Assert.Contains("ItemLocation.InWorld(relocation.DestinationCell.Value)", relocation);
            Assert.Contains("ResidentInventorySlotVisualKind.BuildingBox", inventory);
            Assert.Contains("newColor(0.10f,0.34f,0.72f,0.96f)", inventory);
        }

        [Fact]
        public void Domain_and_save_contract_keep_one_relocation_job_owner()
        {
            string root = FindRepositoryRoot();
            string definition = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Domain",
                "Jobs",
                "BuildingBoxPickupJobDefinition.cs")));
            string handlers = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Application",
                "Inventory",
                "BuildingBoxRelocationHandlers.cs")));
            string codec = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Application",
                "Saving",
                "BuildingBoxPickupJobSaveCodec.cs")));

            Assert.Contains("WorldRelocationStages", definition);
            Assert.Contains("HeldRelocationStages", definition);
            Assert.Contains("ReservationKey.ForItem(StackId)", definition);
            Assert.Contains("ReservationKey.ForPosition(DestinationCell.Value)", definition);
            Assert.Contains("jobs.Claim(command.JobId,stack.Location.OwnerId", handlers);
            Assert.Contains("MoveFullyReservedPreservingReservation", handlers);
            Assert.Contains("MoveReserved(", handlers);
            Assert.Contains("destination_x", codec);
            Assert.Contains("starts_held", codec);
        }

        private static string Read(string runtime, string file)
        {
            return Normalize(File.ReadAllText(Path.Combine(runtime, file)));
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

        private static string Normalize(string source)
        {
            return source
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\t", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Repository root was not found.");
        }
    }
}
