using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class TerrainDesignationRenderingContractTests
    {
        [Fact]
        public void Tunnel_and_depth_designations_use_the_chunk_terrain_surface()
        {
            string root = FindRepositoryRoot();
            string builder = ReadRuntime(root, "DigTerrainChunkMeshBuilder.cs");
            string visual = ReadRuntime(root, "DigTerrainChunkVisual.cs");
            string overlay = ReadRuntime(root, "DigWorldOverlayRenderer.cs");

            Assert.Contains("DigTerrainSurfaceState.Designated", builder);
            Assert.Contains("ApplySurfaceStateTints(data)", visual);
            Assert.Contains("State != DigTerrainSurfaceState.Designated", visual);
            Assert.Contains("marker.SetActive(false);", overlay);
        }

        [Fact]
        public void Chunk_geometry_remains_in_the_side_view_roots_local_space()
        {
            string root = FindRepositoryRoot();
            string visual = ReadRuntime(root, "DigTerrainChunkVisual.cs");

            Assert.Contains("_mesh.vertices = data.Vertices;", visual);
            Assert.Contains("_mesh.normals = data.Normals;", visual);
            Assert.DoesNotContain("ProjectDesignatedGeometry", visual);
            Assert.DoesNotContain("TransformBuilderPosition", visual);
        }

        [Fact]
        public void Solid_excavation_proxies_use_projection_world_positions()
        {
            string root = FindRepositoryRoot();
            string visual = ReadRuntime(root, "DigCellVisual.cs");

            Assert.Contains("if (model.IsSolid)", visual);
            Assert.Contains(
                "transform.position = DigTunnelProjection.CellWorldPosition(",
                visual);
            Assert.DoesNotContain(
                "transform.localPosition = DigTunnelProjection.CellWorldPosition(",
                visual);
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
