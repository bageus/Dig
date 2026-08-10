using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class PartialExcavationRuntimeContractTests
    {
        [Fact]
        public void Partial_quarters_replace_combined_rock_and_survive_interruption()
        {
            string runtime = RuntimeRoot();
            string cell = Read(runtime, "DigCellVisual.cs");
            string progress = Read(runtime, "DigWorldRenderer.ExcavationProgress.cs");
            string snapshot = Read(runtime, "DigTerrainRenderSnapshot.cs");
            string builder = Read(runtime, "DigTerrainRenderSnapshotBuilder.cs");
            string mesh = Read(runtime, "DigTerrainChunkMeshBuilder.cs");
            string interaction = Read(runtime, "DigWorldInteraction.ExcavationCursor.cs");

            Assert.Contains("SynchronizeExcavationQuarterProgress", progress);
            Assert.Contains("_excavationQuarterProgress", progress);
            Assert.Contains("_excavationQuarterProgress.Keys", Read(
                runtime,
                "DigWorldRenderer.VisualCatalog.cs"));
            Assert.Contains("IsPartialExcavation", snapshot);
            Assert.Contains("currentPartialExcavation", builder);
            Assert.Contains("!snapshot.IsRenderedSolid(cell.Key)", mesh);
            Assert.Contains("Model.IsSolid&&_completedExcavationQuarters", cell);
            Assert.DoesNotContain(
                "Model.IsSolid&&Model.IsDesignated&&_completedExcavationQuarters",
                cell);
            Assert.Contains("SynchronizeExcavationQuarterProgress(progress)", interaction);
        }

        [Fact]
        public void Full_commit_refreshes_world_even_when_navigation_refresh_fails()
        {
            string runtime = RuntimeRoot();
            string session = Read(runtime, "DigTerrainWorkSession.cs");
            string navigation = Read(runtime, "DigTerrainWorkNavigation.cs");
            string world = Read(runtime, "DigWorldSession.cs");

            int worldChanged = session.IndexOf("_worldChanged=true", StringComparison.Ordinal);
            int refresh = session.IndexOf("Resultrefresh=RefreshNavigation()", StringComparison.Ordinal);
            Assert.True(worldChanged >= 0 && refresh > worldChanged);
            Assert.Contains("PeekDirtyChunks()", navigation);
            Assert.Contains("_worldSession.DrainDirtyChunks()", navigation);
            Assert.Contains("internalIReadOnlyList<ChunkId>PeekDirtyChunks()", world);
        }

        private static string Read(string runtime, string file)
        {
            return Normalize(File.ReadAllText(Path.Combine(runtime, file)));
        }

        private static string RuntimeRoot()
        {
            return Path.Combine(
                FindRepositoryRoot(),
                "Assets",
                "Dig.Unity",
                "Runtime");
        }

        private static string Normalize(string source)
        {
            return source
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\t", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal);
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
