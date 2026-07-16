using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryRoutingTests
{
    [Fact]
    public void Box_slot_starts_placement_at_resident_cell()
    {
        EntityId residentId = Id(1);
        EntityId stackId = Id(2);
        CellId cell = new CellId(4, 5);
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            residentId,
            stackId,
            cell,
            isBox: true);

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(PresentationInputEffect.StartBuildingPlacement, decision.Effects);
        Assert.Equal(residentId, decision.ActorId);
        Assert.Equal(stackId, decision.TargetEntityId);
        Assert.Equal(cell, decision.TargetCell);
    }

    [Fact]
    public void Alt_click_on_usable_slot_routes_use_command()
    {
        EntityId residentId = Id(1);
        EntityId stackId = Id(2);
        CellId cell = new CellId(4, 5);
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            residentId,
            stackId,
            cell,
            usable: true,
            canUse: true,
            altPressed: true);

        Assert.Equal(ApplicationInputCommandKind.UseInventoryItem, decision.CommandKind);
        Assert.Equal(residentId, decision.ActorId);
        Assert.Equal(stackId, decision.TargetEntityId);
        Assert.Equal(cell, decision.TargetCell);
    }

    [Fact]
    public void Right_click_on_available_slot_routes_drop_command()
    {
        EntityId residentId = Id(1);
        EntityId stackId = Id(2);
        CellId cell = new CellId(4, 5);
        ContextInputDecision decision = Route(
            PointerButtonKind.Right,
            residentId,
            stackId,
            cell,
            canDrop: true);

        Assert.Equal(ApplicationInputCommandKind.DropInventoryStack, decision.CommandKind);
        Assert.Equal(residentId, decision.ActorId);
        Assert.Equal(stackId, decision.TargetEntityId);
        Assert.Equal(cell, decision.TargetCell);
    }

    [Fact]
    public void Right_click_on_reserved_slot_is_consumed_with_reason()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Right,
            Id(1),
            Id(2),
            new CellId(4, 5),
            canDrop: false);

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(ApplicationInputCommandKind.None, decision.CommandKind);
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.ShowReason));
        Assert.Equal("input.inventory.stack_unavailable", decision.ReasonCode);
    }

    [Fact]
    public void Generic_left_click_does_not_start_placement()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            Id(1),
            Id(2),
            new CellId(4, 5));

        Assert.False(decision.ConsumesPointer);
        Assert.Equal(PresentationInputEffect.None, decision.Effects);
    }

    private static ContextInputDecision Route(
        PointerButtonKind button,
        EntityId residentId,
        EntityId stackId,
        CellId cell,
        bool isBox = false,
        bool usable = false,
        bool canUse = false,
        bool canDrop = false,
        bool altPressed = false)
    {
        return new ContextInputRouter().Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                button,
                altPressed: altPressed),
            new ContextInputState(
                selectedResidentId: residentId,
                selectedResidentAlive: true,
                selectedInventoryStackId: stackId,
                selectedInventoryItemUsable: usable,
                selectedInventoryItemIsBuildingBox: isBox,
                canUseSelectedInventoryItem: canUse,
                canDropSelectedInventoryItem: canDrop),
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stackId,
                cell));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
