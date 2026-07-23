using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class ExcavationPreviewColorContractTests
    {
        [Fact]
        public void Tunnel_room_and_depth_previews_have_distinct_cell_based_colors()
        {
            string root = FindRepositoryRoot();
            string runtime = Path.Combine(
                root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
            string terrain = File.ReadAllText(Path.Combine(runtime, "DigTerrainChunkVisual.cs"));
            string cell = File.ReadAllText(Path.Combine(runtime, "DigCellVisual.cs"));
            string depth = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.TunnelDepthExcavation.cs"));
            string room = File.ReadAllText(Path.Combine(
                runtime, "DigCaveRoomPreviewRenderer.Show.cs"));
            string roomInput = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.CaveRooms.cs"));

            Assert.Contains("new Color(0.68f, 0.86f, 0.62f, 1f)", terrain);
            Assert.Contains("DepthDesignationColor", cell + File.ReadAllText(Path.Combine(
                runtime, "DigWorldRenderer.DepthDesignation.cs")));
            Assert.Contains("SetDepthDesignationTint(target)", depth);
            Assert.Contains("UpdateFill(corners)", room);
            Assert.Contains("ResolveCaveRoomPreviewEntrance", roomInput);
            Assert.Contains("Plane frontLayer", roomInput);
            Assert.DoesNotContain("|| cell.Model.IsSolid", roomInput);
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
