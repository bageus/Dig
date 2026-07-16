using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryPresenterTests
{
    private static readonly ItemId BoxItemId = new ItemId("building_box.workshop");
    private static readonly ItemId ToolItemId = new ItemId("tool.pickaxe");
    private static readonly ItemId RockItemId = new ItemId("rock.chunk");

    [Fact]
    public void Presents_only_selected_resident_stacks_with_actionable_items_first()
    {
        EntityId resident = Id(1);
        EntityId other = Id(2);
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            Id(10), RockItemId, 4, ItemLocation.InAgent(resident), tick: 1).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(11), ToolItemId, 1, ItemLocation.InAgent(resident), tick: 2).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(12), BoxItemId, 1, ItemLocation.InAgent(resident), tick: 3).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(13), BoxItemId, 1, ItemLocation.InAgent(other), tick: 4).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(14),
            BoxItemId,
            1,
            ItemLocation.InWorld(new Dig.Domain.World.CellId(3, 3)),
            tick: 5).IsSuccess);

        ResidentInventoryViewModel model = Presenter(inventory)
            .Present(inventory.CreateSnapshot(), resident);

        Assert.Equal(resident.ToString(), model.ResidentId);
        Assert.Equal(3, model.Slots.Count);
        Assert.True(model.Slots[0].IsBuildingBox);
        Assert.True(model.Slots[0].CanStartPlacement);
        Assert.True(model.Slots[0].CanDrop);
        Assert.True(model.Slots[1].IsTool);
        Assert.True(model.Slots[1].CanUse);
        Assert.True(model.Slots[1].CanDrop);
        Assert.Equal(ResidentInventoryItemKind.Generic, model.Slots[2].ItemKind);
        Assert.False(model.Slots[2].CanUse);
        Assert.True(model.Slots[2].CanDrop);
    }

    [Fact]
    public void Reserved_box_is_visible_but_cannot_start_or_drop()
    {
        EntityId resident = Id(1);
        EntityId stackId = Id(10);
        EntityId jobId = Id(20);
        InventoryState inventory = CreateInventory();
        Assert.True(inventory.AddStack(
            stackId,
            BoxItemId,
            quantity: 1,
            ItemLocation.InAgent(resident),
            tick: 1).IsSuccess);
        Assert.True(inventory.ReserveQuantity(stackId, jobId, quantity: 1, tick: 2).IsSuccess);

        ResidentInventorySlotViewModel slot = Presenter(inventory)
            .Present(inventory.CreateSnapshot(), resident)
            .Slots[0];

        Assert.Equal(1, slot.ReservedQuantity);
        Assert.Equal(0, slot.AvailableQuantity);
        Assert.False(slot.CanStartPlacement);
        Assert.False(slot.CanUse);
        Assert.False(slot.CanDrop);
    }

    [Fact]
    public void Empty_inventory_returns_stable_empty_model()
    {
        InventoryState inventory = CreateInventory();
        ResidentInventoryViewModel model = Presenter(inventory)
            .Present(inventory.CreateSnapshot(), Id(1));

        Assert.Empty(model.Slots);
        Assert.Equal(0, model.InventoryVersion);
    }

    private static ResidentInventoryPresenter Presenter(InventoryState inventory)
    {
        return new ResidentInventoryPresenter(BoxItemId, inventory.Catalog);
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                BoxItemId,
                "Workshop BuildingBox",
                maximumStackSize: 1,
                isTool: false),
            new ItemDefinition(
                ToolItemId,
                "Pickaxe",
                maximumStackSize: 1,
                isTool: true),
            new ItemDefinition(
                RockItemId,
                "Rock chunk",
                maximumStackSize: 100,
                isTool: false),
        }));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
