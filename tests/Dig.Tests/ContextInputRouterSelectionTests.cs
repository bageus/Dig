using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class ContextInputRouterSelectionTests
{
    private static readonly EntityId Resident = Id(10);
    private static readonly EntityId OtherResident = Id(11);
    private static readonly EntityId Building = Id(12);
    private static readonly CellId Cell = new CellId(7, 8);
    private readonly ContextInputRouter _router = new ContextInputRouter();

    [Fact]
    public void World_pointer_over_blocking_ui_never_reaches_world_routing()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left,
                isPointerOverBlockingUi: true),
            new ContextInputState(
                selectedResidentId: Resident,
                excavationTool: ExcavationToolKind.Tunnel),
            new ContextPointerTarget(
                ContextWorldTargetKind.HostileResident,
                OtherResident,
                Cell,
                reachable: true));

        Assert.True(decision.ConsumesPointer);
        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.None, decision.Effects);
        Assert.Equal("input.ui_shielded", decision.ReasonCode);
    }

    [Fact]
    public void Right_click_cancels_placement_before_deselect_or_excavation()
    {
        ContextInputDecision decision = RightWorld(
            new ContextInputState(
                selectedResidentId: Resident,
                buildingPlacementActive: true,
                excavationTool: ExcavationToolKind.Room));

        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.CancelBuildingPlacement, decision.Effects);
    }

    [Fact]
    public void Right_click_with_resident_deselects_and_never_excavates()
    {
        ContextInputDecision decision = RightWorld(
            new ContextInputState(
                selectedResidentId: Resident,
                excavationTool: ExcavationToolKind.Room));

        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.DeselectResident, decision.Effects);
        Assert.Equal(Resident, decision.ActorId);
    }

    [Fact]
    public void Right_click_without_resident_preserves_excavation_mode()
    {
        ContextInputDecision decision = RightWorld(
            new ContextInputState(excavationTool: ExcavationToolKind.Tunnel));

        Assert.True(decision.HasApplicationCommand);
        Assert.Equal(ApplicationInputCommandKind.ApplyExcavation, decision.CommandKind);
        Assert.Equal(ExcavationToolKind.Tunnel, decision.ExcavationTool);
    }

    [Fact]
    public void Roster_double_click_selects_and_focuses_without_application_command()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentRoster,
                PointerButtonKind.Left,
                clickCount: 2,
                isPointerOverBlockingUi: true),
            new ContextInputState(),
            new ContextPointerTarget(
                ContextWorldTargetKind.Resident,
                Resident,
                Cell));

        Assert.True(decision.ConsumesPointer);
        Assert.False(decision.HasApplicationCommand);
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.SelectResident));
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.FocusResident));
        Assert.Equal(Resident, decision.TargetEntityId);
    }

    [Fact]
    public void Friendly_resident_click_selects_target_instead_of_moving_selected_resident()
    {
        ContextInputDecision decision = _router.Route(
            WorldLeft(),
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.Resident,
                OtherResident,
                Cell,
                reachable: true));

        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.SelectResident, decision.Effects);
        Assert.Equal(OtherResident, decision.TargetEntityId);
    }

    [Fact]
    public void Dead_target_returns_reason_without_attack_or_selection()
    {
        ContextInputDecision decision = _router.Route(
            WorldLeft(),
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.HostileResident,
                OtherResident,
                Cell,
                reachable: true,
                isAlive: false));

        Assert.False(decision.HasApplicationCommand);
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.ShowReason));
        Assert.Equal("input.target.stale_or_dead", decision.ReasonCode);
    }

    [Fact]
    public void Stale_selected_resident_is_deselected_before_world_command()
    {
        ContextInputDecision decision = _router.Route(
            WorldLeft(),
            new ContextInputState(
                selectedResidentId: Resident,
                selectedResidentAlive: false,
                excavationTool: ExcavationToolKind.Erase),
            Ground(reachable: true));

        Assert.False(decision.HasApplicationCommand);
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.DeselectResident));
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.ShowReason));
        Assert.Equal("input.selected_resident.stale_or_dead", decision.ReasonCode);
    }

    [Fact]
    public void Unreachable_ground_keeps_selection_and_exposes_reason()
    {
        ContextInputDecision decision = _router.Route(
            WorldLeft(),
            new ContextInputState(selectedResidentId: Resident),
            Ground(reachable: false));

        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.ShowReason, decision.Effects);
        Assert.Equal("input.move.unreachable", decision.ReasonCode);
        Assert.Equal(Resident, decision.ActorId);
    }

    [Fact]
    public void Completed_building_and_ground_have_typed_local_selection_effects()
    {
        ContextInputDecision building = _router.Route(
            WorldLeft(),
            new ContextInputState(),
            new ContextPointerTarget(
                ContextWorldTargetKind.CompletedBuilding,
                Building,
                Cell));
        ContextInputDecision ground = _router.Route(
            WorldLeft(),
            new ContextInputState(),
            Ground(reachable: true));

        Assert.Equal(PresentationInputEffect.SelectBuilding, building.Effects);
        Assert.Equal(Building, building.TargetEntityId);
        Assert.Equal(PresentationInputEffect.SelectGround, ground.Effects);
        Assert.Equal(Cell, ground.TargetCell);
    }

    [Fact]
    public void Panel_modes_are_mutually_exclusive_with_stable_precedence()
    {
        Assert.Equal(
            ContextPanelMode.ExcavationPalette,
            _router.ResolvePanelMode(new ContextInputState()));
        Assert.Equal(
            ContextPanelMode.BuildingFunctions,
            _router.ResolvePanelMode(new ContextInputState(
                selectedCompletedBuildingId: Building)));
        Assert.Equal(
            ContextPanelMode.ResidentInventory,
            _router.ResolvePanelMode(new ContextInputState(
                selectedResidentId: Resident,
                selectedCompletedBuildingId: Building)));
        Assert.Equal(
            ContextPanelMode.BuildingPlacement,
            _router.ResolvePanelMode(new ContextInputState(
                selectedResidentId: Resident,
                selectedCompletedBuildingId: Building,
                buildingPlacementActive: true)));
    }

    private ContextInputDecision RightWorld(ContextInputState state)
    {
        return _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Right),
            state,
            Ground(reachable: true));
    }

    private static ContextPointerEvent WorldLeft()
    {
        return new ContextPointerEvent(
            PointerInputSurface.World,
            PointerButtonKind.Left);
    }

    private static ContextPointerTarget Ground(bool reachable)
    {
        return new ContextPointerTarget(
            ContextWorldTargetKind.Ground,
            cell: Cell,
            reachable: reachable);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}