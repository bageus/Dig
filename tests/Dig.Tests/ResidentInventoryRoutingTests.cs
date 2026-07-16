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
        ContextInputDecision decision = new ContextInputRouter().Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                PointerButtonKind.Left),
            new ContextInputState(
                selectedResidentId: residentId,
                selectedResidentAlive: true,
                selectedInventoryStackId: stackId,
                selectedInventoryItemIsBuildingBox: true),
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stackId,
                cell));

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(
            PresentationInputEffect.StartBuildingPlacement,
            decision.Effects);
        Assert.Equal(residentId, decision.ActorId);
        Assert.Equal(stackId, decision.TargetEntityId);
        Assert.Equal(cell, decision.TargetCell);
    }

    [Fact]
    public void Generic_slot_does_not_start_placement()
    {
        ContextInputDecision decision = new ContextInputRouter().Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                PointerButtonKind.Left),
            new ContextInputState(
                selectedResidentId: Id(1),
                selectedInventoryStackId: Id(2)),
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                Id(2)));

        Assert.False(decision.ConsumesPointer);
        Assert.Equal(PresentationInputEffect.None, decision.Effects);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
