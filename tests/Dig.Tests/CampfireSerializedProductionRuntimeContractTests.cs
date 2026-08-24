using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireSerializedProductionRuntimeContractTests
{
    [Fact]
    public void Building_operation_uses_threshold_targeted_supply_without_blocking_on_extraction()
    {
        string productionJob = Read(
            "src/Dig.Domain/Jobs/ProductionWorkJobDefinition.cs");
        string supplyJob = Read(
            "src/Dig.Domain/Jobs/BuildingSupplyJobDefinition.cs");
        string operationTurn = Read(
            "src/Dig.Domain/Production/BuildingSupplyState.OperationTurn.cs");
        string synchronization = ReadRuntime(
            "DigBuildingProductionSynchronization.cs");
        string deferred = ReadRuntime(
            "DigBuildingProductionDeferredSupply.cs");

        Assert.Contains("ReservationKey.ForDestination(BuildingId)", productionJob);
        Assert.Contains("ReservationKey.ForPosition(WorkPosition)", productionJob);
        Assert.Contains("ReservationKey.ForDestination(BuildingId)", supplyJob);
        Assert.DoesNotContain("ReservationKey.ForPosition(WorkPosition)", supplyJob);
        Assert.Contains(
            "ShouldYieldSupplyTurnToRunnableProduction",
            synchronization);
        Assert.Contains(
            "BuildingSupplyQueuePolicy.ShouldAttemptSupplyBeforeProduction",
            deferred);
        Assert.Contains(
            "targetItemIds: queued?.Recipe.Inputs",
            synchronization);
        Assert.Contains(
            "HasNonTerminalResolvedBuildingSupplyJob",
            synchronization);
        Assert.Contains(
            "BuildingOperationTurn.Production",
            operationTurn);
        Assert.Contains(
            "BuildingOperationTurn.Supply",
            ReadRuntime("DigBuildingProductionZones.cs"));
        Assert.Contains("HasNonTerminalProductionWorkJob", synchronization);
        Assert.Contains("HasNonTerminalProductionWorkJob", deferred);
        Assert.Contains("supply.IsSourceResolved", deferred);
        Assert.Contains(
            "BuildingSupplyPlanner.PlanForItems",
            Read("src/Dig.Application/Production/BuildingSupplyUseCases.cs"));
        string playMode = Read(
            "Assets/Dig.Unity/Tests/PlayMode/ActiveProductionBuildingSupplyPlayModeTests.cs");
        Assert.Contains(
            "Three_cooking_cycles_run_before_half_stock_refill_then_production_resumes",
            playMode);
        Assert.Contains("supplyStartedAfterCompletedUnits", playMode);
    }

    [Fact]
    public void Completion_returns_to_work_position_before_job_terminal()
    {
        string definition = Read(
            "src/Dig.Domain/Jobs/ProductionWorkJobDefinition.cs");
        string completion = Read(
            "src/Dig.Application/Production/ProductionCompletionUseCase.cs");
        string lifecycle = ReadRuntime("DigBuildingProductionRuntime.cs");
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
    public void Live_demo_uses_authoritative_material_cadence_and_processed_carry_projection()
    {
        string initialization = ReadRuntime(
            "DigBuildingProductionExecution.cs");
        string content = Read(
            "src/Dig.Domain/Content/CampfireProductionContent.cs");
        string presenter = Read(
            "src/Dig.Presentation.Abstractions/Production/BuildingProductionPresenter.cs");
        string renderer = ReadRuntime(
            "DigBuildingInternalStockRenderer.Zones.cs");
        string equipment = ReadRuntime("DigResidentEquipment.cs");

        Assert.Contains(
            "CampfireProductionContent.ProductionMaterialTicks",
            initialization);
        Assert.Contains("public const long ProductionMaterialTicks = 25", content);
        Assert.Contains("public const long CookingMaterialTicks = ProductionMaterialTicks * 2", content);
        Assert.Contains("showWorkbench", presenter);
        Assert.Contains("Production Log Workbench", renderer);
        Assert.Contains("Destroy(collider)", renderer);
        Assert.Contains(
            "ProductionMaterialStepPhase.ProcessedAwaitingPackage",
            equipment);
        Assert.Contains("production-carry:", equipment);
    }

    [Fact]
    public void Successful_ingress_reflows_before_validation_and_normalizes_after_release()
    {
        string transit = Read(
            "src/Dig.Domain/Inventory/InventoryState.HaulingTransit.cs");
        int preflightNormalize = transit.IndexOf(
            "NormalizeResidentInventory(residentId, tick)",
            StringComparison.Ordinal);
        int claimValidation = transit.IndexOf(
            "ResidentInventorySlotClaimSnapshot[] claims",
            StringComparison.Ordinal);
        int release = transit.IndexOf(
            "ReleaseResidentSlotClaims(jobId, tick)",
            StringComparison.Ordinal);
        int postTransferNormalize = transit.LastIndexOf(
            "NormalizeResidentInventory(residentId, tick)",
            StringComparison.Ordinal);

        Assert.True(preflightNormalize >= 0);
        Assert.True(claimValidation > preflightNormalize);
        Assert.True(release >= 0);
        Assert.True(postTransferNormalize > release);
    }

    private static string ReadRuntime(string file)
    {
        return Read(
            "Assets/Dig.Unity/Runtime/" + file);
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
