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
    public void Resident_uses_one_free_slot_for_a_full_available_material_stack()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness();
        for (int slot = 0; slot < ResidentInventoryLayoutSnapshot.MainSlotCount - 1; slot++)
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
                new[] { CampfireProductionTestHarness.Id(192) },
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
        ResidentInventorySlotClaimSnapshot claim = Assert.Single(
            harness.Inventory.GetResidentSlotClaims(jobId));
        Assert.Equal(4, claim.Quantity);

        Assert.True(harness.Jobs.Start(jobId, 2).IsSuccess);
        Assert.True(harness.Jobs.AdvanceStage(jobId, 2).IsSuccess);
        Result acquired = new AcquireBuildingSupplySourceHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new AcquireBuildingSupplySourceCommand(
                jobId,
                sourceId,
                3));

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        ItemStackSnapshot carried = harness.Inventory.GetResidentInventoryLayout(
                CampfireProductionTestHarness.WorkerId)
            .Slots
            .Where(value => value.ItemId
                == Dig.Domain.Content.CampfireProductionContent.MushroomCapItemId)
            .Select(value => harness.Inventory.GetStack(value.StackId!.Value)!)
            .Single();
        Assert.Equal(4, carried.Quantity);
    }
}

}
