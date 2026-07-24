using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationContinuationRuntimeContractTests
{
    [Fact]
    public void Excavation_reassigns_after_completion_and_forced_work_uses_job_routes()
    {
        string runtime = RuntimeRoot();
        string loop = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSimulationDriverBase.Loop.cs")));
        string spatial = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigTerrainSpatialExcavation.cs")));
        string multi = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigTerrainWorkManualExcavation.MultiWorker.cs")));
        string direct = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigTerrainWorkSession.DirectCommands.cs")));

        Assert.Contains("SynchronizeSpatialExcavations(nextTick,before)", loop);
        Assert.Contains("internalvoidSynchronizeSpatialExcavations", spatial);
        Assert.Contains("CreateSpatialCandidates(agents,work)", spatial);
        Assert.Contains("IsAvailableForAutomaticWork(agent)", spatial);

        Assert.Contains("CreateManualExcavationGroups", multi);
        Assert.Contains("RegisterManualExcavationGroup", multi);
        Assert.Contains("AssignNextManualExcavation", multi);
        Assert.Contains("ReleaseAssignmentsForAgents", multi);
        Assert.DoesNotContain("AssignManualQuarterExcavation(", multi);

        Assert.Contains("CancelManualQuarterExcavation", direct);
        Assert.Contains("SpatialDigJobDefinition", direct);
    }

    [Fact]
    public void Tunnel_traffic_allows_shared_cells_but_rejects_same_tick_edge_swaps()
    {
        string runtime = RuntimeRoot();
        string session = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.cs")));
        string manual = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.TunnelMovement.cs")));
        string spatial = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.SpatialWorkMovement.cs")));
        string renderer = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentRenderer.cs")));
        string visual = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentVisual.cs")));

        Assert.Contains("BeginTunnelTrafficTick(_tick)", session);
        Assert.Contains("MoveThroughTunnelTraffic(agent,destination)", session);
        Assert.Contains("_tunnelTraffic.BeginTick(tick)", manual);
        Assert.Contains("_tunnelTraffic.CanMove", manual);
        Assert.Contains("_tunnelTraffic.RecordMove", manual);
        Assert.Contains("_tunnelTraffic.CanMove", spatial);
        Assert.Contains("_tunnelTraffic.RecordMove", spatial);
        Assert.Contains("ApplyCrowdingOffsets(agents)", renderer);
        Assert.Contains("SetCrowdingOffset", visual);
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
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
