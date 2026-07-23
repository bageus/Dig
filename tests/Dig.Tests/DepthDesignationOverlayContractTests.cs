using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class DepthDesignationOverlayContractTests
    {
        [Fact]
        public void World_overlay_projects_depth_designations_to_visible_open_face()
        {
            string root = FindRepositoryRoot();
            string render = File.ReadAllText(Path.Combine(
                root,
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                "DigWorldOverlayRenderer.Render.cs"));
            string renderer = File.ReadAllText(Path.Combine(
                root,
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                "DigWorldOverlayRenderer.cs"));

            Assert.Contains("PlaceCellAtDepth(marker, cell.X, cell.Y, cell.Z", render);
            Assert.Contains("int visibleFaceZ = z > 0 ? z - 1 : z", renderer);
            Assert.Contains("new CellId(x, y, visibleFaceZ)", renderer);
        }

        [Fact]
        public void Excavation_stroke_scans_all_pointer_hits_for_cell_proxy()
        {
            string root = FindRepositoryRoot();
            string interaction = File.ReadAllText(Path.Combine(
                root,
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                "DigWorldInteraction.Excavation.cs"));

            Assert.Contains("RaycastHit[] hits = GetPointerHits()", interaction);
            Assert.Contains("ResolveExcavationPaintTarget(hits[index])", interaction);
            Assert.DoesNotContain("Physics.Raycast(ray, out RaycastHit hit, 500f)", interaction);
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
