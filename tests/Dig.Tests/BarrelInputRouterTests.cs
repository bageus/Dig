using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class BarrelInputRouterTests
{
    private static readonly EntityId ResidentId =
        EntityId.Parse("e1000000000000000000000000000001");
    private static readonly EntityId BarrelId =
        EntityId.Parse("e2000000000000000000000000000001");
    private static readonly CellId BarrelCell = new CellId(8, 4, 1);

    [Fact]
    public void Selected_resident_routes_reachable_barrel_to_one_attack_command()
    {
        ContextInputDecision decision = new ContextInputRouter().Route(
            new ContextPointerEvent(PointerInputSurface.World, PointerButtonKind.Left),
            new ContextInputState(selectedResidentId: ResidentId),
            new ContextPointerTarget(
                ContextWorldTargetKind.Barrel,
                BarrelId,
                BarrelCell,
                reachable: true));

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(ApplicationInputCommandKind.AttackBarrel, decision.CommandKind);
        Assert.Equal(ResidentId, decision.ActorId);
        Assert.Equal(BarrelId, decision.TargetEntityId);
        Assert.Equal(BarrelCell, decision.TargetCell);
        Assert.Null(decision.ReasonCode);
    }

    [Fact]
    public void Barrel_without_selected_resident_is_consumed_without_attack_command()
    {
        ContextInputDecision decision = new ContextInputRouter().Route(
            new ContextPointerEvent(PointerInputSurface.World, PointerButtonKind.Left),
            new ContextInputState(),
            new ContextPointerTarget(
                ContextWorldTargetKind.Barrel,
                BarrelId,
                BarrelCell,
                reachable: true));

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(ApplicationInputCommandKind.None, decision.CommandKind);
        Assert.Equal("input.barrel.resident_required", decision.ReasonCode);
    }

    [Fact]
    public void Unreachable_barrel_never_falls_through_to_move_or_excavation()
    {
        ContextInputDecision decision = new ContextInputRouter().Route(
            new ContextPointerEvent(PointerInputSurface.World, PointerButtonKind.Left),
            new ContextInputState(
                selectedResidentId: ResidentId,
                excavationTool: ExcavationToolKind.Tunnel),
            new ContextPointerTarget(
                ContextWorldTargetKind.Barrel,
                BarrelId,
                BarrelCell,
                reachable: false));

        Assert.True(decision.ConsumesPointer);
        Assert.Equal(ApplicationInputCommandKind.None, decision.CommandKind);
        Assert.Equal("input.barrel.unreachable_or_unavailable", decision.ReasonCode);
    }
}

}