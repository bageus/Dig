using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class UnifiedItemInteractionRouterTests
{
    private readonly ContextInputRouter _router = new ContextInputRouter();
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId StackId = Id(2);
    private static readonly CellId Cell = new CellId(4, 5, 0);

    [Fact]
    public void Available_generic_item_first_lmb_creates_exact_pickup_command()
    {
        ContextInputDecision decision = Route(
            ItemWorldInteractionAction.Pickup,
            available: true,
            ContextWorldTargetKind.GenericItem);

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(ApplicationInputCommandKind.PickupWorldItem, decision.CommandKind);
        Assert.Equal(ResidentId, decision.ActorId);
        Assert.Equal(StackId, decision.TargetEntityId);
        Assert.Equal(Cell, decision.TargetCell);
    }

    [Fact]
    public void Rejected_item_click_is_consumed_and_cannot_fall_through_to_move()
    {
        ContextInputDecision decision = Route(
            ItemWorldInteractionAction.Pickup,
            available: false,
            ContextWorldTargetKind.GenericItem);

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(ApplicationInputCommandKind.None, decision.CommandKind);
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.ShowReason));
        Assert.Equal("input.world_item.unavailable", decision.ReasonCode);
    }

    [Fact]
    public void Building_box_primary_select_and_alt_resolved_pickup_remain_distinct()
    {
        ContextInputDecision selected = Route(
            ItemWorldInteractionAction.SelectBuildingBox,
            available: true,
            ContextWorldTargetKind.BuildingBox);
        ContextInputDecision pickup = Route(
            ItemWorldInteractionAction.Pickup,
            available: true,
            ContextWorldTargetKind.BuildingBox,
            altPressed: true);

        Assert.True(selected.Effects.HasFlag(PresentationInputEffect.SelectBuildingBox));
        Assert.Equal(ApplicationInputCommandKind.None, selected.CommandKind);
        Assert.Equal(ApplicationInputCommandKind.PickupBuildingBox, pickup.CommandKind);
    }

    [Fact]
    public void Definition_resolved_direct_use_creates_pickup_then_use_command()
    {
        ContextInputDecision decision = Route(
            ItemWorldInteractionAction.DirectUse,
            available: true,
            ContextWorldTargetKind.FoodItem,
            altPressed: true);

        Assert.Equal(ApplicationInputCommandKind.EatWorldItem, decision.CommandKind);
        Assert.Equal(StackId, decision.TargetEntityId);
    }

    private ContextInputDecision Route(
        ItemWorldInteractionAction action,
        bool available,
        ContextWorldTargetKind kind,
        bool altPressed = false)
    {
        return _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left,
                altPressed: altPressed),
            new ContextInputState(
                selectedResidentId: ResidentId,
                selectedResidentAlive: true),
            new ContextPointerTarget(
                kind,
                StackId,
                Cell,
                reachable: available,
                itemActionAvailable: available,
                itemInteractionAction: action));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
