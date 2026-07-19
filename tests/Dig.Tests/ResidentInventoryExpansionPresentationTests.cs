using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryExpansionPresentationTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId LargeBasketStackId = Id(2);
    private static readonly EntityId BasketStackId = Id(3);
    private static readonly EntityId HarnessStackId = Id(4);
    private static readonly EntityId CargoStackId = Id(5);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly ItemId LargeBasketId =
        new ItemId("inventory.large_basket");
    private static readonly ItemId HarnessId =
        new ItemId("inventory.weapon_harness");

    [Fact]
    public void Active_loaded_expansion_exposes_tooltip_and_spill_warning_data()
    {
        InventoryState inventory = CreateInventory(withCargo: true);

        ResidentInventoryExpansionFeedbackViewModel model =
            Assert.NotNull(new ResidentInventoryExpansionFeedbackPresenter().Present(
                inventory,
                ResidentId,
                LargeBasketStackId));

        Assert.Equal(InventoryExpansionGroup.Cargo, model.Group);
        Assert.Equal(2, model.Tier);
        Assert.Equal(6, model.AddedSlots);
        Assert.Equal(35, model.SpeedPenaltyPercent);
        Assert.Equal("visual.resident.large_basket", model.VisualAttachmentId);
        Assert.Contains("raw", model.AcceptedCategories);
        Assert.True(model.IsActive);
        Assert.Equal(1, model.OccupiedSlotCount);
        Assert.True(model.RequiresSpillConfirmation);
    }

    [Fact]
    public void Inactive_expansion_never_requests_compartment_spill_confirmation()
    {
        InventoryState inventory = CreateInventory(withCargo: true);

        ResidentInventoryExpansionFeedbackViewModel model =
            Assert.NotNull(new ResidentInventoryExpansionFeedbackPresenter().Present(
                inventory,
                ResidentId,
                BasketStackId));

        Assert.False(model.IsActive);
        Assert.False(model.RequiresSpillConfirmation);
        Assert.Equal(4, model.AddedSlots);
        Assert.Equal(25, model.SpeedPenaltyPercent);
    }

    [Fact]
    public void Empty_cargo_hides_basket_but_keeps_active_weapon_attachment()
    {
        InventoryState inventory = CreateInventory(withCargo: false);

        ResidentInventoryAttachmentViewModel[] models =
            new ResidentInventoryAttachmentPresenter().Present(inventory).ToArray();

        ResidentInventoryAttachmentViewModel weapon = Assert.Single(models);
        Assert.Equal(InventoryExpansionGroup.Weapon, weapon.Group);
        Assert.Equal(HarnessId.ToString(), weapon.ItemId);
        Assert.Equal("visual.resident.weapon_harness", weapon.VisualAttachmentId);
    }

    [Fact]
    public void Occupied_cargo_shows_highest_tier_basket_alongside_weapon_attachment()
    {
        InventoryState inventory = CreateInventory(withCargo: true);

        ResidentInventoryAttachmentViewModel[] models =
            new ResidentInventoryAttachmentPresenter().Present(inventory).ToArray();

        Assert.Equal(2, models.Length);
        ResidentInventoryAttachmentViewModel cargo = Assert.Single(
            models,
            model => model.Group == InventoryExpansionGroup.Cargo);
        Assert.Equal(LargeBasketId.ToString(), cargo.ItemId);
        Assert.Equal(2, cargo.Tier);
        Assert.Equal("visual.resident.large_basket", cargo.VisualAttachmentId);
        Assert.Single(
            models,
            model => model.Group == InventoryExpansionGroup.Weapon);
    }

    private static InventoryState CreateInventory(bool withCargo)
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            Expansion(
                BasketId,
                "Basket",
                InventoryExpansionGroup.Cargo,
                tier: 1,
                slots: 4,
                speed: 0.75d,
                "visual.resident.basket",
                raw),
            Expansion(
                LargeBasketId,
                "Large basket",
                InventoryExpansionGroup.Cargo,
                tier: 2,
                slots: 6,
                speed: 0.65d,
                "visual.resident.large_basket",
                raw),
            Expansion(
                HarnessId,
                "Weapon harness",
                InventoryExpansionGroup.Weapon,
                tier: 2,
                slots: 4,
                speed: 1d,
                "visual.resident.weapon_harness",
                weapon),
        }));
        Assert.True(inventory.AddStack(
            LargeBasketStackId,
            LargeBasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            BasketStackId,
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            HarnessStackId,
            HarnessId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                2),
            tick: 0).IsSuccess);
        if (withCargo)
        {
            Assert.True(inventory.AddStack(
                CargoStackId,
                OreId,
                3,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Cargo,
                    0),
                tick: 0).IsSuccess);
        }

        return inventory;
    }

    private static ItemDefinition Expansion(
        ItemId id,
        string name,
        InventoryExpansionGroup group,
        int tier,
        int slots,
        double speed,
        string visual,
        ItemCategoryId accepted)
    {
        return new ItemDefinition(
            id,
            name,
            1,
            false,
            new[] { accepted },
            new InventoryExpansionDefinition(
                group,
                tier,
                slots,
                new[] { accepted },
                speed,
                visual));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
