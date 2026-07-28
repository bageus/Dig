using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CampfireFoodCompletionPlayModeTests
{
    [Test]
    public void Full_output_ring_defers_then_retry_commits_exactly_once()
    {
        CampfireFoodProductionPlayModeHarness harness =
            new CampfireFoodProductionPlayModeHarness();
        EntityId orderId = Id(100);
        EntityId jobId = Id(101);
        EntityId outputId = Id(102);
        harness.AddBuildingStock(
            CampfireProductionContent.MushroomCapItemId,
            quantity: 1,
            id: 103);
        harness.ReadyOrder(orderId, jobId, tick: 1);
        MaterialId air = new MaterialId("terrain.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(12, 12),
            chunkSize: 4,
            materials,
            air,
            explored: true).Value;
        Dig.Domain.Buildings.BuildingSnapshot building = harness.Buildings.Get(
            CampfireFoodProductionPlayModeHarness.BuildingId)!;
        CellId[] candidates = ProductionOutputPlacement
            .CreateCandidates(building, maximumLateralDistance: 0)
            .ToArray();
        for (int index = 0; index < candidates.Length; index++)
        {
            Require(harness.Inventory.AddStack(
                Id(200 + index),
                CampfireProductionContent.StoneItemId,
                1,
                ItemLocation.InWorld(candidates[index]),
                tick: 6));
        }

        Result<CellId> blocked = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            harness.Inventory.CreateSnapshot().Stacks,
            maximumLateralDistance: 0);

        Assert.That(blocked.IsFailure, Is.True);
        Assert.That(blocked.Error, Is.EqualTo(ProductionErrors.OutputSpaceUnavailable));
        Assert.That(
            harness.Production.Get(orderId)!.Status,
            Is.EqualTo(ProductionOrderStatus.ReadyToComplete));
        Assert.That(
            harness.Inventory.GetTotal(CampfireProductionContent.GrilledMushroomItemId),
            Is.EqualTo(0));

        EntityId firstBlocker = Id(200);
        EntityId removalOwner = Id(299);
        Require(harness.Inventory.ReserveQuantity(
            firstBlocker,
            removalOwner,
            1,
            tick: 7));
        Require(harness.Inventory.ConsumeReserved(
            removalOwner,
            firstBlocker,
            1,
            tick: 7));
        Result<CellId> available = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            harness.Inventory.CreateSnapshot().Stacks,
            maximumLateralDistance: 0);
        Assert.That(available.IsSuccess, Is.True, available.Error?.ToString());
        Assert.That(available.Value, Is.EqualTo(candidates[0]));

        Require(harness.Complete(
            orderId,
            jobId,
            outputId,
            available.Value,
            tick: 8));
        Result duplicate = harness.Complete(
            orderId,
            jobId,
            Id(300),
            available.Value,
            tick: 9);

        Assert.That(duplicate.IsFailure, Is.True);
        Assert.That(harness.Inventory.GetStack(outputId)!.Quantity, Is.EqualTo(2));
        Assert.That(
            harness.Inventory.GetTotal(CampfireProductionContent.GrilledMushroomItemId),
            Is.EqualTo(2));
        Assert.That(
            harness.Production.Get(orderId)!.Status,
            Is.EqualTo(ProductionOrderStatus.Completed));
    }

    [Test]
    public void Repeated_orders_complete_and_cancelled_use_pickup_keeps_food()
    {
        CampfireFoodProductionPlayModeHarness harness =
            new CampfireFoodProductionPlayModeHarness();
        harness.AddBuildingStock(
            CampfireProductionContent.MushroomCapItemId,
            quantity: 2,
            id: 400);
        EntityId firstOrder = Id(401);
        EntityId secondOrder = Id(402);
        Require(harness.Enqueue(firstOrder, tick: 1));
        Require(harness.Enqueue(secondOrder, tick: 2));
        harness.ReadyQueuedOrder(firstOrder, Id(403), tick: 3);
        Require(harness.Complete(
            firstOrder,
            Id(403),
            Id(404),
            new CellId(4, 2, 0),
            tick: 10));
        harness.ReadyQueuedOrder(secondOrder, Id(405), tick: 11);
        Require(harness.Complete(
            secondOrder,
            Id(405),
            Id(406),
            new CellId(3, 3, 0),
            tick: 18));

        Assert.That(
            harness.Inventory.GetTotal(CampfireProductionContent.GrilledMushroomItemId),
            Is.EqualTo(4));
        Assert.That(
            harness.Production.Get(firstOrder)!.Status,
            Is.EqualTo(ProductionOrderStatus.Completed));
        Assert.That(
            harness.Production.Get(secondOrder)!.Status,
            Is.EqualTo(ProductionOrderStatus.Completed));

        EntityId foodId = Id(407);
        EntityId pickupJobId = Id(408);
        CellId foodCell = new CellId(1, 1, 0);
        Require(harness.Inventory.AddStack(
            foodId,
            CampfireProductionContent.GrilledMushroomItemId,
            1,
            ItemLocation.InWorld(foodCell),
            tick: 19));
        CreateWorldItemPickupHandler create = new CreateWorldItemPickupHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal);
        Require(create.Handle(new CreateWorldItemPickupCommand(
            pickupJobId,
            foodId,
            CampfireFoodProductionPlayModeHarness.WorkerId,
            foodCell,
            priority: 700,
            tick: 20,
            completionAction: WorldItemPickupCompletionAction.UseConsumable)));
        Assert.That(
            ((WorldItemPickupJobDefinition)harness.Jobs.Get(pickupJobId)!.Definition)
                .CompletionAction,
            Is.EqualTo(WorldItemPickupCompletionAction.UseConsumable));

        Require(new CancelWorldItemPickupHandler(
            harness.InventoryRepository,
            harness.JobsRepository,
            harness.Journal).Handle(new CancelWorldItemPickupCommand(
                pickupJobId,
                "player_cancelled",
                tick: 21)));

        Assert.That(harness.Jobs.Get(pickupJobId)!.Status, Is.EqualTo(JobStatus.Cancelled));
        Assert.That(harness.Inventory.GetStack(foodId)!.AvailableQuantity, Is.EqualTo(1));
        Assert.That(harness.Inventory.GetResidentSlotClaims(pickupJobId), Is.Empty);
    }

    private static void Require(Result result) =>
        CampfireFoodProductionPlayModeHarness.Require(result);

    private static EntityId Id(int value) =>
        CampfireFoodProductionPlayModeHarness.Id(value);
}

}
