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
            string targets = Read(runtime, "DigWorldInteraction.BuildingBoxTargets.cs");
            string placement = Read(runtime, "DigBuildingBoxPlacement.cs");
            string representatives = Read(
                runtime,
                "DigBuildingBoxGhostRenderer.Representatives.cs");
            string relocation = Read(runtime, "DigBuildingBoxRelocationExecution.cs");
            string relocationNavigation = Read(
                runtime,
                "DigBuildingBoxRelocationNavigation.cs");
            string pickup = Read(runtime, "DigBuildingBoxPickupExecution.cs");
            string driver = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
            string inventory = Read(runtime, "DigGameHudCanvas.Inventory.cs");

            Assert.Contains("Cursor.visible=false", interaction);
            Assert.Contains("RestoreBuildingPlacementCursor()", interaction);
            Assert.Contains("ProjectPointerToLayer(currentLayer)", interaction);
            Assert.Contains("UpdateBuildingPlacement(_buildingPlacementMode.Value,origin)", interaction);
            Assert.Contains("TryGetMovementTarget", targets);
            Assert.Contains("target.Cell", targets);
            Assert.Contains("BuildingBoxPlacementKind.RelocateBox", interaction);
            Assert.Contains("CreateBuildingBoxRelocation", placement);
            Assert.Contains("BuildingVisualState.BuildingBox", representatives);
            Assert.Contains("BuildingVisualState.Completed", representatives);
            Assert.Contains("SynchronizeBuildingBoxRelocation", driver);
            Assert.Contains("TryPlanBuildingBoxRelocationMovement", pickup);
            Assert.Contains("relocation.StartsHeld", relocationNavigation);
            Assert.Contains("CompleteBuildingBoxRelocationCommand", relocation);
            Assert.Contains("ResidentInventorySlotVisualKind.BuildingBox", inventory);
            Assert.Contains("newColor(0.10f,0.34f,0.72f,0.96f)", inventory);
        }

        [Fact]
        public void Supported_surface_and_forced_move_cancellation_stay_wired()
        {
            string root = FindRepositoryRoot();
            string runtime = RuntimeRoot();
            string support = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Domain",
                "Buildings",
                "BuildingPlacementSurfaceFacts.cs")));
            string presenter = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Presentation.Abstractions",
                "Buildings",
                "BuildingBoxPlacementPresenter.cs")));
            string confirmation = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Application",
                "Buildings",
                "ConfirmBuildingBoxPlacementHandler.cs")));
            string relocation = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Application",
                "Inventory",
                "BuildingBoxRelocationHandlers.cs")));
            string relocationPolicy = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Application",
                "Inventory",
                "BuildingBoxRelocationExecutionPolicy.cs")));
            string campfireContent = Normalize(File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Domain",
                "Content",
                "CampfireBuildingBoxContent.cs")));
            string renderer = Read(runtime, "DigBuildingBoxGhostRenderer.cs");
            string itemGhost = Read(runtime, "DigBuildingBoxGhostRenderer.Items.cs");
            string itemPolicy = Read(runtime, "DigWorldItemVisualPolicy.cs");
            string plans = Read(
                runtime,
                "DigTerrainWorkSession.BuildingBoxRelocationPresentation.cs");
            string execution = Read(runtime, "DigBuildingBoxRelocationExecution.cs");
            string relocationNavigation = Read(
                runtime,
                "DigBuildingBoxRelocationNavigation.cs");
            string direct = Read(runtime, "DigTerrainWorkSession.DirectCommands.cs");
            string cancellation = Read(
                runtime,
                "DigTerrainWorkSession.BuildingBoxDirectCancellation.cs");
            string movement = Read(runtime, "DigWorldInteraction.TunnelMovement.cs");
            string inventoryInput = Read(
                runtime,
                "DigWorldInteraction.ResidentInventory.cs");
            string liveInventoryInput = Read(
                runtime,
                "DigWorldInteraction.CanvasHud.cs");

            Assert.Contains("bottomOccupiedCell.Y+1", support);
            Assert.Contains("HasSupportingPlane(placement.Footprint,world)", presenter);
            Assert.Contains("HasSupportingPlane(placement.Footprint,world)", confirmation);
            Assert.Contains("HasSupportingPlane(command.DestinationCell,world)", relocation);
            Assert.Contains("if(!preview.IsVisible){Clear();return;}", renderer);
            Assert.Contains("RenderBuildingBoxItemPreview(preview)", renderer);
            Assert.Contains("resolution.WorldScale", itemGhost);
            Assert.Contains("RenderPlans", itemGhost);
            Assert.Contains("PlannedItemGhostTint", itemGhost);
            Assert.Contains("CreateCampfireBoxResolution", itemPolicy);
            Assert.Contains("ResolveWorldPosition", itemPolicy);
            Assert.Contains("BuildingBoxPickupJobDefinition", plans);
            Assert.Contains("BuildingBoxRelocationExecutionPolicy.Evaluate", execution);
            Assert.Contains(
                "ResolveBuildingBoxRelocationWorkTarget",
                relocationNavigation);
            Assert.Contains("for(intindex=0;index<5;index++)", execution);
            Assert.Contains("CompleteBuildingBoxRelocationCommand", execution);
            Assert.Contains("IsDepositPosition(workerCell", relocationPolicy);
            Assert.Contains("widthCells:1m", campfireContent);
            Assert.Contains("depthCells:1m", campfireContent);
            Assert.Contains("outdoorOnly:false", campfireContent);
            Assert.Contains("allowsTunnel:true", campfireContent);
            Assert.Contains("BuildingBoxAssemblyJobDefinition", direct);
            Assert.Contains("relocation.IsRelocation", direct);
            Assert.Contains("CancelBuildingBoxPlanHandler", cancellation);
            Assert.Contains("inventory.ReleaseReservations(job.Id,tick)", cancellation);
            Assert.Contains("_buildingRenderer!.Render(_terrainSession.LoadBuildings())", movement);
            Assert.Contains("_hud.SetAgents(agents,_agentSession.Tick)", movement);
            Assert.Contains("BeginResidentInventoryBuildingPlacement", inventoryInput);
            Assert.Contains("InteractResidentInventoryLayoutSlot", inventoryInput);
            Assert.Contains("PointerInputSurface.ResidentInventory", liveInventoryInput);
            Assert.Contains(
                "selectedInventoryItemIsBuildingBox:slot.IsBuildingBox",
                liveInventoryInput);
            Assert.Contains(
                "canPlaceSelectedInventoryItem:slot.CanPlace",
                liveInventoryInput);
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
            Assert.Contains("ItemLocation.InWorld(relocation.DestinationCell.Value)", handlers);
            Assert.Contains("MoveReserved(", handlers);
            Assert.Contains("destination_x", codec);
            Assert.Contains("starts_held", codec);
        }

        [Fact]
        public void Play_mode_cursor_test_respects_runtime_assembly_boundary()
        {
            string root = FindRepositoryRoot();
            string playMode = Normalize(File.ReadAllText(Path.Combine(
                root,
                "Assets",
                "Dig.Unity",
                "Tests",
                "PlayMode",
                "BuildingBoxPlacementCursorPlayModeTests.cs")));
            string deepPlayMode = Normalize(File.ReadAllText(Path.Combine(
                root,
                "Assets",
                "Dig.Unity",
                "Tests",
                "PlayMode",
                "BuildingBoxDeepPlacementPlayModeTests.cs")));
            string inventoryInteraction = Read(
                RuntimeRoot(),
                "DigWorldInteraction.ResidentInventory.cs");

            Assert.DoesNotContain("DigTunnelProjection", playMode);
            Assert.Contains("Assert.AreEqual(2f,firstPosition.x", playMode);
            Assert.Contains("Assert.AreNotEqual(firstPosition.z,secondPosition.z)", playMode);
            Assert.Contains("Relocation_preview_and_plan_match_actual_box_size", playMode);
            Assert.Contains("Invoke(ghost,\"RenderPlans\",new[]", playMode);
            Assert.DoesNotContain("ghost.RenderPlans(", playMode);
            Assert.Contains(
                "Campfire_building_ghost_is_visible_on_z1_through_z3",
                deepPlayMode);
            Assert.Contains("Placement_target_resolves_enabled_movement_surface_on_deeper_layer", playMode);
            Assert.Contains("string?stackIdValue=slot.StackId", inventoryInteraction);
            Assert.Contains("string.IsNullOrWhiteSpace(stackIdValue)", inventoryInteraction);
            Assert.Contains("EntityId.Parse(stackIdValue??string.Empty)", inventoryInteraction);
            Assert.DoesNotContain("EntityId.Parse(slot.StackId)", inventoryInteraction);
        }

        private static string Read(string runtime, string file)
        {
            return Normalize(File.ReadAllText(Path.Combine(runtime, file)));
        }

        private static string RuntimeRoot()
        {
            return Path.Combine(
                FindRepositoryRoot(),
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
