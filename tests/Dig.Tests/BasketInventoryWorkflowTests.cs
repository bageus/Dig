using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{
public sealed class BasketInventoryWorkflowTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId BasketStackId = Id(2);
    private static readonly EntityId LargeBasketStackId = Id(3);
    private static readonly EntityId OreStackId = Id(4);
    private static readonly ItemId OreId = new ItemId("ore.basket_test");
    private static readonly ItemId FillerId = new ItemId("item.main_filler");
    private readonly InMemoryExecutionJournal _journal = new InMemoryExecutionJournal();
    private long _tick = 1;

    [Fact]
    public void Surface_basket_extends_cargo_only_after_pickup_and_loaded_main_falls_back_to_cargo()
    {
        InventoryState inventory = CreateInventory();
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();

        PickUp(inventories, jobs, BasketStackId, Id(10), new CellId(1, 0));

        ResidentInventoryLayoutViewModel emptyBasket = Present(inventory);
        Assert.Equal(4, emptyBasket.CargoCapacity);
        Assert.Equal(4, emptyBasket.GetCompartment(
            ResidentInventoryCompartment.Cargo).Count);
        Assert.Equal(1d, emptyBasket.MoveSpeedMultiplier);
        Assert.Empty(new ResidentInventoryAttachmentPresenter().Present(inventory));

        FillRemainingMainSlots(inventory);
        Assert.True(inventory.AddStack(
            OreStackId,
            OreId,
            quantity: 3,
            ItemLocation.InWorld(new CellId(3, 0)),
            tick: _tick++).IsSuccess);
        PickUp(inventories, jobs, OreStackId, Id(11), new CellId(3, 0));

        ResidentInventorySlotSnapshot[] cargo = inventory
            .GetResidentInventoryLayout(ResidentId)
            .Slots
            .Where(value => !value.IsEmpty
                && value.Slot.Compartment == ResidentInventoryCompartment.Cargo)
            .OrderBy(value => value.Slot.Index)
            .ToArray();
        Assert.Equal(3, cargo.Length);
        Assert.All(cargo, value => Assert.Equal(1, value.Quantity));
        Assert.Equal(new[] { 0, 1, 2 }, cargo.Select(value => value.Slot.Index));
        Assert.Equal(0.75d, inventory.GetResidentMoveSpeedMultiplier(ResidentId));
        ResidentInventoryAttachmentViewModel attachment = Assert.Single(
            new ResidentInventoryAttachmentPresenter().Present(inventory));
        Assert.Equal(ResidentInventoryExpansionContent.BasketItemId.ToString(),
            attachment.ItemId);

        CellId dropCell = new CellId(5, 0);
        Result dropped = inventory.DropResidentStackWithSpill(
            BasketStackId,
            ItemLocation.InWorld(dropCell),
            tick: _tick++);

        Assert.True(dropped.IsSuccess, dropped.Error?.ToString());
        Assert.Equal(0, Present(inventory).CargoCapacity);
        Assert.Equal(1d, inventory.GetResidentMoveSpeedMultiplier(ResidentId));
        Assert.Empty(new ResidentInventoryAttachmentPresenter().Present(inventory));
        Assert.Equal(ItemLocation.InWorld(dropCell),
            inventory.GetStack(BasketStackId)!.Location);
        Assert.Equal(ItemLocation.InWorld(dropCell),
            inventory.GetStack(OreStackId)!.Location);
        Assert.Equal(ItemLocation.InWorld(new CellId(2, 0)),
            inventory.GetStack(LargeBasketStackId)!.Location);
    }

    private void PickUp(
        InMemoryInventoryRepository inventories,
        InMemoryJobRepository jobs,
        EntityId stackId,
        EntityId jobId,
        CellId sourceCell)
    {
        Result created = new CreateWorldItemPickupHandler(
            inventories,
            jobs,
            _journal).Handle(new CreateWorldItemPickupCommand(
                jobId,
                stackId,
                ResidentId,
                sourceCell,
                priority: 675,
                tick: _tick++));
        Assert.True(created.IsSuccess, created.Error?.ToString());
        Advance(jobs, jobId);
        Advance(jobs, jobId);
        Result completed = new CompleteWorldItemPickupHandler(
            inventories,
            jobs,
            _journal).Handle(new CompleteWorldItemPickupCommand(jobId, _tick++));
        Assert.True(completed.IsSuccess, completed.Error?.ToString());
    }

    private void Advance(InMemoryJobRepository jobs, EntityId jobId)
    {
        Result advanced = new AdvanceJobHandler(jobs, _journal).Handle(
            new AdvanceJobCommand(jobId, _tick++));
        Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
    }

    private static InventoryState CreateInventory()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();
        ItemDefinition[] definitions = content.Items
            .Concat(new[]
            {
                new ItemDefinition(
                    OreId,
                    "Basket test ore",
                    100,
                    false,
                    new[] { ResidentInventoryExpansionContent.RawMaterialCategoryId }),
                new ItemDefinition(
                    FillerId,
                    "Main filler",
                    1,
                    false,
                    new[] { ResidentInventoryExpansionContent.GeneralItemCategoryId }),
            })
            .ToArray();
        InventoryState inventory = new InventoryState(new ItemCatalog(definitions));
        Assert.True(inventory.AddUnit(
            BasketStackId,
            ResidentInventoryExpansionContent.BasketItemId,
            ItemLocation.InWorld(new CellId(1, 0)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddUnit(
            LargeBasketStackId,
            ResidentInventoryExpansionContent.LargeBasketItemId,
            ItemLocation.InWorld(new CellId(2, 0)),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static void FillRemainingMainSlots(InventoryState inventory)
    {
        for (int slot = 1; slot < ResidentInventoryLayoutSnapshot.MainSlotCount; slot++)
        {
            Assert.True(inventory.AddUnit(
                Id(20 + slot),
                FillerId,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    slot),
                tick: slot).IsSuccess);
        }
    }

    private static ResidentInventoryLayoutViewModel Present(InventoryState inventory)
    {
        return new ResidentInventoryLayoutPresenter()
            .Present(inventory, ResidentId);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
