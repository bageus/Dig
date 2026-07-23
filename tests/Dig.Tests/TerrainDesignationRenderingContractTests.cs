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
        public void Chunk_builder_axes_are_transformed_to_the_runtime_projection_contract()
        {
            string root = FindRepositoryRoot();
            string visual = ReadRuntime(root, "DigTerrainChunkVisual.cs");
            string projection = ReadRuntime(root, "DigTunnelProjection.cs");

            Assert.Contains(
                "return new Vector3(position.x, -position.z, position.y);",
                visual);
            Assert.Contains("_mesh.vertices = TransformPositions(data.Vertices);", visual);
            Assert.Contains("_mesh.normals = TransformDirections(data.Normals);", visual);
            Assert.Contains("cell.X,", projection);
            Assert.Contains("-cell.Y,", projection);
            Assert.Contains("DepthOrigin + (cell.Z * DepthSpacing)", projection);
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
