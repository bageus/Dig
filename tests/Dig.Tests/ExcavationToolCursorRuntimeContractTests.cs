using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class ExcavationToolCursorRuntimeContractTests
    {
        [Fact]
        public void Excavation_tools_keep_visible_cursors_and_authoritative_commands()
        {
            string root = FindRepositoryRoot();
            string runtime = Path.Combine(
                root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
            string cursor = File.ReadAllText(Path.Combine(
                runtime, "DigExcavationCursorRenderer.cs"));
            string cursorDriver = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.ExcavationCursor.cs"));
            string rooms = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.CaveRooms.cs"));
            string roomPreview = File.ReadAllText(Path.Combine(
                runtime, "DigCaveRoomPreviewRenderer.cs"));
            string depth = File.ReadAllText(Path.Combine(
                runtime, "DigAgentSimulationDriverBase.CaveRooms.cs"));
            string excavation = File.ReadAllText(Path.Combine(
                runtime, "DigAgentSimulationDriverBase.Excavation.cs"));
            string depthOverlay = File.ReadAllText(Path.Combine(
                runtime, "DigWorldRenderer.DepthDesignation.cs"));
            string jobs = File.ReadAllText(Path.Combine(runtime, "DigJobVisual.cs"));

            Assert.Contains("new Color(0.68f, 0.86f, 0.62f, 0.72f)", cursor);
            Assert.Contains("new Color(0.74f, 0.62f, 0.90f, 0.72f)", cursor);
            Assert.Contains("DesignationOverlap = 1.015f", cursor);
            Assert.Contains("MarkerThickness = 0.025f", cursor);
            Assert.Contains("SynchronizeTunnelDesignations", cursor);
            Assert.Contains("SynchronizeTunnelDesignations(world)", excavation);
            Assert.Contains("ResolveTunnelCursorTarget", cursorDriver);
            Assert.Contains("ResolveDepthCursorTarget", cursorDriver);
            Assert.Contains("ProjectPointerToLayer(0)", cursorDriver);

            Assert.Contains("Input.GetMouseButtonDown(0) && result.Succeeded", rooms);
            Assert.Contains("_roomPlacementHandledThisFrame = true", rooms);
            Assert.Contains("ApplyCaveRoomPlan(result.Plan!)", rooms);
            Assert.Contains("PreviewThickness = 0.025f", roomPreview);
            Assert.Contains("new CellId(entrance.X, entrance.Y, 0)", roomPreview);

            Assert.Contains("TryAssignSpatialExcavation", depth);
            Assert.Contains("IsAvailableForAutomaticPlanning", depth);
            Assert.Contains("RemoveDepthDesignationTint(commit.Target)", depth);
            Assert.Contains("RemoveDepthDesignationTint", depthOverlay);
            Assert.Contains("targetZ > 0", jobs);
            Assert.Contains("RockCellHalfExtent + 0.08f", jobs);
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
