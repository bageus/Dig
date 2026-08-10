using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class BarrelDirectCommandReservationContractTests
{
    [Fact]
    public void Barrel_start_returns_typed_reservation_failure_and_runtime_replaces_assignment()
    {
        string root = FindRepositoryRoot();
        string start = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Dig.Application",
            "WorldObjects",
            "BarrelAttackStartUseCase.cs"));
        string direct = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigTerrainWorkSession.DirectCommands.cs"));
        string playMode = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "BarrelAttackSurfacePlayModeTests.cs"));

        Assert.Contains("ReservationKey.ForAgent(command.WorkerId)", start);
        Assert.Contains("JobErrors.AgentUnavailable", start);
        Assert.Contains("jobs.CanClaim", start);
        Assert.Contains("RejectNewAttack", start);
        Assert.DoesNotContain("Validated barrel attack start failed", start);
        Assert.Contains("!job.IsTerminal", direct);
        Assert.Contains("job.AssignedAgentId == residentId", direct);
        Assert.Contains("RemoveAllRoutePlans(job.Id)", direct);
        Assert.Contains(
            "Direct_barrel_order_replaces_existing_job_and_claims_selected_resident",
            playMode);
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
