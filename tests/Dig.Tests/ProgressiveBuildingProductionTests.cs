using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ProgressiveBuildingProductionTests
{
    [Fact]
    public void Each_material_step_consumes_one_unit_at_skill_resolved_duration()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(100);
        EntityId orderId = CampfireProductionTestHarness.Id(100);
        EntityId jobId = CampfireProductionTestHarness.Id(101);
        harness.AddBuildingStock(CampfireProductionContent.MushroomLegItemId, 2, 110);
        harness.AddBuildingStock(CampfireProductionContent.MushroomCapItemId, 1, 111);
        harness.GrantSkill(AgentSkillCatalog.Woodworking, 25);
        Assert.True(harness.Enqueue(
            orderId,
            CampfireProductionContent.TentRecipeId,
            1).IsSuccess);
        Assert.True(harness.Prepare(jobId, 2).IsSuccess);
        harness.ClaimBeginAndReachWork(orderId, jobId, 3);

        ProductionOrderSnapshot started = harness.Production.Get(orderId)!;
        Assert.All(started.MaterialSteps, step => Assert.Equal(75, step.RequiredTicks));
        Assert.True(harness.Work(orderId, jobId, elapsedTicks: 74, tick: 6).IsSuccess);
        Assert.Equal(2, harness.Inventory.GetTotal(CampfireProductionContent.MushroomLegItemId));
        Assert.True(harness.Work(orderId, jobId, elapsedTicks: 1, tick: 7).IsSuccess);
        Assert.Equal(1, harness.Inventory.GetTotal(CampfireProductionContent.MushroomLegItemId));
        Assert.Equal(ProductionOrderStatus.InProgress, harness.Production.Get(orderId)!.Status);

        Assert.True(harness.Work(orderId, jobId, elapsedTicks: 150, tick: 8).IsSuccess);
        Assert.Equal(0, harness.Inventory.GetTotal(CampfireProductionContent.MushroomLegItemId));
        Assert.Equal(0, harness.Inventory.GetTotal(CampfireProductionContent.MushroomCapItemId));
        Assert.Equal(ProductionOrderStatus.ReadyToComplete, harness.Production.Get(orderId)!.Status);
        Assert.Equal(JobStageKind.Finalize, harness.Jobs.Get(jobId)!.Stage);
    }

    [Fact]
    public void Cooking_output_and_skill_grant_are_per_order_not_per_output_unit()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        EntityId orderId = CampfireProductionTestHarness.Id(120);
        EntityId jobId = CampfireProductionTestHarness.Id(121);
        EntityId outputId = CampfireProductionTestHarness.Id(122);
        harness.AddBuildingStock(CampfireProductionContent.MushroomCapItemId, 1, 123);
        Assert.True(harness.Enqueue(
            orderId,
            CampfireProductionContent.GrilledMushroomRecipeId,
            1).IsSuccess);
        Assert.True(harness.Prepare(jobId, 2).IsSuccess);
        harness.ClaimBeginAndReachWork(orderId, jobId, 3);
        Assert.True(harness.Work(orderId, jobId, 1, 6).IsSuccess);
        CellId outputCell = new CellId(4, 2, 0);

        Assert.True(harness.Complete(
            orderId,
            jobId,
            outputId,
            outputCell,
            7).IsSuccess);

        ItemStackSnapshot output = harness.Inventory.GetStack(outputId)!;
        Assert.Equal(2, output.Quantity);
        Assert.Equal(ItemLocation.InWorld(outputCell), output.Location);
        Assert.Equal(120, harness.Agents.Get(CampfireProductionTestHarness.WorkerId)!
            .CreateSnapshot(7)
            .GetSkillLevel(AgentSkillCatalog.Cooking));
        Assert.True(harness.Complete(
            orderId,
            jobId,
            CampfireProductionTestHarness.Id(124),
            outputCell,
            8).IsFailure);
        Assert.Equal(120, harness.Agents.Get(CampfireProductionTestHarness.WorkerId)!
            .CreateSnapshot(8)
            .GetSkillLevel(AgentSkillCatalog.Cooking));
    }

    [Fact]
    public void Active_order_keeps_enabled_replenishment_planning_to_capacity()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        EntityId orderId = CampfireProductionTestHarness.Id(130);
        EntityId jobId = CampfireProductionTestHarness.Id(131);
        CellId sourceCell = new CellId(1, 1, 0);
        harness.AddBuildingStock(CampfireProductionContent.MushroomCapItemId, 1, 132);
        Assert.True(harness.Inventory.AddStack(
            CampfireProductionTestHarness.Id(133),
            CampfireProductionContent.MushroomCapItemId,
            3,
            ItemLocation.InWorld(sourceCell),
            0).IsSuccess);
        harness.Enqueue(orderId, CampfireProductionContent.GrilledMushroomRecipeId, 1);
        harness.Prepare(jobId, 2);
        BuildingSupplySnapshot supply = harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!;

        Assert.NotEmpty(BuildingSupplyPlanner.Plan(
            supply,
            harness.Inventory.GetAvailableWorldStacks(),
            new[] { sourceCell },
            new[] { sourceCell },
            new CellId(4, 3, 0),
            4)
            .Allocations);

        harness.ClaimBeginAndReachWork(orderId, jobId, 3);
        harness.Work(orderId, jobId, 1, 6);
        harness.Complete(
            orderId,
            jobId,
            CampfireProductionTestHarness.Id(134),
            new CellId(4, 2, 0),
            7);
        supply = harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!;
        Assert.NotEmpty(BuildingSupplyPlanner.Plan(
            supply,
            harness.Inventory.GetAvailableWorldStacks(),
            new[] { sourceCell },
            new[] { sourceCell },
            new CellId(4, 3, 0),
            4)
            .Allocations);
    }
}

}
