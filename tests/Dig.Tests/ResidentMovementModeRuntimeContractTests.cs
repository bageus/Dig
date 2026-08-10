using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentMovementModeRuntimeContractTests
{
    [Fact]
    public void Runtime_routes_all_movement_sources_through_mode_resolution()
    {
        string runtime = RuntimeRoot();
        string session = File.ReadAllText(Path.Combine(runtime, "DigAgentSession.cs"));
        string modes = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSession.MovementModes.cs"));
        string manual = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSession.TunnelMovement.cs"));
        string spatial = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSession.SpatialWorkMovement.cs"));
        string driver = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSimulationDriverBase.cs"));

        Assert.Contains("TryAdvanceAutomaticMovement(agent, destination)", session);
        Assert.Contains("ResidentMovementCommandSource.Automatic", modes);
        Assert.Contains("ResidentMovementCommandSource.Manual", manual);
        Assert.Contains("ResidentMovementCommandSource.SpatialWork", spatial);
        Assert.Contains("IsMovementStepDue(", modes);
        Assert.Contains("IsMovementStepDue(", manual);
        Assert.Contains("IsMovementStepDue(", spatial);
        Assert.Contains("SetMovementModeResolver", driver);
        Assert.Contains("ResidentInventoryMovementCadence.IsDue", modes);
    }

    [Fact]
    public void Repeat_and_interruptions_are_typed_and_destination_based()
    {
        string runtime = RuntimeRoot();
        string manual = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSession.TunnelMovement.cs"));
        string order = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentSession.ManualMovementOrder.cs"));
        string movement = manual + order;

        Assert.Contains("previous.Destination == path.Cells[path.Cells.Count - 1]", movement);
        Assert.Contains("ResidentMovementInterruptionReason.RepeatedCommand", movement);
        Assert.Contains("ResidentMovementInterruptionReason.ReplacedByCommand", movement);
        Assert.Contains("ResidentMovementInterruptionReason.RouteUnavailable", movement);
        Assert.Contains("ResidentMovementInterruptionReason.AgentDead", movement);
        Assert.DoesNotContain("DateTime", movement);
        Assert.DoesNotContain("Time.time", movement);
    }

    [Fact]
    public void Presentation_uses_mode_duration_and_carrying_state()
    {
        string runtime = RuntimeRoot();
        string visual = File.ReadAllText(Path.Combine(runtime, "DigAgentVisual.MovementModes.cs"));
        string presenter = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src", "Dig.Presentation.Abstractions", "Agents",
            "ResidentVisualPresenter.cs"));

        Assert.Contains("movementMode.TransitionDurationMultiplier", visual);
        Assert.Contains("movementMode.IsCarrying", visual);
        Assert.Contains("isMoving) return isCarrying", presenter);
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            RepositoryRoot(),
            "Assets", "Dig.Unity", "Runtime");
    }

    private static string RepositoryRoot()
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
