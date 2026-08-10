using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class CellVisualBuilderSpaceContractTests
    {
        [Fact]
        public void Cell_visual_uses_chunk_builder_local_axes_without_rendering_open_floor_tiles()
        {
            string root = FindRepositoryRoot();
            string visual = File.ReadAllText(Path.Combine(
                root,
                "Assets",
                "Dig.Unity",
                "Runtime",
                "DigCellVisual.cs"));

            Assert.Contains(
                "transform.localPosition = new Vector3(model.X, depth, model.Y);",
                visual);
            Assert.Contains(
                "_renderer.enabled = Model.IsSolid && !showQuarters;",
                visual);
            Assert.DoesNotContain("DigTunnelProjection.FloorDepth", visual);
            Assert.DoesNotContain("DigTunnelProjection.FloorThickness", visual);
            Assert.DoesNotContain(
                "transform.position = DigTunnelProjection.CellWorldPosition",
                visual);
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
