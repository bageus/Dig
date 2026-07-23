using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class DepthDesignationOverlayContractTests
    {
        [Fact]
        public void World_overlay_projects_surface_and_depth_designations_to_visible_faces()
        {
            string root = FindRepositoryRoot();
            string render = ReadRuntime(root, "DigWorldOverlayRenderer.Render.cs");
            string renderer = ReadRuntime(root, "DigWorldOverlayRenderer.cs");

            Assert.Contains(
                "PlaceExcavationDesignation(marker, cell.X, cell.Y, cell.Z)",
                render);
            Assert.Contains("if (z == 0)", renderer);
            Assert.Contains("DigTunnelProjection.CellWorldPosition", renderer);
            Assert.Contains("DigTunnelProjection.RockCellHalfExtent + FrontFaceOffset", renderer);
            Assert.Contains("Quaternion.Euler(90f, 0f, 0f)", renderer);
            Assert.Contains("PlaceCellAtDepth(marker, x, y, z, 0.08f, scale)", renderer);
            Assert.Contains("int visibleFaceZ = z > 0 ? z - 1 : z", renderer);
            Assert.Contains("new CellId(x, y, visibleFaceZ)", renderer);
        }

        [Fact]
        public void Excavation_stroke_scans_all_pointer_hits_for_cell_proxy()
        {
            string root = FindRepositoryRoot();
            string interaction = ReadRuntime(root, "DigWorldInteraction.Excavation.cs");

            Assert.Contains("RaycastHit[] hits = GetPointerHits()", interaction);
            Assert.Contains("ResolveExcavationPaintTarget(hits[index])", interaction);
            Assert.DoesNotContain("Physics.Raycast(ray, out RaycastHit hit, 500f)", interaction);
        }

        [Fact]
        public void Terrain_cell_refresh_preserves_active_excavation_proxy()
        {
            string root = FindRepositoryRoot();
            string visual = ReadRuntime(root, "DigCellVisual.cs");

            Assert.Contains("private void Awake()", visual);
            Assert.Contains("DisableInteractionCollider();", visual);
            Assert.Contains("private void DisableInteractionCollider()", visual);

            int configureStart = visual.IndexOf("public void Configure(", StringComparison.Ordinal);
            int selectedStart = visual.IndexOf("public void SetSelected(", StringComparison.Ordinal);
            Assert.True(configureStart >= 0);
            Assert.True(selectedStart > configureStart);
            string configureBody = visual.Substring(configureStart, selectedStart - configureStart);
            Assert.DoesNotContain("DisableInteractionCollider", configureBody);
            Assert.DoesNotContain("collider.enabled = false", configureBody);
        }

        [Fact]
        public void Depth_tool_uses_the_same_hold_and_drag_stroke_contract_as_tunnel_tool()
        {
            string root = FindRepositoryRoot();
            string depth = ReadRuntime(root, "DigWorldInteraction.TunnelDepthExcavation.cs");

            Assert.Contains("Input.GetMouseButton(0)", depth);
            Assert.Contains("Input.GetMouseButtonUp(0)", depth);
            Assert.Contains("_excavationStrokePlanner.Resolve", depth);
            Assert.Contains("ApplyTunnelDepthStroke", depth);
            Assert.Contains("DesignateTunnelDepthCell", depth);
            Assert.DoesNotContain("Input.GetMouseButtonDown(0)", depth);
        }

        private static string ReadRuntime(string root, string fileName)
        {
            return File.ReadAllText(Path.Combine(
                root,
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                fileName));
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
