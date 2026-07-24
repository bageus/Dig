using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class TerrainDesignationRenderingContractTests
    {
        [Fact]
        public void Tunnel_designation_tint_is_an_overlay_and_does_not_recolor_rock()
        {
            string root = FindRepositoryRoot();
            string builder = ReadRuntime(root, "DigTerrainChunkMeshBuilder.cs");
            string visual = ReadRuntime(root, "DigTerrainChunkVisual.cs");
            string cursor = ReadRuntime(root, "DigExcavationCursorRenderer.cs");

            Assert.Contains("DigTerrainSurfaceState.Designated", builder);
            Assert.Contains("SynchronizeTunnelDesignations", cursor);
            Assert.Contains("SetTunnelDesignation", cursor);
            Assert.Contains("_mesh.vertices = data.Vertices;", visual);
            Assert.Contains("_mesh.normals = data.Normals;", visual);
            Assert.DoesNotContain("ApplySurfaceStateTints", visual);
            Assert.DoesNotContain("DesignatedColor", visual);
        }

        [Fact]
        public void Designation_changes_do_not_reproject_the_terrain_mesh()
        {
            string root = FindRepositoryRoot();
            string visual = ReadRuntime(root, "DigTerrainChunkVisual.cs");

            Assert.DoesNotContain("ProjectDesignatedGeometry", visual);
            Assert.DoesNotContain("TransformBuilderPosition", visual);
            Assert.DoesNotContain("data.Triangles[submesh]", visual);
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
