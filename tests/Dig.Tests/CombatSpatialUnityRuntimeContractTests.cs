using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSpatialUnityRuntimeContractTests
{
    [Fact]
    public void Unity_runtime_uses_spatial_execution_and_one_hover_click_classifier()
    {
        string root = FindRepositoryRoot();
        string session = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.Combat.cs"));
        string interaction = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.Combat.cs"));
        string cursor = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.DirectCommandCursor.cs"));
        string selection = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.Selection.cs"));

        Assert.Contains("CombatSpatialExecutionHandler", session, StringComparison.Ordinal);
        Assert.Contains("TryAdvanceCombat", session, StringComparison.Ordinal);
        Assert.Contains("CanIssuePlayerAttackOrder", interaction, StringComparison.Ordinal);
        Assert.Contains("TryResolveHostileCombatHoverTarget", cursor, StringComparison.Ordinal);
        Assert.Contains("DirectCommandCursorKind.Sword", cursor, StringComparison.Ordinal);
        Assert.Contains("InterruptForCombat", interaction, StringComparison.Ordinal);
        Assert.Contains("CancelPlayerAttackOrder", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveCombatAttackCommand", interaction, StringComparison.Ordinal);
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
