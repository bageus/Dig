using System;
using System.IO;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelInfrastructureUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_reconciles_provenance_before_assignment_and_settlement()
    {
        string runtime = RuntimeRoot();
        string designations = Read(runtime, "DigTerrainWorkDesignations.cs");
        string navigation = Read(runtime, "DigTerrainWorkNavigation.cs");
        string session = Read(runtime, "DigTerrainWorkSession.cs");
        string infrastructure = Read(runtime, "DigTerrainTunnelInfrastructure.cs")
            + Read(runtime, "DigTerrainTunnelInfrastructure.Saving.cs");
        string terrainCommit = Read(
            runtime,
            "DigAgentSimulationDriverBase.TerrainCommitSync.cs");

        int topology = designations.IndexOf(
            "SynchronizeTunnelInfrastructureRuntime(",
            StringComparison.Ordinal);
        int assignment = designations.IndexOf(
            "AssignAvailableJobsCommand(tick)",
            StringComparison.Ordinal);
        Assert.True(topology >= 0 && assignment > topology);
        Assert.Contains("LoadCompletedCaveRoomPlans()", infrastructure);
        Assert.Contains("PlannedTunnelCells", infrastructure);
        Assert.Contains("PlannedVerticalTunnelCells", infrastructure);
        Assert.Contains("SynchronizeTunnelAutomaticSupportHandler", infrastructure);
        Assert.Contains("SynchronizeTunnelJunctionTrimPlacementHandler", infrastructure);
        Assert.Contains("SynchronizeTunnelJunctionTrimPlacementCommand(", infrastructure);
        Assert.Contains("CompleteTunnelAutomaticWorkHandler", infrastructure);
        Assert.Contains("TryPlanTunnelAutomaticWorkMovement", navigation);
        Assert.Contains("AdvanceTunnelAutomaticWork", session);

        int postCommitTopology = terrainCommit.IndexOf(
            "SynchronizeTunnelInfrastructureRuntime(",
            StringComparison.Ordinal);
        int settlement = terrainCommit.IndexOf(
            "SettleWorldItems(tick)",
            StringComparison.Ordinal);
        Assert.True(postCommitTopology >= 0 && settlement > postCommitTopology);
    }

    [Fact]
    public void Runtime_imports_authoritative_job_route_and_command_contracts()
    {
        string infrastructure = Read(
            RuntimeRoot(),
            "DigTerrainTunnelInfrastructure.cs");

        Assert.Contains("usingDig.Application.Jobs;", infrastructure);
        Assert.Contains("newTerrainWorkRoutePlan(", infrastructure);
        Assert.Contains("newAdvanceJobCommand(", infrastructure);
        Assert.Contains("newReleaseJobAssignmentCommand(", infrastructure);
    }

    [Fact]
    public void Completed_infrastructure_is_published_to_collider_free_world_visuals()
    {
        string runtime = RuntimeRoot();
        string driver = Read(runtime, "DigAgentSimulationDriverBase.cs");
        string infrastructure = Read(runtime, "DigTerrainTunnelInfrastructure.cs");
        string worldRenderer = Read(
            runtime,
            "DigWorldRenderer.TunnelInfrastructure.cs");
        string renderer = Read(runtime, "DigTunnelInfrastructureRenderer.cs");

        Assert.Contains(
            "BindTunnelInfrastructureVisualSink(WorldRenderer.SetTunnelInfrastructureVisuals)",
            driver);
        Assert.Contains("TunnelInfrastructureVisualPresenter", infrastructure);
        Assert.Contains("TunnelInfrastructureVisualVolumeViewModel", infrastructure);
        Assert.Contains("PublishTunnelInfrastructureVisuals()", infrastructure);
        int completion = infrastructure.IndexOf(
            "CompleteTunnelAutomaticWorkCommand(job.Id,tick)",
            StringComparison.Ordinal);
        int publication = infrastructure.IndexOf(
            "PublishTunnelInfrastructureVisuals()",
            completion,
            StringComparison.Ordinal);
        Assert.True(completion >= 0 && publication > completion);
        Assert.Contains("SetTunnelInfrastructureVisuals", worldRenderer);
        Assert.Contains("MeshFilter", renderer);
        Assert.Contains("MeshRenderer", renderer);
        Assert.DoesNotContain("Collider", renderer);
        Assert.DoesNotContain("UnityEngine.Random", renderer);
    }

    [Theory]
    [InlineData(TunnelAutomaticWorkKind.WoodenSupport)]
    [InlineData(TunnelAutomaticWorkKind.JunctionStoneTrim)]
    public void Job_overlay_projects_both_automatic_tunnel_kinds_without_world_marker(
        TunnelAutomaticWorkKind kind)
    {
        EntityId jobId = Id(1);
        EntityId segmentId = Id(2);
        CellId target = new CellId(14, 7, 2);
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new TunnelAutomaticWorkJobDefinition(
            jobId,
            segmentId,
            kind,
            target,
            createdTick: 3,
            JobRetryPolicy.Default)).IsSuccess);
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);

        JobOverlayViewModel model = Assert.Single(new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository)).Load());

        Assert.Equal(target.X, model.TargetX);
        Assert.Equal(target.Y, model.TargetY);
        Assert.Equal(target.Z, model.TargetZ);
        Assert.Equal(JobToolKind.Construction, model.PreferredToolKind);
        Assert.True(model.IsTunnelInfrastructure);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
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
