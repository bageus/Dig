using System;
using System.IO;
using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
    public sealed class RuntimeInteractionRootCauseContractTests
    {
        [Fact]
        public void Excavation_cadence_is_external_to_quarter_reservation()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            ExcavationWorkTarget target = new ExcavationWorkTarget(
                new CellId(4, 4, 0),
                0);
            EntityId worker = EntityId.Parse(
                "00000000-0000-0000-0000-000000000041");
            ExcavationWorkerAssignment assignment = coordinator.Assign(
                worker,
                target,
                ExcavationApproachSide.Left,
                miningSkill: 0);
            ExcavationQuarter reserved = assignment.ReservedQuarters;

            ExcavationQuarterCompletion completion = Assert.Single(
                coordinator.ApplyWork(worker));

            Assert.Equal(reserved, completion.Quarter);
            Assert.Equal(reserved, coordinator.GetState(target).Completed);
            Assert.Contains(coordinator.GetProgress(), value =>
                value.Target.Equals(target)
                && value.Completed == reserved);
        }

        [Fact]
        public void Runtime_routes_objects_before_movement_and_uses_explicit_placement_modes()
        {
            string runtime = RuntimeRoot();
            string priority = Read(runtime, "DigWorldInteraction.ResidentCommandPriority.cs");
            string interaction = Read(runtime, "DigWorldInteraction.cs");
            string boxes = Read(runtime, "DigWorldInteraction.BuildingBoxes.cs");
            string boxGhost = Read(runtime, "DigBuildingBoxGhostRenderer.Representatives.cs");
            string itemPlacement = Read(runtime, "DigWorldInteraction.InventoryItemPlacement.cs");
            string itemGhost = Read(runtime, "DigInventoryItemGhostRenderer.cs");
            string itemVisual = Read(runtime, "DigWorldItemVisual.cs");
            string itemSelection = Read(runtime, "DigWorldItemVisual.Selection.cs");

            Assert.True(
                priority.IndexOf("TryResolveCompletedBuildingHit", StringComparison.Ordinal)
                < priority.IndexOf("TryApplyTunnelMove", StringComparison.Ordinal));
            Assert.Contains("TryHandleBuildingPlacementClick()", interaction);
            Assert.Contains("TryHandleInventoryItemPlacementClick()", interaction);
            Assert.Contains("BuildingBoxGhostViewModel?visiblePreview", boxes);
            Assert.Contains("_inputRouter.Route", boxes);
            Assert.Contains("ApplyDecision(decision)", boxes);
            Assert.DoesNotContain("UpdateBuildingPlacementHover();", boxes);
            Assert.Contains("BuildingBoxPlacementKind.RelocateBox", boxGhost);
            Assert.Contains("BuildingVisualState.BuildingBox", boxGhost);
            Assert.Contains("BuildingVisualState.Completed", boxGhost);
            Assert.Contains("ValidateResidentInventoryPlacement", itemPlacement);
            Assert.Contains("CreateResidentInventoryPlacement", itemPlacement);
            Assert.Contains("DigTransparentVisualSurface", itemGhost);
            Assert.Contains("_interactionCollider.isTrigger=true", itemVisual);
            Assert.Contains("SetInteractionHighlighted", itemSelection);
        }

        [Fact]
        public void Runtime_gates_job_finalization_on_visible_quarter_progress()
        {
            string runtime = RuntimeRoot();
            string terrain = Read(runtime, "DigTerrainWorkSession.cs");
            string spatial = Read(runtime, "DigTerrainSpatialExcavation.cs");
            string quarters = Read(runtime, "DigTerrainWorkExcavationQuarters.cs");
            string cursor = Read(runtime, "DigWorldInteraction.ExcavationCursor.cs");
            string cursorRenderer = Read(runtime, "DigExcavationCursorRenderer.cs");
            string marker = Read(runtime, "DigExcavationQuarterMarker.cs");
            string room = Read(runtime, "DigCaveRoomPreviewRenderer.Show.cs");

            Assert.Contains("AdvanceExcavationQuarterWork", terrain);
            Assert.Contains("if(!quartersComplete)", terrain);
            Assert.Contains("AdvanceExcavationQuarterWork", spatial);
            Assert.Contains("LoadExcavationQuarterProgress", quarters);
            Assert.Contains("SynchronizeExcavationQuarterProgress", cursor);
            Assert.Contains("ClearExcavationQuarterProgress()", cursor);
            Assert.Contains("internalvoidClearExcavationQuarterProgress()", cursorRenderer);
            Assert.Contains("SetProgress(ExcavationQuarter.None)", cursorRenderer);
            Assert.Contains("ExcavationQuarter.UpperLeft", marker);
            Assert.Contains("ExcavationQuarter.LowerRight", marker);
            Assert.Contains("frontEdges", room);
            Assert.Contains("edge.enabled=true", room);
        }

        [Fact]
        public void Runtime_releases_unroutable_dig_jobs_and_commits_only_room_rock()
        {
            string runtime = RuntimeRoot();
            string navigation = Read(runtime, "DigTerrainWorkNavigation.cs");
            string roomSession = Read(runtime, "DigWorldSession.CaveRooms.cs");
            string roomInput = Read(runtime, "DigWorldInteraction.CaveRooms.cs");
            string invalidCells = Read(
                runtime,
                "DigCaveRoomPreviewRenderer.InvalidCells.cs");

            Assert.Contains("ReleaseUnroutableExcavationAssignment", navigation);
            Assert.Contains("ReleaseJobAssignmentCommand(job.Id,tick)", navigation);
            Assert.Contains("_excavationQuarterWork.Cancel", navigation);
            Assert.Contains("SetDigDesignations(plan.ExcavationCells", roomSession);
            Assert.DoesNotContain("SetDigDesignations(plan.VolumeCells", roomSession);
            Assert.Contains("TryResolveCaveRoomPreview", roomInput);
            Assert.Contains("CaveRoomPlanFailureReason.BaseTunnelMissing", roomInput);
            Assert.Contains("OverlaySemanticKind.PreviewInvalid", invalidCells);
        }

        [Fact]
        public void Unity_bootstrap_keeps_required_adapter_identifiers_intact()
        {
            string bootstrap = Read(RuntimeRoot(), "DigUnityBootstrap.cs");

            Assert.Contains("BindExcavationSkillSource", bootstrap);
            Assert.Contains("GetStorageStatus()", bootstrap);
            Assert.Contains("DigStockpileRendererstockpileRenderer", bootstrap);
            Assert.Contains("SetStorageStatus(storage)", bootstrap);
            Assert.Contains("SetSimulationControls(simulation)", bootstrap);
            Assert.Contains(
                "SetToolAssignmentControls(terrainSession,jobRenderer)",
                bootstrap);
            Assert.Contains(
                "SetBuildingControls(terrainSession,buildingRenderer,jobRenderer)",
                bootstrap);
            Assert.Contains("stringstage", bootstrap);
            Assert.Contains("cameraObject.tag=\"MainCamera\"", bootstrap);
            Assert.DoesNotContain("GetSD()", bootstrap);
            Assert.DoesNotContain("DigSckpileRenderer", bootstrap);
            Assert.DoesNotContain("controlD", bootstrap);
            Assert.DoesNotContain("stringsage", bootstrap);
            Assert.DoesNotContain("cameraObject.ag", bootstrap);
        }

        [Fact]
        public void Unity_simulation_and_mushroom_adapters_keep_compile_safe_references()
        {
            string runtime = RuntimeRoot();
            string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
            string mushrooms = Read(runtime, "DigWorldInteraction.Mushrooms.cs");

            Assert.Contains("AgentRenderer!.SelectedAgentId", loop);
            Assert.Contains("BuildingRenderer!.SelectedBuildingId", loop);
            Assert.Contains(
                "MushroomRenderer!.Render(WorldSession.FilterCurrentlyVisibleMushrooms(",
                loop);
            Assert.DoesNotContain(
                "MushroomRenderer!.Render(TerrainSession!.LoadMushrooms())",
                loop);
            Assert.DoesNotContain("BuildingRender!", loop);
            Assert.Contains("_hud!.SetCommandResult(result)", mushrooms);
            Assert.Contains("_hud!.SetStatus(\"Dwarforderedtochopmushroom.\")", mushrooms);
        }

        [Fact]
        public void Unity_runtime_uses_non_nullable_guarded_resident_ids()
        {
            string runtime = RuntimeRoot();
            string quarters = Read(runtime, "DigTerrainWorkExcavationQuarters.cs");
            string cadence = Read(runtime, "DigTerrainWorkExcavationCadence.cs");
            string inventory = Read(runtime, "DigWorldInteraction.ResidentInventory.cs");

            Assert.Contains("_excavationMiningSkill?.Invoke(workerId)??0", quarters);
            Assert.Contains("_excavationCadenceResolver.Resolve", cadence);
            Assert.Contains("ResolveMiningWorkInterval", cadence);
            Assert.DoesNotContain("MiningOutputWorldSeed", cadence);
            Assert.DoesNotContain("_manualExcavationMiningSkill", quarters);
            Assert.Equal(
                1,
                CountOccurrences(inventory, "string?residentIdValue=resident.Id;"));
            Assert.Equal(
                1,
                CountOccurrences(
                    inventory,
                    "EntityId.Parse(residentIdValue??string.Empty)"));
            Assert.DoesNotContain("EntityId.Parse(residentIdValue)", inventory);
            Assert.DoesNotContain("EntityId.Parse(resident.Id)", inventory);
        }

        [Fact]
        public void Runtime_keeps_box_only_selection_and_nearest_stroke_assignment_wired()
        {
            string runtime = RuntimeRoot();
            string decisions = Read(runtime, "DigWorldInteraction.Decisions.cs");
            string boxSelection = Read(
                runtime,
                "DigWorldInteraction.BuildingBoxSelection.cs");
            string itemSelection = Read(runtime, "DigWorldItemVisual.Selection.cs");
            string roster = Read(runtime, "DigGameHudCanvas.Roster.cs");
            string cell = Read(runtime, "DigCellVisual.cs");
            string marker = Read(runtime, "DigExcavationQuarterMarker.cs");
            string cursor = Read(runtime, "DigWorldInteraction.ExcavationCursor.cs");
            string nearest = Read(runtime, "DigTerrainWorkNearestAutomaticExcavation.cs");
            string stroke = Read(runtime, "DigWorldInteraction.Excavation.cs");
            string strokeBatch = Read(
                runtime,
                "DigWorldInteraction.ExcavationStrokeBatch.cs");
            string driver = Read(
                runtime,
                "DigAgentSimulationDriverBase.Excavation.cs");
            string designations = Read(runtime, "DigTerrainWorkDesignations.cs");
            string spatial = Read(runtime, "DigTerrainSpatialExcavation.cs");
            string roomPreview = Read(runtime, "DigCaveRoomPreviewRenderer.Show.cs");

            Assert.Contains("PresentationInputEffect.SelectBuildingBox", decisions);
            Assert.Contains("SelectBuildingBox(item.Model,item)", decisions);
            Assert.Contains("SetSelectionHighlighted(false)", boxSelection);
            Assert.Contains("SetSelectionHighlighted(true)", boxSelection);
            Assert.Contains("ResolveWorldItemVisual(item.StackId)", boxSelection);
            Assert.Contains("Color.Lerp(tint,SelectionColor", itemSelection);
            Assert.DoesNotContain("DigBuildingBoxSelectionHighlight", boxSelection);
            Assert.False(File.Exists(Path.Combine(
                runtime,
                "DigBuildingBoxSelectionHighlight.cs")));
            Assert.Contains("SelectBuildingBoxFromHud(id)", roster);
            Assert.Contains("SetExcavationProgress(ExcavationQuartercompleted)", cell);
            Assert.Contains("_quarterRenderers[index].gameObject.SetActive(!excavated)", cell);
            Assert.Contains("renderer.enabled=!excavated", marker);
            Assert.DoesNotContain("ExcavatedColor", marker);
            Assert.Contains("ClearExcavationQuarterProgress()", cursor);
            Assert.Contains("SetExcavationQuarterProgress", cursor);
            Assert.Contains("OrderBy(value=>value!.TargetDistance)", nearest);
            Assert.Contains("AssignedAgentId.GetValueOrDefault()", nearest);
            Assert.DoesNotContain("AssignedAgentId.Value", nearest);
            Assert.Contains("StageExcavationCell(target,active)", stroke);
            Assert.Contains("CommitPendingExcavationStroke()", stroke);
            Assert.Contains("usingDig.Application.Jobs;", strokeBatch);
            Assert.Contains("StageExcavationDesignation", strokeBatch);
            Assert.Contains("CommitExcavationDesignationBatch", strokeBatch);
            Assert.Contains("StageExcavationDesignation", driver);
            Assert.Contains("CommitExcavationDesignationBatch", driver);
            Assert.Contains("AssignNearestAutomaticDigJobs(agents,cells,tick)", designations);
            Assert.Contains("AssignNearestAutomaticSpatialJobs(agents,tick)", spatial);
            Assert.Equal(
                2,
                CountOccurrences(roomPreview, "_overlays!.ConfigureLineRenderer("));
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int startIndex = 0;
            while (true)
            {
                int index = source.IndexOf(value, startIndex, StringComparison.Ordinal);
                if (index < 0)
                {
                    return count;
                }

                count++;
                startIndex = index + value.Length;
            }
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
