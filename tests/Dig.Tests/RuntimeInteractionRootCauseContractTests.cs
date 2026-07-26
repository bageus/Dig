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
        public void Low_skill_excavation_finishes_one_reserved_quarter_before_replanning()
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

            coordinator.ApplySwing(worker, deterministicSeed: 1);

            ExcavationWorkerAssignment current = coordinator.GetAssignment(worker)!;
            Assert.Equal(reserved, current.ReservedQuarters);
            Assert.Equal(ExcavationQuarter.None, coordinator.GetState(target).Completed);

            for (ulong seed = 2; seed < 20
                && coordinator.GetState(target).Completed == ExcavationQuarter.None;
                seed++)
            {
                coordinator.ApplySwing(worker, seed);
            }

            Assert.Equal(reserved, coordinator.GetState(target).Completed & reserved);
            Assert.Contains(coordinator.GetProgress(), value =>
                value.Target.Equals(target)
                && value.Completed != ExcavationQuarter.None);
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

            Assert.True(
                priority.IndexOf("TryResolveCompletedBuildingHit", StringComparison.Ordinal)
                < priority.IndexOf("TryApplyTunnelMove", StringComparison.Ordinal));
            Assert.Contains("TryHandleBuildingPlacementClick()", interaction);
            Assert.Contains("TryHandleInventoryItemPlacementClick()", interaction);
            Assert.Contains("ConfirmBuildingPlacement();", boxes);
            Assert.Contains("BuildingVisualState.Completed", boxGhost);
            Assert.DoesNotContain("BuildingVisualState.BuildingBox", boxGhost);
            Assert.Contains("ValidateResidentInventoryDrop", itemPlacement);
            Assert.Contains("DropResidentInventoryStack", itemPlacement);
            Assert.Contains("DigTransparentVisualSurface", itemGhost);
            Assert.Contains("_interactionCollider.isTrigger=true", itemVisual);
        }

        [Fact]
        public void Runtime_gates_job_finalization_on_visible_quarter_progress()
        {
            string runtime = RuntimeRoot();
            string terrain = Read(runtime, "DigTerrainWorkSession.cs");
            string spatial = Read(runtime, "DigTerrainSpatialExcavation.cs");
            string quarters = Read(runtime, "DigTerrainWorkExcavationQuarters.cs");
            string cursor = Read(runtime, "DigWorldInteraction.ExcavationCursor.cs");
            string marker = Read(runtime, "DigExcavationQuarterMarker.cs");
            string room = Read(runtime, "DigCaveRoomPreviewRenderer.Show.cs");

            Assert.Contains("AdvanceExcavationQuarterWork", terrain);
            Assert.Contains("if(!quartersComplete)", terrain);
            Assert.Contains("AdvanceExcavationQuarterWork", spatial);
            Assert.Contains("LoadExcavationQuarterProgress", quarters);
            Assert.Contains("SynchronizeExcavationQuarterProgress", cursor);
            Assert.Contains("ExcavationQuarter.UpperLeft", marker);
            Assert.Contains("ExcavationQuarter.LowerRight", marker);
            Assert.Contains("frontEdges", room);
            Assert.Contains("edge.enabled=true", room);
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
        public void Unity_runtime_uses_current_skill_source_and_guarded_resident_ids()
        {
            string runtime = RuntimeRoot();
            string quarters = Read(runtime, "DigTerrainWorkExcavationQuarters.cs");
            string inventory = Read(runtime, "DigWorldInteraction.ResidentInventory.cs");

            Assert.Contains(
                "unchecked((ulong)(uint)_worldSession.MiningOutputWorldSeed)",
                quarters);
            Assert.Contains("_excavationMiningSkill?.Invoke(workerId)??0", quarters);
            Assert.DoesNotContain("_manualExcavationMiningSkill", quarters);
            Assert.Equal(
                2,
                CountOccurrences(inventory, "string?residentIdValue=resident.Id;"));
            Assert.Equal(
                2,
                CountOccurrences(inventory, "EntityId.Parse(residentIdValue!)"));
            Assert.DoesNotContain("EntityId.Parse(residentIdValue)", inventory);
            Assert.DoesNotContain("EntityId.Parse(resident.Id)", inventory);
        }

        [Fact]
        public void Runtime_keeps_box_selection_quarter_geometry_and_nearest_dig_wired()
        {
            string runtime = RuntimeRoot();
            string decisions = Read(runtime, "DigWorldInteraction.Decisions.cs");
            string boxes = Read(runtime, "DigWorldInteraction.BuildingBoxes.cs");
            string roster = Read(runtime, "DigGameHudCanvas.Roster.cs");
            string cell = Read(runtime, "DigCellVisual.cs");
            string marker = Read(runtime, "DigExcavationQuarterMarker.cs");
            string cursor = Read(runtime, "DigWorldInteraction.ExcavationCursor.cs");
            string nearest = Read(runtime, "DigTerrainWorkNearestAutomaticExcavation.cs");
            string designations = Read(runtime, "DigTerrainWorkDesignations.cs");
            string spatial = Read(runtime, "DigTerrainSpatialExcavation.cs");

            Assert.Contains("PresentationInputEffect.SelectBuildingBox", decisions);
            Assert.Contains("SelectBuildingBox(item.Model,item)", decisions);
            Assert.Contains("DigBuildingBoxSelectionHighlight", boxes);
            Assert.Contains("ResolveWorldItemVisual(item.StackId)", boxes);
            Assert.Contains("SelectBuildingBoxFromHud(id)", roster);
            Assert.Contains("SetExcavationProgress(ExcavationQuartercompleted)", cell);
            Assert.Contains("_quarterRenderers[index].gameObject.SetActive(!excavated)", cell);
            Assert.Contains("renderer.enabled=!excavated", marker);
            Assert.DoesNotContain("ExcavatedColor", marker);
            Assert.Contains("ClearExcavationQuarterProgress()", cursor);
            Assert.Contains("SetExcavationQuarterProgress", cursor);
            Assert.Contains("_directAssignmentPlanner!.Plan", nearest);
            Assert.Contains("_directSpatialAssignmentPlanner!.Plan", nearest);
            Assert.Contains("JobStatus.Available", nearest);
            Assert.Contains("AssignNearestAutomaticDigJobs(agents,cells,tick)", designations);
            Assert.Contains("AssignNearestAutomaticSpatialJobs(agents,tick)", spatial);
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
