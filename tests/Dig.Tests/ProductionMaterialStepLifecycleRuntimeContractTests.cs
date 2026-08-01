using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionMaterialStepLifecycleRuntimeContractTests
{
    [Fact]
    public void Runtime_routes_package_then_stock_workbench_and_per_step_deposit()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string zones = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingProductionZones.cs"));
        string lifecycle = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingProductionMaterialLifecycle.cs"));
        string application = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Dig.Application",
            "Production",
            "ProductionMaterialStepLifecycleUseCases.cs"));
        string domain = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Dig.Domain",
            "Production",
            "ProductionOrderState.MaterialSteps.cs"));
        string playMode = File.ReadAllText(Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "CampfireProductionRuntimePlayModeTests.cs"));

        Assert.Contains("ResolveProductionPackagePlacementTarget", zones + lifecycle);
        Assert.Contains("EnsureProductionOutputPackage", zones);
        Assert.Contains("StageProductionMaterialCommand", lifecycle);
        Assert.Contains("DepositProductionMaterialCommand", lifecycle);
        Assert.Contains("ProductionMaterialStepPhase.StagedOnWorkbench", lifecycle);
        Assert.Contains(
            "ProductionMaterialStepPhase.ProcessedAwaitingPackage",
            lifecycle);
        Assert.Contains("ConsumeReservedProductionUnit", application);
        Assert.Contains("production.StageMaterial", application);
        Assert.Contains("production.DepositProcessedMaterial", application);
        Assert.Contains("Only staged material can receive production work", domain);
        Assert.Contains("Package_workbench_processing_and_deposit", playMode);
        Assert.DoesNotContain("ConsumeReservedProductionUnit", ReadApplyHandler(root));
    }

    private static string ReadApplyHandler(string root)
    {
        return File.ReadAllText(Path.Combine(
            root,
            "src",
            "Dig.Application",
            "Production",
            "ProductionExecutionUseCases.cs"));
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
