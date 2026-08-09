using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class EmbeddedTerrainDepositReliefSourceContractTests
    {
        [Fact]
        public void Deposit_clusters_are_inset_and_visible_relief_is_bounded()
        {
            string topology = ReadRuntime(
                "DigTerrainChunkMeshBuilder.DepositDecorations.cs");
            string geometry = ReadRuntime(
                "DigTerrainChunkMeshBuilder.DepositDecorationGeometry.cs");
            string connectors = ReadRuntime(
                "DigTerrainChunkMeshBuilder.DepositDecorationConnectors.cs");

            Assert.Contains(
                "private const float DepositReliefInset = 0.030f;",
                topology);
            Assert.Contains(
                "private const float DepositMaximumVisibleRelief = 0.032f;",
                topology);
            Assert.Contains(
                "private const float DepositConnectorRelief = 0.004f;",
                topology);
            Assert.Contains("- normal * DepositReliefInset", topology);
            Assert.Contains("ClampDepositReliefHeight", topology);
            Assert.DoesNotContain("+ normal * 0.018f", topology);
            Assert.DoesNotContain("cell.IsDesignated", topology);

            Assert.Contains("float scale = 0.072f", geometry);
            Assert.Contains("ClampDepositReliefHeight(", geometry);
            Assert.DoesNotContain("0.05f + (scale * 0.42f", geometry);
            Assert.DoesNotContain("0.08f + scale * 0.95f", geometry);
            Assert.DoesNotContain("normal * (0.035f * damageHeight)", geometry);
            Assert.DoesNotContain("0.026f + (pebbleScale * 0.32f", geometry);

            Assert.Contains(
                "DepositReliefInset + DepositConnectorRelief",
                connectors);
            Assert.DoesNotContain("Vector3 lift = normal * 0.008f;", connectors);
        }

        private static string ReadRuntime(string file)
        {
            return File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                file));
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "src"))
                    && Directory.Exists(Path.Combine(current.FullName, "unity")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Repository root was not found.");
        }
    }
}
