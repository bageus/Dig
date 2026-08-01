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

        if (target.ItemInteractionAction
            != Dig.Domain.Inventory.ItemWorldInteractionAction.None)
        {
            return RouteWorldItemAction(state, target);
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

        if (target.Kind == ContextWorldTargetKind.Barrel)
        {
            if (state.HasUsableResidentSelection
                && target.Reachable
                && target.EntityId.HasValue
                && target.Cell.HasValue)
            {
                return Command(
                    ApplicationInputCommandKind.AttackBarrel,
                    state.SelectedResidentId,
                    target.EntityId,
                    target.Cell);
            }

            return Local(
                PresentationInputEffect.ShowReason,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                targetEntityId: target.EntityId,
                targetCell: target.Cell,
                reasonCode: state.HasUsableResidentSelection
                    ? "input.barrel.unreachable_or_unavailable"
                    : "input.barrel.resident_required");
        }

        if (target.Kind == ContextWorldTargetKind.Mushroom)
        {
            if (state.HasUsableResidentSelection
                && target.Reachable
                && target.EntityId.HasValue
                && target.Cell.HasValue)
            {
                return Command(
                    ApplicationInputCommandKind.ChopMushroom,
                    state.SelectedResidentId,
                    target.EntityId,
                    target.Cell);
            }

            return Local(
                PresentationInputEffect.ShowReason,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                targetEntityId: target.EntityId,
                targetCell: target.Cell,
                reasonCode: state.HasUsableResidentSelection
                    ? "input.mushroom.unreachable_or_absent"
                    : "input.mushroom.resident_required");
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

        return None();
    }

    private static ContextInputDecision RouteWorldItemAction(
        ContextInputState state,
        ContextPointerTarget target)
    {
        switch (target.ItemInteractionAction)
        {
            case Dig.Domain.Inventory.ItemWorldInteractionAction.SelectBuildingBox:
                if (target.EntityId.HasValue)
                {
                    return Local(
                        PresentationInputEffect.SelectBuildingBox,
                        consumesPointer: true,
                        targetEntityId: target.EntityId,
                        targetCell: target.Cell);
                }
                break;

            case Dig.Domain.Inventory.ItemWorldInteractionAction.Pickup:
                if (state.HasUsableResidentSelection
                    && target.ItemActionAvailable
                    && target.EntityId.HasValue)
                {
                    return Command(
                        target.Kind == ContextWorldTargetKind.BuildingBox
                            ? ApplicationInputCommandKind.PickupBuildingBox
                            : ApplicationInputCommandKind.PickupWorldItem,
                        state.SelectedResidentId,
                        target.EntityId,
                        target.Cell);
                }

                return Local(
                    PresentationInputEffect.ShowReason,
                    consumesPointer: true,
                    actorId: state.SelectedResidentId,
                    targetEntityId: target.EntityId,
                    targetCell: target.Cell,
                    reasonCode: state.HasUsableResidentSelection
                        ? "input.world_item.unavailable"
                        : "input.world_item.resident_required");

            case Dig.Domain.Inventory.ItemWorldInteractionAction.DirectUse:
                if (state.HasUsableResidentSelection
                    && target.ItemActionAvailable
                    && target.EntityId.HasValue)
                {
                    return Command(
                        ApplicationInputCommandKind.EatWorldItem,
                        state.SelectedResidentId,
                        target.EntityId,
                        target.Cell);
                }

                return Local(
                    PresentationInputEffect.ShowReason,
                    consumesPointer: true,
                    actorId: state.SelectedResidentId,
                    targetEntityId: target.EntityId,
                    targetCell: target.Cell,
                    reasonCode: state.HasUsableResidentSelection
                        ? "input.world_item.use_unavailable"
                        : "input.world_item.resident_required");

            case Dig.Domain.Inventory.ItemWorldInteractionAction.UseProductionPackage:
                if (state.HasUsableResidentSelection
                    && target.ItemActionAvailable
                    && target.EntityId.HasValue)
                {
                    return Command(
                        ApplicationInputCommandKind.UseProductionPackage,
                        state.SelectedResidentId,
                        target.EntityId,
                        target.Cell);
                }

                return Local(
                    PresentationInputEffect.ShowReason,
                    consumesPointer: true,
                    actorId: state.SelectedResidentId,
                    targetEntityId: target.EntityId,
                    targetCell: target.Cell,
                    reasonCode: state.HasUsableResidentSelection
                        ? "input.production_package.unavailable"
                        : "input.production_package.resident_required");
        }

        return None();
    }

    private static ContextInputDecision MoveFallback(
        ContextInputState state,
        ContextPointerTarget target)
    {
        bool movementTarget = target.Kind == ContextWorldTargetKind.Ground
            || target.Kind == ContextWorldTargetKind.GenericItem
            || target.Kind == ContextWorldTargetKind.FoodItem
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