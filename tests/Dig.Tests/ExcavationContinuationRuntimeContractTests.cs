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
        string spatialAssignment = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigTerrainSpatialExcavation.Assignment.cs")));
        string multi = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigTerrainWorkManualExcavation.MultiWorker.cs")));
        string direct = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigTerrainWorkSession.DirectCommands.cs")));

        Assert.Contains("SynchronizeSpatialExcavations(nextTick,before)", loop);
        Assert.Contains("internalvoidSynchronizeSpatialExcavations", spatial);
        Assert.Contains("CreateSpatialCandidates(agents,work)", spatial);
        Assert.Contains("IsAvailableForAutomaticWork(agent)", spatial);

        Assert.Contains("CollectDesignatedCells()", multi);
        Assert.Contains("CollectTemplateRoomGroups(designated)", multi);
        Assert.Contains("_directAssignmentPlanner!.Plan", multi);
        Assert.Contains("AssignSpecificJobCommand", multi);
        Assert.Contains("ReleaseAssignmentsForAgents", multi);
        Assert.DoesNotContain("ManualExcavationGroup", multi);
        Assert.DoesNotContain("NoCandidates", multi);
        Assert.DoesNotContain("radius:4", multi);
        Assert.DoesNotContain("AssignManualQuarterExcavation(", multi);

        Assert.Contains("_clusterPlanner!.Select", spatialAssignment);
        Assert.Contains("CollectTemplateRoomGroups(designated)", spatialAssignment);
        Assert.Contains("_directSpatialAssignmentPlanner!.Plan", spatialAssignment);
        Assert.Contains("AssignSpecificJobCommand", spatialAssignment);
        Assert.DoesNotContain("SpatialManualAssignmentRadius", spatialAssignment);
        Assert.DoesNotContain("SetCandidates", spatialAssignment);
        Assert.DoesNotContain("NoCandidates", spatialAssignment);

        Assert.Contains("CancelManualQuarterExcavation", direct);
        Assert.Contains("!job.IsTerminal&&job.AssignedAgentId==residentId", direct);
        Assert.Contains("ReleaseJobAssignmentCommand", direct);
        Assert.Contains("RemoveAllRoutePlans(job.Id)", direct);
        Assert.DoesNotContain("job.DefinitionisSpatialDigJobDefinition", direct);
    }

    [Fact]
    public void Tunnel_traffic_allows_shared_cells_but_rejects_same_tick_edge_swaps()
    {
        string runtime = RuntimeRoot();
        string session = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.cs")));
        string manual = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.TunnelMovement.cs")));
        string traffic = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.SurfaceTraffic.cs")));
        string spatial = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.SpatialWorkMovement.cs")));
        string corridor = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.SurfaceCorridor.cs")));
        string renderer = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentRenderer.cs")));
        string residentRig = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentRenderer.ResidentRig.cs")));
        string visual = Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentVisual.cs")));

        Assert.Contains("BeginTunnelTrafficTick(_tick)", session);
        Assert.Contains("TryAdvanceAutomaticMovement(agent,destination)", session);
        Assert.Contains("MoveThroughTunnelTraffic(agent,destination)", Normalize(File.ReadAllText(Path.Combine(
            runtime, "DigAgentSession.MovementModes.cs"))));
        Assert.Contains("_tunnelTraffic.BeginTick(tick)", traffic);
        Assert.Contains("_tunnelTraffic.CanMove", manual);
        Assert.Contains("_tunnelTraffic.RecordMove", manual);
        Assert.Contains("MoveThroughTunnelTraffic(agent,next)", spatial);
        Assert.Contains("_tunnelTraffic.CanMove", corridor);
        Assert.Contains("_tunnelTraffic.RecordMove", corridor);
        Assert.DoesNotContain("ApplyCrowdingOffsets(agents)", renderer);
        Assert.DoesNotContain("SetCrowdingOffset", residentRig);
        Assert.Contains("ResidentDirectionalLaneResolver.Resolve", visual);
        Assert.Contains("_directionalLaneOffsetX", visual);
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
