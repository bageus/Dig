using System;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ItemStackVisualLayoutPresenterTests
{
    private readonly ItemStackVisualLayoutPresenter _presenter =
        new ItemStackVisualLayoutPresenter();

    [Theory]
    [InlineData(1, ItemStackQuantityBand.Single, 1)]
    [InlineData(2, ItemStackQuantityBand.Small, 2)]
    [InlineData(4, ItemStackQuantityBand.Small, 2)]
    [InlineData(5, ItemStackQuantityBand.Medium, 3)]
    [InlineData(9, ItemStackQuantityBand.Medium, 3)]
    [InlineData(10, ItemStackQuantityBand.Large, 4)]
    [InlineData(500, ItemStackQuantityBand.Large, 4)]
    public void Quantity_bands_use_bounded_visible_instance_counts(
        int quantity,
        ItemStackQuantityBand expectedBand,
        int expectedVisibleCount)
    {
        ItemStackVisualLayoutViewModel model = _presenter.Present(
            Stack("stack.a", "item.ore.iron", quantity));

        Assert.Equal(expectedBand, model.QuantityBand);
        Assert.Equal(expectedVisibleCount, model.VisibleInstanceCount);
        Assert.InRange(model.VisibleInstanceCount, 1, 4);
        Assert.Equal(quantity.ToString(), model.QuantityBadge);
    }

    [Fact]
    public void Identical_stack_snapshot_produces_identical_layout_and_version()
    {
        WorldItemViewModel snapshot = Stack(
            "stack.deterministic",
            "item.ore.gold",
            quantity: 8,
            reservedQuantity: 3,
            WorldItemInteractionKind.Pickup);

        ItemStackVisualLayoutViewModel first = _presenter.Present(snapshot);
        ItemStackVisualLayoutViewModel second = _presenter.Present(snapshot);

        Assert.Equal(first.LayoutVersion, second.LayoutVersion);
        Assert.Equal(first.Version, second.Version);
        Assert.Equal(first.Instances, second.Instances);
        Assert.Equal(ItemReservationVisualState.Partial, first.ReservationState);
    }

    [Fact]
    public void Quantity_changes_within_band_preserve_geometry_but_update_badge_version()
    {
        ItemStackVisualLayoutViewModel two = _presenter.Present(
            Stack("stack.band", "item.material.stone", quantity: 2));
        ItemStackVisualLayoutViewModel four = _presenter.Present(
            Stack("stack.band", "item.material.stone", quantity: 4));

        Assert.Equal(ItemStackQuantityBand.Small, two.QuantityBand);
        Assert.Equal(two.LayoutVersion, four.LayoutVersion);
        Assert.Equal(two.Instances, four.Instances);
        Assert.NotEqual(two.Version, four.Version);
        Assert.Equal("2", two.QuantityBadge);
        Assert.Equal("4", four.QuantityBadge);
    }

    [Theory]
    [InlineData(0, ItemReservationVisualState.None)]
    [InlineData(2, ItemReservationVisualState.Partial)]
    [InlineData(6, ItemReservationVisualState.Full)]
    public void Reservation_visual_state_uses_published_quantities(
        int reservedQuantity,
        ItemReservationVisualState expected)
    {
        ItemStackVisualLayoutViewModel model = _presenter.Present(
            Stack(
                "stack.reserved",
                "item.food.meal",
                quantity: 6,
                reservedQuantity));

        Assert.Equal(expected, model.ReservationState);
        Assert.Equal(6 - reservedQuantity, model.AvailableQuantity);
        Assert.Equal(3, model.VisibleInstanceCount);
    }

    [Fact]
    public void Reservation_change_does_not_relayout_the_stack()
    {
        ItemStackVisualLayoutViewModel available = _presenter.Present(
            Stack("stack.reserve.layout", "item.alcohol.ale", 12, 0));
        ItemStackVisualLayoutViewModel reserved = _presenter.Present(
            Stack("stack.reserve.layout", "item.alcohol.ale", 12, 12));

        Assert.Equal(available.LayoutVersion, reserved.LayoutVersion);
        Assert.Equal(available.Instances, reserved.Instances);
        Assert.NotEqual(available.Version, reserved.Version);
        Assert.Equal(ItemReservationVisualState.Full, reserved.ReservationState);
    }

    [Fact]
    public void Stack_identity_changes_deterministic_variant()
    {
        ItemStackVisualLayoutViewModel first = _presenter.Present(
            Stack("stack.variant.a", "item.equipment.pickaxe", 10));
        ItemStackVisualLayoutViewModel second = _presenter.Present(
            Stack("stack.variant.b", "item.equipment.pickaxe", 10));

        Assert.NotEqual(first.LayoutVersion, second.LayoutVersion);
        Assert.NotEqual(first.Instances, second.Instances);
    }

    [Fact]
    public void Invalid_world_item_facts_are_rejected_before_layout()
    {
        WorldItemViewModel invalid = Stack(
            "stack.invalid",
            "item.invalid",
            quantity: 0,
            reservedQuantity: 0);

        Assert.Throws<ArgumentException>(() => _presenter.Present(invalid));
    }

    private static WorldItemViewModel Stack(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity = 0,
        WorldItemInteractionKind interactionKind = WorldItemInteractionKind.None)
    {
        return new WorldItemViewModel(
            stackId,
            itemId,
            quantity,
            reservedQuantity,
            cellX: 4,
            cellY: 7,
            interactionKind);
    }
}
}
