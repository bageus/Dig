using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class TunnelFrontLayerInputContractTests
    {
        [Fact]
        public void Tunnel_and_delete_input_ignore_hidden_depth_cell_proxies()
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

            Assert.Equal(2, CountOccurrences(interaction, "&& cell.Model.Z == 0"));
            Assert.Contains("ResolveExcavationPaintTarget(hits[index])", interaction);
        }

        private static int CountOccurrences(string text, string fragment)
        {
            int count = 0;
            int offset = 0;
            while ((offset = text.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += fragment.Length;
            }

            return count;
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
