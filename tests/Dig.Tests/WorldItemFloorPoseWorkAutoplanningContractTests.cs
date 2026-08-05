using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldItemFloorPoseWorkAutoplanningContractTests
{
    [Fact]
    public void Loose_world_pose_is_definition_owned_and_grounded_before_interaction()
    {
        string policy = ReadRuntime("DigWorldItemVisualPolicy.cs");
        string visual = ReadRuntime("DigWorldItemVisual.cs")
            + ReadRuntime("DigWorldItemVisual.FloorPose.cs");
        string grounding = ReadRuntime("DigWorldItemGrounding.cs");
        string internalStock = ReadRuntime("DigBuildingInternalStockRenderer.cs");

        Assert.Contains("item.IsBuildingBox", policy);
        Assert.Contains("Quaternion.Euler(0f, 0f, 90f)", policy);
        Assert.Contains("ResolveLooseWorldRotation", visual);
        Assert.Contains("RefreshInteractionColliderForCurrentPose", visual);
        Assert.Contains("ApplyColliderGeometry(resolution);", visual);
        Assert.Contains("ApplyLooseWorldFloorPose", grounding);
        Assert.Contains("visual.PlaceOnFloor", internalStock);
        Assert.DoesNotContain("itemId ==", policy);
        Assert.DoesNotContain("verticalOffset", policy);
    }

    [Fact]
    public void Automatic_job_candidate_paths_use_schedule_gated_projection()
    {
        string viewModel = Read(
            "src", "Dig.Presentation.Abstractions", "Agents", "AgentViewModel.cs");
        string candidates = Read(
            "src", "Dig.Domain", "Agents", "AgentDecisionCandidates.cs");
        string runtime = string.Concat(
            ReadRuntime("DigTerrainWorkSession.cs"),
            ReadRuntime("DigTerrainHauling.cs"),
            ReadRuntime("DigJobSession.cs"),
            ReadRuntime("DigBuildingBoxAssemblyCandidates.cs"),
            ReadRuntime("DigBuildingPackingExecution.cs"));
        string clock = ReadRuntime("DigGameHudCanvas.Clock.cs");
        string needsRuntime = ReadRuntime("DigTerrainWorkSession.ResidentNeeds.cs");

        Assert.Contains("IsScheduledForWork", viewModel);
        Assert.Contains(
            "IsAlive && IsScheduledForWork && AutomaticPlanningEnabled",
            viewModel);
        Assert.Contains("workingTime", candidates);
        Assert.Contains("automaticEatAllowed", candidates);
        Assert.Contains("automaticSleepAllowed && context.BedAvailable", candidates);
        Assert.Contains("automaticRestAllowed && context.RestAvailable", candidates);
        Assert.Contains("agent.AutomaticPlanningEnabled", candidates);
        Assert.Contains("agent.IsAvailableForAutomaticPlanning", runtime);
        Assert.Contains("HasAvailableAutomaticJob(agent)", needsRuntime);
        Assert.Contains("job.Status == JobStatus.Available", needsRuntime);
        Assert.Contains("bool work = IsInsideWorkWindow", clock);
        Assert.Contains("? new Color(0.96f, 0.50f, 0.12f, alpha)", clock);
        Assert.Contains(": new Color(0.26f, 0.56f, 0.88f, alpha)", clock);
    }

    [Fact]
    public void Overlay_play_mode_teardown_uses_unambiguous_unity_object_type()
    {
        string test = Read(
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "Issue14OverlayPlayModeTests.cs");

        Assert.Contains("UnityEngine.Object.DestroyImmediate(_root);", test);
        Assert.DoesNotContain("            Object.DestroyImmediate(_root);", test);
    }

    private static string ReadRuntime(string file)
    {
        return Read("unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime", file);
    }

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        for (int index = 0; index < parts.Length; index++)
        {
            path = Path.Combine(path, parts[index]);
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