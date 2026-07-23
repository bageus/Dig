using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class ExcavationPreviewColorContractTests
    {
        [Fact]
        public void Tunnel_room_and_depth_previews_use_runtime_visible_geometry()
        {
            string root = FindRepositoryRoot();
            string runtime = Path.Combine(
                root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
            string terrain = File.ReadAllText(Path.Combine(runtime, "DigTerrainChunkVisual.cs"));
            string depth = File.ReadAllText(Path.Combine(
                runtime, "DigWorldRenderer.DepthDesignation.cs"));
            string roomResources = File.ReadAllText(Path.Combine(
                runtime, "DigCaveRoomPreviewRenderer.Resources.cs"));
            string roomShow = File.ReadAllText(Path.Combine(
                runtime, "DigCaveRoomPreviewRenderer.Show.cs"));
            string roomInput = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.CaveRooms.cs"));

            Assert.Contains("new Color(0.68f, 0.86f, 0.62f, 1f)", terrain);
            Assert.Contains("PrimitiveType.Cube", depth);
            Assert.Contains("DigTunnelProjection.CellWorldPosition(target)", depth);
            Assert.Contains("Vector3.one * 0.94f", depth);
            Assert.Contains("DepthDesignationColor", depth);
            Assert.Contains("renderQueue = (int)RenderQueue.Transparent", roomResources);
            Assert.Contains("_fillMaterial.SetFloat(\"_Cull\", 0f)", roomResources);
            Assert.Contains("_fillMaterial.SetFloat(\"_ZWrite\", 0f)", roomResources);
            Assert.Contains("UpdateFill(corners)", roomShow);
            Assert.Contains("ResolveCaveRoomPreviewEntrance", roomInput);
            Assert.Contains("Plane frontLayer", roomInput);
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
