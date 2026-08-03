using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class SleepingResidentUnityRuntimeContractTests
{
    [Fact]
    public void Sleep_recovery_boundary_requires_a_committed_interval()
    {
        string state = Read("src", "Dig.Domain", "Agents", "AgentState.cs");
        string needs = Read("src", "Dig.Domain", "Agents", "AgentNeeds.cs");

        Assert.Contains("_activeAction.IntentKind == AgentIntentKind.Sleep", state,
            StringComparison.Ordinal);
        Assert.Contains("_activeAction.ElapsedTicks > 0", state,
            StringComparison.Ordinal);
        Assert.Contains("bool alertnessRecoveryCommitted", needs,
            StringComparison.Ordinal);
        Assert.Contains("bool nutritionCritical", needs, StringComparison.Ordinal);
        Assert.Contains("&& !alertnessRecoveryCommitted", needs,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Sleep_has_a_distinct_selectable_visual_and_roster_path()
    {
        string presenter = Read("src", "Dig.Presentation.Abstractions", "Agents",
            "ResidentVisualPresenter.cs");
        string rig = ReadRuntime("DigResidentRig.cs");
        string hudProjection = ReadRuntime("DigAgentRenderer.HudProjection.cs");
        string playMode = Read("unity", "Dig.Unity", "Assets", "Dig.Unity",
            "Tests", "PlayMode", "ResidentNeedsRuntimeIntegrationPlayModeTests.cs");

        Assert.Contains("ResidentActionVisualState.Sleep", presenter,
            StringComparison.Ordinal);
        Assert.Contains("case ResidentActionVisualState.Sleep", rig,
            StringComparison.Ordinal);
        Assert.Contains(".Where(model => model.IsAlive)", hudProjection,
            StringComparison.Ordinal);
        Assert.DoesNotContain("AgentIntentKind.Sleep", hudProjection,
            StringComparison.Ordinal);
        Assert.Contains("LoadResidentRoster", playMode, StringComparison.Ordinal);
        Assert.Contains("GetHudModels", playMode, StringComparison.Ordinal);
        Assert.Contains("SelectById", playMode, StringComparison.Ordinal);
        Assert.Contains("Assert.That(activeSleep, Is.Not.Null)", playMode,
            StringComparison.Ordinal);
        Assert.Contains("AgentSnapshot sleepingSnapshot = activeSleep!;", playMode,
            StringComparison.Ordinal);
        Assert.DoesNotContain("activeSleep.HasValue", playMode,
            StringComparison.Ordinal);
        Assert.DoesNotContain("activeSleep!.Value", playMode,
            StringComparison.Ordinal);
    }

    private static string ReadRuntime(string file) => Read(
        "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime", file);

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
        }

        return File.ReadAllText(path);
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
