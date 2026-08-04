using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentWorkToolUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_uses_typed_job_tools_and_restores_inventory_equipment()
    {
        string root = FindRepositoryRoot();
        string presentation = Path.Combine(root, "src",
            "Dig.Presentation.Abstractions", "Jobs");
        string runtime = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Runtime");
        string playMode = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Tests", "PlayMode");

        string model = Read(presentation, "JobOverlayViewModel.cs");
        string presenter = Read(presentation, "JobOverlayPresenter.cs");
        string facing = Read(runtime, "DigAgentRenderer.WorkFacing.cs");
        string visual = Read(runtime, "DigAgentVisual.HandTools.cs");
        string equipment = Read(runtime, "DigAgentEquipmentVisual.cs");
        string scenario = Read(playMode, "ResidentWorkToolVisualsPlayModeTests.cs");

        Assert.Contains("ResidentWorkToolVisualKind", model);
        Assert.Contains("Pickaxe", model);
        Assert.Contains("Axe", model);
        Assert.Contains("Hammer", model);
        Assert.Contains("job.Stage != JobStageKind.PerformWork", presenter);
        Assert.Contains("DigJobDefinition => ResidentWorkToolVisualKind.Pickaxe", presenter);
        Assert.Contains("SpatialDigJobDefinition => ResidentWorkToolVisualKind.Pickaxe", presenter);
        Assert.Contains("MushroomChopJobDefinition => ResidentWorkToolVisualKind.Axe", presenter);
        Assert.Contains("BuildingWorkKind.Construction", presenter);
        Assert.Contains("BuildingBoxAssemblyJobDefinition => ResidentWorkToolVisualKind.Hammer", presenter);
        Assert.Contains("BuildingBoxPackingJobDefinition => ResidentWorkToolVisualKind.Hammer", presenter);
        Assert.DoesNotContain("ProductionWorkJobDefinition => ResidentWorkToolVisualKind.Hammer", presenter);

        Assert.Contains("Dictionary<string, ResidentWorkToolVisualKind>", facing);
        Assert.Contains("workTool == ResidentWorkToolVisualKind.Hammer", facing);
        Assert.Contains("ResidentWorkToolVisualKind.None", facing);
        Assert.Contains("_equipmentModel?.ItemId", visual);
        Assert.Contains("ResolveSocket(DigResidentSocketKind.RightHand)", visual);
        Assert.Contains("RefreshHandEquipment()", visual);

        Assert.Contains("weapon.club", equipment);
        Assert.Contains("Club Head", equipment);
        Assert.Contains("Pickaxe Head", equipment);
        Assert.Contains("Axe Blade", equipment);
        Assert.Contains("Hammer Head", equipment);
        Assert.Contains("Destroy(collider)", equipment);

        Assert.Contains(
            "Right_hand_switches_club_pickaxe_axe_hammer_club_and_empty",
            scenario);
        Assert.Contains("visual.Clear()", scenario);
        Assert.Contains("GetComponentsInChildren<Collider>(true)", scenario);
        Assert.Contains(
            "UnityEngine.Object.DestroyImmediate(_root)",
            scenario);
        Assert.Contains(
            "UnityEngine.Object.DestroyImmediate(_material)",
            scenario);
        Assert.DoesNotContain(
            "\n            Object.DestroyImmediate(",
            scenario);
    }

    [Fact]
    public void Hand_visual_rebuild_invalidates_stale_hover_renderers()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Runtime");
        string playMode = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Tests", "PlayMode");

        string hover = Read(runtime, "DigAgentVisual.HoverLifecycle.cs");
        string hands = Read(runtime, "DigAgentVisual.HandTools.cs");
        string equipment = Read(runtime, "DigAgentEquipmentVisual.cs");
        string scenario = Read(playMode, "ResidentWorkToolVisualsPlayModeTests.cs");

        Assert.Contains("PrepareHoverForRendererMutation", hover);
        Assert.Contains("CompleteHoverRendererMutation", hover);
        Assert.Contains("InvalidateHoverRendererCache", hover);
        Assert.Contains("if (renderer == null", hover);
        Assert.Contains("PrepareHoverForRendererMutation()", hands);
        Assert.Contains("CompleteHoverRendererMutation(reapplyHover)", hands);
        Assert.Contains("CompleteHoverRendererMutation(refreshHover)", hands);
        Assert.Contains("child.SetParent(null, worldPositionStays: true)", equipment);
        Assert.Contains("child.gameObject.SetActive(false)", equipment);
        Assert.Contains(
            "Hover_survives_hand_visual_rebuild_without_destroyed_renderer_access",
            scenario);
        Assert.Contains("LogAssert.NoUnexpectedReceived()", scenario);
    }

    private static string Read(string root, string file)
    {
        string path = Path.Combine(root, file);
        Assert.True(File.Exists(path), path);
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
