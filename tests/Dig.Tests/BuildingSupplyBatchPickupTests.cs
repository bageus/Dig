using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyBatchPickupTests
{
    [Fact]
    public void Resident_uses_four_free_slots_for_four_available_material_items()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        for (int slot = 0; slot < ResidentInventoryLayoutSnapshot.MainSlotCount - 4; slot++)
        {
            Assert.True(harness.Inventory.AddStack(
                CampfireProductionTestHarness.Id(180 + slot),
                Dig.Domain.Content.CampfireProductionContent.StoneItemId,
                1,
                ItemLocation.InResidentSlot(
                    CampfireProductionTestHarness.WorkerId,
                    ResidentInventoryCompartment.Main,
                    slot),
                0).IsSuccess);
        }

        CellId sourceCell = new CellId(1, 1, 0);
        EntityId sourceId = CampfireProductionTestHarness.Id(190);
        EntityId jobId = CampfireProductionTestHarness.Id(191);
        Assert.True(harness.Inventory.AddStack(
            sourceId,
            Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(sourceCell),
            0).IsSuccess);

        Result created = new CreateBuildingSupplyJobHandler(
            harness.Content,
            harness.SupplyRepository,
            harness.ProductionRepository,
            harness.BuildingsRepository,
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CreateBuildingSupplyJobCommand(
                jobId,
                CampfireProductionTestHarness.BuildingId,
                CampfireProductionTestHarness.WorkerId,
                new[] { sourceCell },
                new[]
                {
                    sourceCell,
                    harness.Buildings.Get(CampfireProductionTestHarness.BuildingId)!
                        .WorkPosition,
                },
                new[]
                {
                    CampfireProductionTestHarness.Id(192),
                    CampfireProductionTestHarness.Id(194),
                    CampfireProductionTestHarness.Id(195),
                    CampfireProductionTestHarness.Id(196),
                },
                new[] { CampfireProductionTestHarness.Id(193) },
                priority: 500,
                tick: 1,
                targetItemIds: new[]
                {
                    Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId,
                }));

        Assert.True(created.IsSuccess, created.Error?.ToString());
        BuildingSupplyJobDefinition definition = Assert.IsType<BuildingSupplyJobDefinition>(
            harness.Jobs.Get(jobId)!.Definition);
        Assert.Equal(4, Assert.Single(definition.Allocations).Quantity);
        ResidentInventorySlotClaimSnapshot[] claims = harness.Inventory
            .GetResidentSlotClaims(jobId)
            .ToArray();
        Assert.Equal(4, claims.Length);
        Assert.All(claims, claim => Assert.Equal(1, claim.Quantity));
        Assert.Equal(4, claims.Select(claim => claim.Slot).Distinct().Count());
        Result normalizedBeforePickup = harness.Inventory.NormalizeResidentInventory(
            CampfireProductionTestHarness.WorkerId,
            tick: 2);
        Assert.True(
            normalizedBeforePickup.IsSuccess,
            normalizedBeforePickup.Error?.ToString());
        Assert.Equal(4, harness.Inventory.GetResidentSlotClaims(jobId).Count);
        Assert.All(
            harness.Inventory.GetResidentSlotClaims(jobId),
            claim => Assert.Equal(1, claim.Quantity));

        Assert.True(harness.Jobs.Start(jobId, 3).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(jobId, 3).IsSuccess);
        Result acquired = new AcquireBuildingSupplySourceHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new AcquireBuildingSupplySourceCommand(
                jobId,
                sourceId,
                4));

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        ItemStackSnapshot[] carried = harness.Inventory.GetResidentInventoryLayout(
                CampfireProductionTestHarness.WorkerId)
            .Slots
            .Where(value => value.ItemId
                == Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId)
            .Select(value => harness.Inventory.GetStack(value.StackId!.Value)!)
            .ToArray();
        Assert.Equal(4, carried.Length);
        Assert.All(carried, stack => Assert.Equal(1, stack.Quantity));
        Assert.All(carried, stack => Assert.Equal(1, stack.ReservedQuantity));
        Assert.Equal(4, carried.Select(stack => stack.Location).Distinct().Count());
        Result normalizedAfterPickup = harness.Inventory.NormalizeResidentInventory(
            CampfireProductionTestHarness.WorkerId,
            tick: 5);
        Assert.True(
            normalizedAfterPickup.IsSuccess,
            normalizedAfterPickup.Error?.ToString());
        ItemStackSnapshot[] normalizedCarried = carried
            .Select(stack => harness.Inventory.GetStack(stack.StackId)!)
            .ToArray();
        Assert.All(normalizedCarried, stack => Assert.Equal(1, stack.Quantity));
        Assert.All(normalizedCarried, stack => Assert.Equal(1, stack.ReservedQuantity));
        Assert.Equal(
            carried.Select(stack => stack.Location).OrderBy(value => value.ToString()),
            normalizedCarried.Select(stack => stack.Location).OrderBy(value => value.ToString()));
    }
}

}
