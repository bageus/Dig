using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSurfaceMovementRuntimeContractTests
{
    [Fact]
    public void Combat_requires_one_authoritative_surface_pose_before_attack()
    {
        string root = FindRepositoryRoot();
        string engagement = File.ReadAllText(Path.Combine(root,
            "src/Dig.Application/Combat/CombatSpatialExecutionHandler.Engagement.cs"));
        string surface = File.ReadAllText(Path.Combine(root,
            "src/Dig.Application/Combat/CombatSpatialExecutionHandler.SurfaceMovement.cs"));
        string attack = File.ReadAllText(Path.Combine(root,
            "src/Dig.Application/Combat/CombatSpatialExecutionHandler.Attack.cs"));

        Assert.Contains("IsAtCombatSurfacePose", engagement, StringComparison.Ordinal);
        Assert.Contains("CompleteCombatSurfaceApproach", engagement, StringComparison.Ordinal);
        Assert.Contains("MoveAgentOnSurfaceCommand", surface, StringComparison.Ordinal);
        Assert.Contains("WorkSurfacePositioning.Resolve", surface, StringComparison.Ordinal);
        Assert.Contains("SurfacePose.FloorCentre", surface, StringComparison.Ordinal);
        Assert.Contains("combat_surface_pose_invalidated", attack, StringComparison.Ordinal);
        Assert.True(
            attack.IndexOf("IsAtCombatSurfacePose", StringComparison.Ordinal)
                < attack.IndexOf("ResolveCombatAttackCommand", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
