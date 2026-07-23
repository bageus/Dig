using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class ExcavationPaletteAndWorldItemProjectionContractTests
    {
        [Fact]
        public void Excavation_palette_starts_idle_and_world_items_use_world_projection()
        {
            string root = FindRepositoryRoot();
            string runtime = Path.Combine(
                root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
            string defaults = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.ExcavationDefaults.cs"));
            string cursor = File.ReadAllText(Path.Combine(
                runtime, "DigExcavationCursorRenderer.cs"));
            string item = File.ReadAllText(Path.Combine(
                runtime, "DigWorldItemVisual.cs"));

            Assert.DoesNotContain(
                "_excavationMode = DigExcavationDrawingMode.Tunnel;",
                defaults);
            Assert.Contains("SetTunnelDigInteractionActive(active: false)", defaults);
            Assert.Contains("!cell.IsSolid", cursor);
            Assert.Contains("transform.position = DigTunnelProjection.ResidentWorldPosition(", item);
            Assert.DoesNotContain(
                "transform.localPosition = DigTunnelProjection.ResidentWorldPosition(",
                item);
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
