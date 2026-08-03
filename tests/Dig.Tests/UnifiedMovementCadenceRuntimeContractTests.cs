using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class UnifiedMovementCadenceRuntimeContractTests
{
    [Fact]
    public void Driver_replans_and_executes_only_one_movement_substep()
    {
        string root = RepositoryRoot();
        string driver = Read(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/"
                + "DigAgentSimulationDriverBase.Loop.cs");
        string substeps = Read(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/"
                + "DigAgentSession.MovementSubsteps.cs");
        string modes = Read(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/"
                + "DigAgentSession.MovementModes.cs");

        Assert.Equal(1, Count(driver, "AdvanceMovementSubstep("));
        Assert.Contains("PlanMovement(afterFirstMovement,AgentSession.Tick)", driver);
        Assert.Contains("PlanSpatialExcavationMovement(afterFirstMovement)", driver);
        Assert.DoesNotContain("_tick=checked(_tick+1)", substeps);
        Assert.DoesNotContain("_autonomy.Execute", substeps);
        Assert.Contains("GetCombatIntent(agent.Id)!=null", substeps);
        Assert.Contains("ResidentInventoryMovementCadence.ResolveStepCount", modes);
        Assert.Contains("_movementSources", modes);
        Assert.Contains("Math.Min(budget,currentBudget)", modes);
    }

    private static int Count(string source, string fragment)
    {
        int count = 0;
        int start = 0;
        while ((start = source.IndexOf(fragment, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += fragment.Length;
        }

        return count;
    }

    private static string Read(string root, string relative)
    {
        return Normalize(File.ReadAllText(Path.Combine(root, relative)));
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

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}

}
