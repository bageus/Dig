using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class PostExcavationTopologyRuntimeContractTests
    {
        [Fact]
        public void Authoritative_commit_rebuilds_topology_and_settles_items_before_reservations()
        {
            string runtime = RuntimeRoot();
            string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
            string sync = Read(runtime, "DigAgentSimulationDriverBase.TerrainCommitSync.cs");
            string session = Read(runtime, "DigTerrainWorkSession.cs");
            string navigation = Read(runtime, "DigTerrainWorkNavigation.cs");

            int reconcile = loop.IndexOf(
                "result=ReconcileCommittedTerrainRuntime(result,AgentSession.Tick);",
                StringComparison.Ordinal);
            int pickup = loop.IndexOf(
                "result=TerrainSession.AdvanceBuildingBoxPickup",
                StringComparison.Ordinal);
            Assert.True(reconcile >= 0 && pickup > reconcile);
            Assert.Contains("!TerrainSession.HasWorldChanged", sync);
            Assert.Contains("RefreshCommittedTerrainNavigation()", sync);
            Assert.Contains("SynchronizeExcavatedTunnelNavigation()", sync);
            Assert.Contains("renderer.Initialize(AgentSession.TunnelVolume)", sync);
            Assert.Contains("TerrainSession.SettleWorldItems(tick)", sync);
            Assert.Contains("if(current.IsFailure){returncurrent;}", sync);
            Assert.Contains("internalboolHasWorldChanged=>_worldChanged", session);
            Assert.Contains("internalResultRefreshCommittedTerrainNavigation()", navigation);
        }

        [Fact]
        public void Runtime_uses_domain_approach_and_application_item_settlement_owners()
        {
            string runtime = RuntimeRoot();
            string cadence = Read(runtime, "DigTerrainWorkExcavationCadence.cs");
            string quarters = Read(runtime, "DigTerrainWorkExcavationQuarters.cs");
            string gravity = Read(runtime, "DigTerrainWorkSession.WorldItemGravity.cs");

            Assert.Contains("ExcavationApproachResolver.Resolve", cadence);
            Assert.DoesNotContain("ResolveExcavationApproach", cadence);
            Assert.DoesNotContain("ResolveExcavationApproach", quarters);
            Assert.Contains("WorldItemGravitySettlement.Settle", gravity);
        }

        [Fact]
        public void Spatial_commit_marks_world_first_and_retry_accepts_open_cell()
        {
            string runtime = RuntimeRoot();
            string spatial = Read(runtime, "DigAgentSimulationDriverBase.CaveRooms.cs");
            string world = Read(runtime, "DigWorldSession.cs");

            int mutation = spatial.IndexOf(
                "WorldSession!.ExcavateSpatialCell(commit.Target)",
                StringComparison.Ordinal);
            int changed = spatial.IndexOf(
                "TerrainSession!.MarkAuthoritativeWorldChanged()",
                StringComparison.Ordinal);
            int topology = spatial.IndexOf(
                "AgentSession!.CompleteTunnelDepthExcavation",
                StringComparison.Ordinal);
            Assert.True(mutation >= 0 && changed > mutation && topology > changed);
            Assert.Contains("Result<CellSnapshot>current=world.GetCell(cell)", world);
            Assert.Contains("if(!current.Value.IsSolid){returnResult.Success();}", world);
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
