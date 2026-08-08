using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireProductionStallUnityContractTests
{
    [Fact]
    public void Production_work_advances_each_simulation_tick_at_the_workstation()
    {
        string lifecycle = ReadRuntime(
            "DigBuildingProductionMaterialLifecycle.cs");

        Assert.Contains("if (!At(worker, production.WorkPosition)", lifecycle);
        Assert.Contains("|| !IsAtPreciseWorkPose(job, worker)", lifecycle);
        Assert.DoesNotContain("tick % 2", lifecycle);
        Assert.Contains("ApplyProductionWorkCommand", lifecycle);
        Assert.Contains("ProductionMaterialStepPhase.Processing", lifecycle);
    }

    [Fact]
    public void Production_jobs_project_a_build_work_pose()
    {
        string presenter = Read(
            "src",
            "Dig.Presentation.Abstractions",
            "Jobs",
            "JobOverlayPresenter.cs");
        string model = Read(
            "src",
            "Dig.Presentation.Abstractions",
            "Jobs",
            "JobOverlayViewModel.cs");
        string renderer = ReadRuntime("DigAgentRenderer.WorkFacing.cs");
        string visual = ReadRuntime("DigAgentVisual.WorkFacing.cs");

        Assert.Contains("ProductionWorkJobDefinition production", presenter);
        Assert.Contains("isProductionWork", presenter);
        Assert.Contains("IsProductionWork", model);
        Assert.Contains("job.IsProductionWork", renderer);
        Assert.Contains("animateBuildWork", renderer + visual);
        Assert.Contains("ResidentActionVisualState.Build", visual);
    }

    [Fact]
    public void PlayMode_covers_material_transit_progress_and_package_close()
    {
        string playMode = Read(
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "CampfireProductionRuntimePlayModeTests.cs");

        Assert.Contains("InitializeBuildingProductionDemo", playMode);
        Assert.Contains("sawPackageBeforeAcquire", playMode);
        Assert.Contains("sawAcquire", playMode);
        Assert.Contains("sawStagedWithoutCarry", playMode);
        Assert.Contains("sawProcessing", playMode);
        Assert.Contains("sawProcessedAwaitingPackage", playMode);
        Assert.Contains("sawDeposited", playMode);
        Assert.Contains("ProductionOrderStatus.Completed", playMode);
        Assert.Contains("package.IsClosed", playMode);
    }

    private static string ReadRuntime(string file)
    {
        return Read(
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            file);
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
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "unity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
