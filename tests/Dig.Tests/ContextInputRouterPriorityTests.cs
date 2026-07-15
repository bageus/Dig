using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class ContextInputRouterPriorityTests
{
    private static readonly EntityId Resident = Id(1);
    private static readonly EntityId Stack = Id(2);
    private static readonly EntityId Target = Id(3);
    private static readonly CellId Cell = new CellId(5, 6);
    private readonly ContextInputRouter _router = new ContextInputRouter();

    [Fact]
    public void Valid_building_placement_has_highest_world_priority()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(
                selectedResidentId: Resident,
                selectedInventoryStackId: Stack,
                buildingPlacementActive: true,
                buildingPlacementValid: true,
                excavationTool: ExcavationToolKind.Tunnel),
            Ground(reachable: true));

        AssertCommand(decision, ApplicationInputCommandKind.ConfirmBuildingPlacement);
        Assert.Equal(Resident, decision.ActorId);
        Assert.Equal(Stack, decision.TargetEntityId);
        Assert.Equal(Cell, decision.TargetCell);
    }

    [Fact]
    public void Invalid_building_placement_keeps_preview_and_emits_no_command()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(
                selectedResidentId: Resident,
                buildingPlacementActive: true,
                buildingPlacementValid: false,
                buildingPlacementReasonCode: "placement.cell_blocked"),
            Ground(reachable: true));

        Assert.False(decision.HasApplicationCommand);
        Assert.True(decision.ConsumesPointer);
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.KeepBuildingPreview));
        Assert.True(decision.Effects.HasFlag(PresentationInputEffect.ShowReason));
        Assert.Equal("placement.cell_blocked", decision.ReasonCode);
    }

    [Fact]
    public void Alt_inventory_use_precedes_other_inventory_behavior()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                PointerButtonKind.Left,
                altPressed: true),
            new ContextInputState(
                selectedResidentId: Resident,
                selectedInventoryStackId: Stack,
                selectedInventoryItemUsable: true,
                selectedInventoryItemIsBuildingBox: true,
                canUseSelectedInventoryItem: true),
            new ContextPointerTarget(ContextWorldTargetKind.GenericItem, Stack));

        AssertCommand(decision, ApplicationInputCommandKind.UseInventoryItem);
        Assert.Equal(Stack, decision.TargetEntityId);
    }

    [Fact]
    public void Targeted_drop_precedes_resident_move()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(
                selectedResidentId: Resident,
                selectedInventoryStackId: Stack,
                excavationTool: ExcavationToolKind.Room),
            Ground(reachable: true));

        AssertCommand(decision, ApplicationInputCommandKind.DropInventoryStack);
        Assert.Equal(Stack, decision.TargetEntityId);
        Assert.Equal(Cell, decision.TargetCell);
    }

    [Fact]
    public void Supported_alt_building_box_creates_pickup_command()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left,
                altPressed: true),
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.BuildingBox,
                Target,
                Cell,
                reachable: true,
                supportsAltInteraction: true));

        AssertCommand(decision, ApplicationInputCommandKind.PickupBuildingBox);
        Assert.Equal(Target, decision.TargetEntityId);
    }

    [Fact]
    public void Unsupported_alt_building_box_falls_back_to_ground_move()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left,
                altPressed: true),
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.BuildingBox,
                Target,
                Cell,
                reachable: true,
                supportsAltInteraction: false));

        AssertCommand(decision, ApplicationInputCommandKind.MoveResident);
        Assert.Equal(Resident, decision.ActorId);
        Assert.Null(decision.TargetEntityId);
        Assert.Equal(Cell, decision.TargetCell);
    }

    [Fact]
    public void Plain_world_building_box_enters_placement_without_command()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.BuildingBox,
                Target,
                Cell));

        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.StartBuildingPlacement, decision.Effects);
        Assert.Equal(Target, decision.TargetEntityId);
    }

    [Fact]
    public void Inventory_building_box_enters_the_same_placement_effect()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                PointerButtonKind.Left),
            new ContextInputState(
                selectedResidentId: Resident,
                selectedInventoryStackId: Stack,
                selectedInventoryItemIsBuildingBox: true),
            new ContextPointerTarget(ContextWorldTargetKind.GenericItem, Stack));

        Assert.False(decision.HasApplicationCommand);
        Assert.Equal(PresentationInputEffect.StartBuildingPlacement, decision.Effects);
        Assert.Equal(Stack, decision.TargetEntityId);
    }

    [Fact]
    public void Hostile_target_precedes_movement()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.HostileResident,
                Target,
                Cell,
                reachable: true));

        AssertCommand(decision, ApplicationInputCommandKind.AttackTarget);
        Assert.Equal(Target, decision.TargetEntityId);
    }

    [Fact]
    public void Reachable_ground_creates_move_for_selected_resident()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(selectedResidentId: Resident),
            Ground(reachable: true));

        AssertCommand(decision, ApplicationInputCommandKind.MoveResident);
        Assert.Equal(Resident, decision.ActorId);
        Assert.Equal(Cell, decision.TargetCell);
    }

    [Fact]
    public void Generic_item_without_interaction_contract_uses_ground_move_fallback()
    {
        ContextInputDecision decision = _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left,
                altPressed: true),
            new ContextInputState(selectedResidentId: Resident),
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                Target,
                Cell,
                reachable: true,
                supportsAltInteraction: false));

        AssertCommand(decision, ApplicationInputCommandKind.MoveResident);
        Assert.Null(decision.TargetEntityId);
    }

    [Fact]
    public void Excavation_runs_only_without_resident_selection()
    {
        ContextInputDecision decision = WorldLeft(
            new ContextInputState(excavationTool: ExcavationToolKind.Erase),
            Ground(reachable: true));

        AssertCommand(decision, ApplicationInputCommandKind.ApplyExcavation);
        Assert.Equal(ExcavationToolKind.Erase, decision.ExcavationTool);
        Assert.Null(decision.ActorId);
    }

    private ContextInputDecision WorldLeft(
        ContextInputState state,
        ContextPointerTarget target)
    {
        return _router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left),
            state,
            target);
    }

    private static ContextPointerTarget Ground(bool reachable)
    {
        return new ContextPointerTarget(
            ContextWorldTargetKind.Ground,
            cell: Cell,
            reachable: reachable);
    }

    private static void AssertCommand(
        ContextInputDecision decision,
        ApplicationInputCommandKind expected)
    {
        Assert.True(decision.ConsumesPointer);
        Assert.True(decision.HasApplicationCommand);
        Assert.Equal(expected, decision.CommandKind);
        Assert.Equal(PresentationInputEffect.None, decision.Effects);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}