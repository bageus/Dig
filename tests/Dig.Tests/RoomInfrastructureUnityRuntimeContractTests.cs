using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomInfrastructureUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_synchronizes_completed_rooms_stock_jobs_and_candidates()
    {
        string source = RuntimeSource("DigTerrainRoomInfrastructure.cs");

        Assert.Contains("SynchronizeCompletedRoomInfrastructureCommand", source);
        Assert.Contains("SynchronizeRoomTemporaryStockCellCommand", source);
        Assert.Contains("SynchronizeRoomUpgradeJobsCommand", source);
        Assert.Contains("CreateDynamicCandidates(agents,work.WorkCell)", source);
        Assert.Contains("CreateRoomDeliveryCandidates(agents,source.Location.CellId)", source);
        Assert.Contains("_roomAssignment.Handle(newAssignAvailableJobsCommand(tick))", source);
        Assert.Contains("RoomUpgradeRuntimeIdentity.CreateJobId", source);
        Assert.Contains("RoomUpgradeRuntimeIdentity.CreateTransitStackId", source);
    }

    [Fact]
    public void Room_jobs_route_and_execute_before_generic_hauling()
    {
        string navigation = RuntimeSource("DigTerrainWorkNavigation.cs");
        string session = RuntimeSource("DigTerrainWorkSession.cs");
        string execution = RuntimeSource(
            "DigTerrainRoomInfrastructure.Execution.cs");

        Assert.True(
            navigation.IndexOf("TryPlanRoomUpgradeMovement", StringComparison.Ordinal)
            < navigation.IndexOf(
                "TryPlanTunnelAutomaticWorkMovement",
                StringComparison.Ordinal));
        Assert.DoesNotContain("TryPlanHaulingMovement", navigation);
        Assert.True(
            session.IndexOf("IsRoomUpgradeJob(job.Id)", StringComparison.Ordinal)
            < session.IndexOf(
                "job.DefinitionisTunnelAutomaticWorkJobDefinition",
                StringComparison.Ordinal));
        Assert.Contains("AcquireHaulingItemCommand", execution);
        Assert.Contains("CompleteRoomUpgradeDeliveryCommand", execution);
        Assert.Contains("CommitRoomUpgradeWorkIntervalCommand", execution);
        Assert.Contains("CompleteRoomUpgradeWorkCommand", execution);
    }

    [Fact]
    public void Terrain_commit_and_save_restore_rebind_authoritative_room_runtime()
    {
        string commit = RuntimeSource(
            "DigAgentSimulationDriverBase.TerrainCommitSync.cs");
        string saving = RuntimeSource(
            "DigTerrainRoomInfrastructure.Saving.cs");

        Assert.Contains("SynchronizeRoomInfrastructureRuntime", commit);
        Assert.Contains("CaptureRoomInfrastructureRuntimeState()", saving);
        Assert.Contains("_roomInfrastructure!.Get().CaptureSnapshot()", saving);
        Assert.Contains("RestoreRoomInfrastructureRuntimeState(", saving);
        Assert.Contains("RoomInfrastructureState.Restore(runtime.Infrastructure)", saving);
        Assert.Contains("_roomRuntimeSequence=runtime.NextRuntimeSequence", saving);
        Assert.Contains("ComposeRoomInfrastructureHandlers()", saving);
    }

    private static string RuntimeSource(string fileName)
    {
        string path = Path.Combine(
            FindRepositoryRoot(),
            "Assets", "Dig.Unity", "Runtime", fileName);
        return Normalize(File.ReadAllText(path));
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
