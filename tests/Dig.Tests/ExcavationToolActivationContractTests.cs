using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class ExcavationToolActivationContractTests
    {
        [Fact]
        public void Tunnel_tools_clear_resident_selection_before_activation()
        {
            string root = FindRepositoryRoot();
            string activation = ReadRuntime(
                root,
                "DigWorldInteraction.Excavation.Activation.cs");
            string hud = ReadRuntime(root, "DigHudOverlay.Excavation.cs");

            Assert.Contains("_agentRenderer.SelectedCount > 0", activation);
            Assert.Contains("RestoreSelection(Array.Empty<string>()", activation);
            Assert.Contains("SetExcavationDrawingMode(mode)", activation);
            Assert.Contains("GUI.enabled = true", hud);
            Assert.Contains("ActivateExcavationDrawingMode(", hud);
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
