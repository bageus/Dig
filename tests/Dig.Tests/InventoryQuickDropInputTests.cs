using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryQuickDropInputTests
{
    private static readonly EntityId Resident = Id(1);
    private static readonly EntityId Stack = Id(2);
    private static readonly CellId ResidentCell = new CellId(3, 4, 0);
    private readonly ContextInputRouter _router = new ContextInputRouter();

    [Fact]
    public void C_left_click_drops_available_non_box_stack_at_resident_cell()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            clickCount: 1,
            altPressed: false,
            dropPressed: true,
            isBuildingBox: false);

        Assert.Equal(ApplicationInputCommandKind.DropInventoryStack, decision.CommandKind);
        Assert.Equal(Resident, decision.ActorId);
        Assert.Equal(Stack, decision.TargetEntityId);
        Assert.Equal(ResidentCell, decision.TargetCell);
    }

    [Fact]
    public void Double_left_click_without_C_does_not_quick_drop()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            clickCount: 2,
            altPressed: false,
            dropPressed: false,
            isBuildingBox: false);

        Assert.False(decision.HasApplicationCommand);
    }

    [Fact]
    public void Right_click_does_not_quick_drop()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Right,
            clickCount: 1,
            altPressed: false,
            dropPressed: true,
            isBuildingBox: false);

        Assert.False(decision.HasApplicationCommand);
    }

    [Fact]
    public void Building_box_C_left_click_uses_profile_enabled_exact_stack_quick_drop()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            clickCount: 1,
            altPressed: false,
            dropPressed: true,
            isBuildingBox: true);

        Assert.Equal(ApplicationInputCommandKind.DropInventoryStack, decision.CommandKind);
        Assert.Equal(Resident, decision.ActorId);
        Assert.Equal(Stack, decision.TargetEntityId);
        Assert.Equal(ResidentCell, decision.TargetCell);
    }

    [Fact]
    public void Alt_use_precedes_C_quick_drop()
    {
        ContextInputDecision decision = Route(
            PointerButtonKind.Left,
            clickCount: 1,
            altPressed: true,
            dropPressed: true,
            isBuildingBox: false);

        Assert.Equal(ApplicationInputCommandKind.UseInventoryItem, decision.CommandKind);
    }

    private ContextInputDecision Route(
        PointerButtonKind button,
        int clickCount,
        bool altPressed,
        bool dropPressed,
        bool isBuildingBox)
    {
        return _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                button,
                clickCount,
                altPressed,
                isPointerOverBlockingUi: false,
                dropPressed),
            new ContextInputState(
                selectedResidentId: Resident,
                selectedInventoryStackId: Stack,
                selectedInventoryItemUsable: true,
                selectedInventoryItemIsBuildingBox: isBuildingBox,
                canUseSelectedInventoryItem: true,
                canDropSelectedInventoryItem: true),
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                Stack,
                ResidentCell));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}