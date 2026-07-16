namespace Dig.Presentation.Input
{

public sealed partial class ContextInputRouter
{
    private static ContextInputDecision RouteWorldLeftClick(
        ContextPointerEvent pointer,
        ContextInputState state,
        ContextPointerTarget target)
    {
        if (state.BuildingPlacementActive)
        {
            if (target.Kind == ContextWorldTargetKind.Ground
                && target.Cell.HasValue
                && state.BuildingPlacementValid)
            {
                return Command(
                    ApplicationInputCommandKind.ConfirmBuildingPlacement,
                    state.SelectedResidentId,
                    state.SelectedInventoryStackId,
                    target.Cell);
            }

            return Local(
                PresentationInputEffect.KeepBuildingPreview
                    | PresentationInputEffect.ShowReason,
                consumesPointer: true,
                targetCell: target.Cell,
                reasonCode: state.BuildingPlacementReasonCode
                    ?? "input.building_placement.invalid");
        }

        if (state.SelectedResidentId.HasValue && !state.SelectedResidentAlive)
        {
            return Local(
                PresentationInputEffect.DeselectResident
                    | PresentationInputEffect.ShowReason,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                reasonCode: "input.selected_resident.stale_or_dead");
        }

        if (state.SelectedInventoryStackId.HasValue
            && target.Kind == ContextWorldTargetKind.Ground
            && target.Cell.HasValue)
        {
            return Command(
                ApplicationInputCommandKind.DropInventoryStack,
                state.SelectedResidentId,
                state.SelectedInventoryStackId,
                target.Cell);
        }

        if (pointer.AltPressed && target.Kind == ContextWorldTargetKind.BuildingBox)
        {
            if (state.HasUsableResidentSelection
                && target.SupportsAltInteraction
                && target.EntityId.HasValue)
            {
                return Command(
                    ApplicationInputCommandKind.PickupBuildingBox,
                    state.SelectedResidentId,
                    target.EntityId,
                    target.Cell);
            }

            return MoveFallback(state, target);
        }

        if (pointer.AltPressed && target.Kind == ContextWorldTargetKind.GenericItem)
        {
            if (state.HasUsableResidentSelection
                && target.SupportsAltInteraction
                && target.EntityId.HasValue)
            {
                return Command(
                    ApplicationInputCommandKind.PickupWorldItem,
                    state.SelectedResidentId,
                    target.EntityId,
                    target.Cell);
            }

            return MoveFallback(state, target);
        }

        if (target.Kind == ContextWorldTargetKind.BuildingBox)
        {
            return Local(
                PresentationInputEffect.StartBuildingPlacement,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                targetEntityId: target.EntityId,
                targetCell: target.Cell);
        }

        if (state.HasUsableResidentSelection
            && target.Kind == ContextWorldTargetKind.HostileResident)
        {
            if (!target.IsAlive || !target.EntityId.HasValue)
            {
                return Local(
                    PresentationInputEffect.ShowReason,
                    consumesPointer: true,
                    actorId: state.SelectedResidentId,
                    targetEntityId: target.EntityId,
                    reasonCode: "input.target.stale_or_dead");
            }

            return Command(
                ApplicationInputCommandKind.AttackTarget,
                state.SelectedResidentId,
                target.EntityId,
                target.Cell);
        }

        ContextInputDecision move = MoveFallback(state, target);
        if (move.ConsumesPointer)
        {
            return move;
        }

        if (state.HasUsableResidentSelection
            && target.Kind == ContextWorldTargetKind.Ground
            && target.Cell.HasValue)
        {
            return Local(
                PresentationInputEffect.ShowReason,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                targetCell: target.Cell,
                reasonCode: "input.move.unreachable");
        }

        if (!state.SelectedResidentId.HasValue
            && state.ExcavationTool != ExcavationToolKind.None
            && target.Kind == ContextWorldTargetKind.Ground
            && target.Cell.HasValue)
        {
            return Command(
                ApplicationInputCommandKind.ApplyExcavation,
                actorId: null,
                targetEntityId: null,
                targetCell: target.Cell,
                excavationTool: state.ExcavationTool);
        }

        if (target.Kind == ContextWorldTargetKind.Resident
            && target.EntityId.HasValue)
        {
            if (!target.IsAlive)
            {
                return Local(
                    PresentationInputEffect.ShowReason,
                    consumesPointer: true,
                    targetEntityId: target.EntityId,
                    reasonCode: "input.target.stale_or_dead");
            }

            PresentationInputEffect effect = PresentationInputEffect.SelectResident;
            if (pointer.ClickCount >= 2)
            {
                effect |= PresentationInputEffect.FocusResident;
            }

            return Local(
                effect,
                consumesPointer: true,
                targetEntityId: target.EntityId);
        }

        if (target.Kind == ContextWorldTargetKind.CompletedBuilding
            && target.EntityId.HasValue)
        {
            return Local(
                PresentationInputEffect.SelectBuilding,
                consumesPointer: true,
                targetEntityId: target.EntityId,
                targetCell: target.Cell);
        }

        if (target.Kind == ContextWorldTargetKind.Ground && target.Cell.HasValue)
        {
            return Local(
                PresentationInputEffect.SelectGround,
                consumesPointer: true,
                targetCell: target.Cell);
        }

        return None();
    }

    private static ContextInputDecision MoveFallback(
        ContextInputState state,
        ContextPointerTarget target)
    {
        bool movementTarget = target.Kind == ContextWorldTargetKind.Ground
            || target.Kind == ContextWorldTargetKind.GenericItem
            || target.Kind == ContextWorldTargetKind.BuildingBox;
        if (!movementTarget
            || !state.HasUsableResidentSelection
            || !target.Reachable
            || !target.Cell.HasValue)
        {
            return None();
        }

        return Command(
            ApplicationInputCommandKind.MoveResident,
            state.SelectedResidentId,
            targetEntityId: null,
            target.Cell);
    }
}
}
