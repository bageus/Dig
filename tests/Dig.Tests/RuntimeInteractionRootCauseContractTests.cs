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
            string priority = Normalize(Read(
                runtime,
                "DigWorldInteraction.ResidentCommandPriority.cs"));
            string interaction = Normalize(Read(runtime, "DigWorldInteraction.cs"));
            string boxes = Normalize(Read(runtime, "DigWorldInteraction.BuildingBoxes.cs"));
            string boxGhost = Normalize(Read(
                runtime,
                "DigBuildingBoxGhostRenderer.Representatives.cs"));
            string itemPlacement = Normalize(Read(
                runtime,
                "DigWorldInteraction.InventoryItemPlacement.cs"));
            string itemGhost = Normalize(Read(runtime, "DigInventoryItemGhostRenderer.cs"));
            string itemVisual = Normalize(Read(runtime, "DigWorldItemVisual.cs"));
            string itemSelection = Normalize(Read(
                runtime,
                "DigWorldItemVisual.Selection.cs"));

            Assert.True(
                priority.IndexOf("TryResolveCompletedBuildingHit", StringComparison.Ordinal)
                < priority.IndexOf("TryApplyTunnelMove", StringComparison.Ordinal));
            Assert.Contains("TryHandleBuildingPlacementClick()", interaction);
            Assert.Contains("TryHandleInventoryItemPlacementClick()", interaction);
            Assert.Contains("ConfirmBuildingPlacement();", boxes);
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
            string terrain = Normalize(Read(runtime, "DigTerrainWorkSession.cs"));
            string spatial = Normalize(Read(runtime, "DigTerrainSpatialExcavation.cs"));
            string quarters = Normalize(Read(
                runtime,
                "DigTerrainWorkExcavationQuarters.cs"));
            string cursor = Normalize(Read(
                runtime,
                "DigWorldInteraction.ExcavationCursor.cs"));
            string cursorRenderer = Normalize(Read(
                runtime,
                "DigExcavationCursorRenderer.cs"));
            string marker = Normalize(Read(runtime, "DigExcavationQuarterMarker.cs"));
            string room = Normalize(Read(runtime, "DigCaveRoomPreviewRenderer.Show.cs"));

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
        public void Runtime_releases_unroutable_dig_jobs_and_commits_only_valid_room_cells()
        {
            string runtime = RuntimeRoot();
            string navigation = Normalize(Read(runtime, "DigTerrainWorkNavigation.cs"));
            string roomSession = Normalize(Read(runtime, "DigWorldSession.CaveRooms.cs"));
            string roomInput = Normalize(Read(runtime, "DigWorldInteraction.CaveRooms.cs"));
            string invalidCells = Normalize(Read(
                runtime,
                "DigCaveRoomPreviewRenderer.InvalidCells.cs"));

            Assert.Contains("ReleaseUnroutableExcavationAssignment", navigation);
            Assert.Contains("ApplyCaveRoomPlan", roomSession);
            Assert.Contains("SetDigDesignations", roomSession);
            Assert.Contains("IsProtected(plan.ExcavationCells[index])", roomSession);
            Assert.Contains("SetInvalidCells(_invalidCaveRoomCells)", roomInput);
            Assert.Contains("RenderEdges", invalidCells);
        }

        private static string Normalize(string source) => source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);

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
