using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentEquipmentDomainTests
{
    private static readonly ItemId ToolItemId = new ItemId("tool.pickaxe");
    private static readonly ItemId HammerItemId = new ItemId("tool.hammer");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId OtherResidentId = Id(4);
    private static readonly EntityId FirstStackId = Id(2);
    private static readonly EntityId SecondStackId = Id(3);

    [Fact]
    public void World_tool_cannot_be_equipped()
    {
        InventoryState inventory = CreateInventory();
        CellId cell = new CellId(2, 3);
        Assert.True(inventory.AddStack(
            FirstStackId,
            ToolItemId,
            1,
            ItemLocation.InWorld(cell),
            tick: 0).IsSuccess);

        Result result = inventory.EquipTool(FirstStackId, ResidentId, tick: 1);

        Assert.Equal(InventoryErrors.ToolNotCarried, result.Error);
        Assert.Equal(ItemLocation.InWorld(cell), inventory.GetStack(FirstStackId)!.Location);
    }

    [Fact]
    public void Foreign_carried_tool_cannot_be_equipped()
    {
        InventoryState inventory = CreateInventory();
        AddCarriedTool(inventory, FirstStackId, ResidentId: OtherResidentId);

        Result result = inventory.EquipTool(FirstStackId, ResidentId, tick: 1);

        Assert.Equal(InventoryErrors.ToolNotCarried, result.Error);
        Assert.Equal(
            ItemLocation.InAgent(OtherResidentId),
            inventory.GetStack(FirstStackId)!.Location);
    }

    [Fact]
    public void Aggregate_rejects_a_second_tool_slot()
    {
        InventoryState inventory = CreateInventory();
        AddCarriedTool(inventory, FirstStackId, ResidentId);
        AddCarriedTool(inventory, SecondStackId, ResidentId);

        Result first = inventory.EquipTool(FirstStackId, ResidentId, tick: 1);
        Result second = inventory.EquipTool(SecondStackId, ResidentId, tick: 2);

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(InventoryErrors.ToolSlotOccupied, second.Error);
        Assert.Equal(
            ItemLocation.InAgent(ResidentId),
            inventory.GetStack(SecondStackId)!.Location);
    }

    [Fact]
    public void Presenter_rejects_duplicate_slots_across_inventory_snapshots()
    {
        InventoryState first = CreateInventory();
        InventoryState second = CreateInventory();
        AddCarriedTool(first, FirstStackId, ResidentId);
        AddCarriedTool(second, SecondStackId, ResidentId);
        Assert.True(first.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);
        Assert.True(second.EquipTool(SecondStackId, ResidentId, tick: 1).IsSuccess);

        Assert.Throws<InvalidOperationException>(() =>
            new ResidentEquipmentPresenter().Present(
                first.CreateSnapshot(),
                second.CreateSnapshot()));
    }

    [Fact]
    public void Equipped_mining_profile_reduces_work_interval()
    {
        InventoryState inventory = CreateInventory();
        AddCarriedTool(inventory, FirstStackId, ResidentId);
        Assert.True(inventory.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);
        EquipmentRates rates = CreateRates();

        int interval = rates.ResolveIntervalTicks(
            ResidentId,
            EquipmentWorkKind.Mining,
            baseIntervalTicks: 3,
            inventory.CreateSnapshot());

        Assert.Equal(1, interval);
        Assert.Equal(
            EquipmentAppearanceKind.Mining,
            rates.ResolveAppearance(ToolItemId));
    }

    [Fact]
    public void Mismatched_profile_keeps_base_interval()
    {
        InventoryState inventory = CreateInventory();
        AddCarriedTool(inventory, FirstStackId, ResidentId);
        Assert.True(inventory.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);

        int interval = CreateRates().ResolveIntervalTicks(
            ResidentId,
            EquipmentWorkKind.Construction,
            baseIntervalTicks: 3,
            inventory.CreateSnapshot());

        Assert.Equal(3, interval);
        Assert.Equal(
            EquipmentAppearanceKind.Construction,
            CreateRates().ResolveAppearance(HammerItemId));
    }

    [Fact]
    public void Empty_equipment_keeps_base_interval()
    {
        int interval = CreateRates().ResolveIntervalTicks(
            ResidentId,
            EquipmentWorkKind.Mining,
            baseIntervalTicks: 3,
            CreateInventory().CreateSnapshot());

        Assert.Equal(3, interval);
    }

    [Fact]
    public void Rates_reject_duplicate_slots_across_inventory_snapshots()
    {
        InventoryState first = CreateInventory();
        InventoryState second = CreateInventory();
        AddCarriedTool(first, FirstStackId, ResidentId);
        AddCarriedTool(second, SecondStackId, ResidentId);
        Assert.True(first.EquipTool(FirstStackId, ResidentId, tick: 1).IsSuccess);
        Assert.True(second.EquipTool(SecondStackId, ResidentId, tick: 1).IsSuccess);

        Assert.Throws<InvalidOperationException>(() =>
            CreateRates().ResolveIntervalTicks(
                ResidentId,
                EquipmentWorkKind.Mining,
                baseIntervalTicks: 3,
                first.CreateSnapshot(),
                second.CreateSnapshot()));
    }

    private static EquipmentRates CreateRates()
    {
        return new EquipmentRates(new[]
        {
            new EquipmentProfile(
                ToolItemId,
                EquipmentAppearanceKind.Mining,
                EquipmentWorkKind.Mining,
                workIntervalTicks: 1),
            new EquipmentProfile(
                HammerItemId,
                EquipmentAppearanceKind.Construction,
                EquipmentWorkKind.Construction,
                workIntervalTicks: 1),
        });
    }

    private static void AddCarriedTool(
        InventoryState inventory,
        EntityId stackId,
        EntityId ResidentId)
    {
        Assert.True(inventory.AddStack(
            stackId,
            ToolItemId,
            1,
            ItemLocation.InAgent(ResidentId),
            tick: 0).IsSuccess);
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(ToolItemId, "Pickaxe", 1, isTool: true),
        }));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
