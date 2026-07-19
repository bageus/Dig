using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryDiagnosticsTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId BasketStackId = Id(2);
    private static readonly EntityId ToolStackId = Id(3);
    private static readonly EntityId JobId = Id(4);
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly ItemId ToolId = new ItemId("tool.pickaxe");
    private static readonly ItemId OreId = new ItemId("ore.iron");

    [Fact]
    public void Diagnostics_expose_layout_held_speed_and_slot_claims()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            new ItemDefinition(ToolId, "Pickaxe", 1, true, new[] { weapon }),
            new ItemDefinition(
                BasketId,
                "Basket",
                1,
                false,
                new[] { raw },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Cargo,
                    tier: 1,
                    addedSlots: 4,
                    acceptedCategories: new[] { raw },
                    moveSpeedMultiplierWhenOccupied: 0.75d,
                    visualAttachmentId: "visual.basket")),
        }));
        Assert.True(inventory.AddStack(
            BasketStackId,
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            ToolStackId,
            ToolId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1),
            tick: 0).IsSuccess);
        Assert.True(inventory.EquipTool(ToolStackId, ResidentId, tick: 1).IsSuccess);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            JobId,
            ResidentId,
            OreId,
            12,
            tick: 2).IsSuccess);

        ResidentInventoryDiagnosticViewModel model =
            new ResidentInventoryDiagnosticsPresenter().Present(inventory, ResidentId);

        Assert.Equal(6, model.MainCapacity);
        Assert.Equal(4, model.CargoCapacity);
        Assert.Equal(0, model.WeaponCapacity);
        Assert.Equal(BasketId.ToString(), model.ActiveCargoExpansionItemId);
        Assert.Equal(ToolStackId.ToString(), model.HeldStackId);
        Assert.Equal(1d, model.MoveSpeedMultiplier);
        ResidentInventorySlotDiagnosticViewModel held = Assert.Single(
            model.Slots,
            slot => slot.StackId == ToolStackId.ToString());
        Assert.Equal(1, held.HeldQuantity);
        ResidentInventorySlotDiagnosticViewModel claimed = Assert.Single(
            model.Slots,
            slot => slot.IncomingClaimQuantity == 12);
        Assert.Equal(ResidentInventoryCompartment.Cargo, claimed.Compartment);
        Assert.Equal(JobId.ToString(), Assert.Single(claimed.ClaimJobIds));
    }

    [Fact]
    public void Occupied_cargo_reports_active_speed_penalty()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            new ItemDefinition(
                BasketId,
                "Basket",
                1,
                false,
                new[] { raw },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Cargo,
                    tier: 1,
                    addedSlots: 4,
                    acceptedCategories: new[] { raw },
                    moveSpeedMultiplierWhenOccupied: 0.75d,
                    visualAttachmentId: "visual.basket")),
        }));
        Assert.True(inventory.AddStack(
            BasketStackId,
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(9),
            OreId,
            3,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);

        ResidentInventoryDiagnosticViewModel model =
            new ResidentInventoryDiagnosticsPresenter().Present(inventory, ResidentId);

        Assert.Equal(0.75d, model.MoveSpeedMultiplier);
        Assert.Equal(3, model.Slots.Sum(slot => slot.Quantity));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}