using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentWorkRatePresenterTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId StackId = Id(2);
    private static readonly ItemId PickaxeId = new ItemId("demo.tool.pickaxe");
    private static readonly ItemId HammerId = new ItemId("demo.tool.hammer");

    [Fact]
    public void Pickaxe_accelerates_mining_only()
    {
        InventoryState inventory = CreateInventory();
        Equip(inventory, PickaxeId);

        ResidentWorkRateViewModel model = CreatePresenter().Present(
            new[] { ResidentId.ToString() },
            inventory.CreateSnapshot())[0];

        Assert.Equal(PickaxeId.ToString(), model.EquippedItemId);
        Assert.Equal(1, model.MiningIntervalTicks);
        Assert.Equal(2, model.ConstructionIntervalTicks);
        Assert.Equal(3d, model.MiningSpeedMultiplier);
        Assert.Equal(1d, model.ConstructionSpeedMultiplier);
    }

    [Fact]
    public void Hammer_accelerates_construction_only()
    {
        InventoryState inventory = CreateInventory();
        Equip(inventory, HammerId);

        ResidentWorkRateViewModel model = CreatePresenter().Present(
            new[] { ResidentId.ToString() },
            inventory.CreateSnapshot())[0];

        Assert.Equal(HammerId.ToString(), model.EquippedItemId);
        Assert.Equal(3, model.MiningIntervalTicks);
        Assert.Equal(1, model.ConstructionIntervalTicks);
        Assert.Equal(1d, model.MiningSpeedMultiplier);
        Assert.Equal(2d, model.ConstructionSpeedMultiplier);
    }

    [Fact]
    public void Empty_slot_uses_base_intervals()
    {
        ResidentWorkRateViewModel model = CreatePresenter().Present(
            new[] { ResidentId.ToString() },
            CreateInventory().CreateSnapshot())[0];

        Assert.Null(model.EquippedItemId);
        Assert.Equal(3, model.MiningIntervalTicks);
        Assert.Equal(2, model.ConstructionIntervalTicks);
    }

    [Theory]
    [InlineData(0, 2, true)]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    [InlineData(3, 1, true)]
    public void Work_cadence_is_deterministic(long tick, int interval, bool expected)
    {
        Assert.Equal(expected, EquipmentWorkCadence.IsDue(tick, interval));
    }

    private static ResidentWorkRatePresenter CreatePresenter()
    {
        return new ResidentWorkRatePresenter(
            new EquipmentRates(new[]
            {
                new EquipmentProfile(
                    PickaxeId,
                    EquipmentAppearanceKind.Mining,
                    EquipmentWorkKind.Mining,
                    workIntervalTicks: 1),
                new EquipmentProfile(
                    HammerId,
                    EquipmentAppearanceKind.Construction,
                    EquipmentWorkKind.Construction,
                    workIntervalTicks: 1),
            }),
            miningBaseIntervalTicks: 3,
            constructionBaseIntervalTicks: 2);
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(PickaxeId, "Pickaxe", 1, isTool: true),
            new ItemDefinition(HammerId, "Hammer", 1, isTool: true),
        }));
    }

    private static void Equip(InventoryState inventory, ItemId itemId)
    {
        Assert.True(inventory.AddStack(
            StackId,
            itemId,
            quantity: 1,
            ItemLocation.InAgent(ResidentId),
            tick: 0).IsSuccess);
        Assert.True(inventory.EquipTool(StackId, ResidentId, tick: 1).IsSuccess);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}