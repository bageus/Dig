using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireSerializedProductionRuntimeContractTests
{
    [Fact]
    public void Building_operation_is_serialized_and_refill_is_planned_before_production()
    {
        string productionJob = Read(
            "src/Dig.Domain/Jobs/ProductionWorkJobDefinition.cs");
        string supplyJob = Read(
            "src/Dig.Domain/Jobs/BuildingSupplyJobDefinition.cs");
        string synchronization = ReadRuntime(
            "DigBuildingProductionSynchronization.cs");
        string deferred = ReadRuntime(
            "DigBuildingProductionDeferredSupply.cs");

        Assert.Contains("ReservationKey.ForDestination(BuildingId)", productionJob);
        Assert.Contains("ReservationKey.ForPosition(WorkPosition)", productionJob);
        Assert.Contains("ReservationKey.ForDestination(BuildingId)", supplyJob);
        Assert.DoesNotContain("ReservationKey.ForPosition(WorkPosition)", supplyJob);
        Assert.True(
            synchronization.IndexOf(
                "CreateEligibleSupplyJobs(tick, agents, navigation)",
                StringComparison.Ordinal)
            < synchronization.IndexOf(
                "PrepareEligibleProductionOrders(tick, navigation)",
                StringComparison.Ordinal));
        Assert.Contains("HasNonTerminalProductionWorkJob", synchronization);
        Assert.Contains("HasNonTerminalProductionWorkJob", deferred);
    }

    [Fact]
    public void Completion_returns_to_work_position_before_job_terminal()
    {
        string definition = Read(
            "src/Dig.Domain/Jobs/ProductionWorkJobDefinition.cs");
        string completion = Read(
            "src/Dig.Application/Production/ProductionCompletionUseCase.cs");
        string lifecycle = ReadRuntime(
            "DigBuildingProductionMaterialLifecycle.cs");
        string zones = ReadRuntime("DigBuildingProductionZones.cs");

        Assert.Contains("JobStageKind.TravelToDestination", definition);
        Assert.Contains("could not enter its return stage", completion);
        Assert.Contains(
            "job.Stage == JobStageKind.TravelToDestination",
            lifecycle);
        Assert.Contains(
            "current?.Stage == JobStageKind.TravelToDestination",
            zones);
        Assert.Contains("production.WorkPosition", zones);
    }

    [Fact]
    public void Demo_uses_one_tick_workbench_and_processed_carry_projection()
    {
        string initialization = ReadRuntime(
            "DigBuildingProductionExecution.cs");
        string presenter = Read(
            "src/Dig.Presentation.Abstractions/Production/BuildingProductionPresenter.cs");
        string renderer = ReadRuntime(
            "DigBuildingInternalStockRenderer.Zones.cs");
        string equipment = ReadRuntime("DigResidentEquipment.cs");

        Assert.Contains(
            "CampfireProductionContent.TestProductionMaterialTicks",
            initialization);
        Assert.Contains("showWorkbench", presenter);
        Assert.Contains("Production Log Workbench", renderer);
        Assert.Contains("Destroy(collider)", renderer);
        Assert.Contains(
            "ProductionMaterialStepPhase.ProcessedAwaitingPackage",
            equipment);
        Assert.Contains("production-carry:", equipment);
    }

    [Fact]
    public void Successful_ingress_normalizes_after_releasing_slot_claim()
    {
        string transit = Read(
            "src/Dig.Domain/Inventory/InventoryState.HaulingTransit.cs");
        int release = transit.IndexOf(
            "ReleaseResidentSlotClaims(jobId, tick)",
            StringComparison.Ordinal);
        int normalize = transit.IndexOf(
            "NormalizeResidentInventory(residentId, tick)",
            StringComparison.Ordinal);

        Assert.True(release >= 0);
        Assert.True(normalize > release);
    }

    private static string ReadRuntime(string file)
    {
        return Read(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/" + file);
    }

    private static string Read(string relative)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relative));
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
