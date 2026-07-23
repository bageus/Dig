using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class TerrainDesignationRenderingContractTests
    {
        [Fact]
        public void Tunnel_and_depth_designations_tint_the_chunk_submesh_without_visible_overlay_marker()
        {
            string root = FindRepositoryRoot();
            string builder = ReadRuntime(root, "DigTerrainChunkMeshBuilder.cs");
            string visual = ReadRuntime(root, "DigTerrainChunkVisual.cs");
            string overlay = ReadRuntime(root, "DigWorldOverlayRenderer.cs");

            Assert.Contains("DigTerrainSurfaceState.Designated", builder);
            Assert.Contains("ApplySurfaceStateTints(data)", visual);
            Assert.Contains("State != DigTerrainSurfaceState.Designated", visual);
            Assert.Contains("SetColor(\"_BaseColor\", DesignatedColor)", visual);
            Assert.Contains("SetColor(\"_Color\", DesignatedColor)", visual);
            Assert.Contains("marker.SetActive(false);", overlay);
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
