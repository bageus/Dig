using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class UnifiedItemInteractionPlayModeTests
{
    [Test]
    public void New_content_definitions_resolve_interactions_without_unity_item_id_rules()
    {
        ItemDefinition material = new ItemDefinition(
            new ItemId("material.new_test"),
            "New test material",
            maximumStackSize: 20,
            isTool: false);
        ItemDefinition food = new ItemDefinition(
            new ItemId("meal.no_prefix_required"),
            "No-prefix meal",
            maximumStackSize: 5,
            isTool: false,
            foodUse: new ItemFoodUseDefinition(900, 3));
        ItemDefinition box = new ItemDefinition(
            new ItemId("packed.no_prefix_required"),
            "No-prefix building box",
            maximumStackSize: 1,
            isTool: false,
            categories: new[] { ItemInteractionCategoryIds.BuildingBox });
        ItemDefinition club = CombatEquipmentContent.CreateItems()[0];

        Assert.That(
            material.Interactions.WorldPrimaryAction,
            Is.EqualTo(ItemWorldInteractionAction.Pickup));
        Assert.That(
            food.Interactions.WorldAltAction,
            Is.EqualTo(ItemWorldInteractionAction.DirectUse));
        Assert.That(
            box.Interactions.WorldPrimaryAction,
            Is.EqualTo(ItemWorldInteractionAction.SelectBuildingBox));
        Assert.That(box.Interactions.InventoryQuickDropAllowed, Is.True);
        Assert.That(
            club.Interactions.InventoryPrimaryAction,
            Is.EqualTo(ItemInventoryInteractionAction.PlaceItem));
        Assert.That(club.Interactions.InventoryQuickDropAllowed, Is.True);
    }

    [Test]
    public void First_item_click_is_consumed_before_ground_fallback()
    {
        EntityId resident = Id(1);
        EntityId stack = Id(2);
        CellId cell = new CellId(3, 4, 0);
        ContextInputDecision decision = new ContextInputRouter().Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left),
            new ContextInputState(
                selectedResidentId: resident,
                selectedResidentAlive: true),
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stack,
                cell,
                reachable: false,
                itemActionAvailable: false,
                itemInteractionAction: ItemWorldInteractionAction.Pickup));

        Assert.That(decision.ConsumesPointer, Is.True);
        Assert.That(decision.CommandKind, Is.EqualTo(ApplicationInputCommandKind.None));
        Assert.That(
            decision.Effects.HasFlag(PresentationInputEffect.ShowReason),
            Is.True);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
