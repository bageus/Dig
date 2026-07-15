using System;
using Dig.Domain.Core;

namespace Dig.Presentation.Input
{

public sealed partial class ContextInputRouter
{
    public ContextInputDecision Route(
        ContextPointerEvent pointer,
        ContextInputState state,
        ContextPointerTarget target)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (pointer.Surface == PointerInputSurface.World
            && pointer.IsPointerOverBlockingUi)
        {
            return Local(
                PresentationInputEffect.None,
                consumesPointer: true,
                reasonCode: "input.ui_shielded");
        }

        if (pointer.Surface == PointerInputSurface.ResidentRoster)
        {
            return RouteRoster(pointer, target);
        }

        if (pointer.Button == PointerButtonKind.Right)
        {
            return RouteRightClick(pointer, state, target);
        }

        if (pointer.Surface == PointerInputSurface.ResidentInventory)
        {
            return RouteInventory(pointer, state, target);
        }

        return RouteWorldLeftClick(pointer, state, target);
    }

    public ContextPanelMode ResolvePanelMode(ContextInputState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.BuildingPlacementActive)
        {
            return ContextPanelMode.BuildingPlacement;
        }

        if (state.HasUsableResidentSelection)
        {
            return ContextPanelMode.ResidentInventory;
        }

        return state.SelectedCompletedBuildingId.HasValue
            ? ContextPanelMode.BuildingFunctions
            : ContextPanelMode.ExcavationPalette;
    }

    private static ContextInputDecision RouteRoster(
        ContextPointerEvent pointer,
        ContextPointerTarget target)
    {
        if (pointer.Button != PointerButtonKind.Left
            || target.Kind != ContextWorldTargetKind.Resident
            || !target.EntityId.HasValue)
        {
            return None();
        }

        if (!target.IsAlive)
        {
            return Local(
                PresentationInputEffect.ShowReason,
                consumesPointer: true,
                targetEntityId: target.EntityId,
                reasonCode: "input.target.stale_or_dead");
        }

        PresentationInputEffect effects = PresentationInputEffect.SelectResident;
        if (pointer.ClickCount >= 2)
        {
            effects |= PresentationInputEffect.FocusResident;
        }

        return Local(
            effects,
            consumesPointer: true,
            targetEntityId: target.EntityId);
    }

    private static ContextInputDecision RouteRightClick(
        ContextPointerEvent pointer,
        ContextInputState state,
        ContextPointerTarget target)
    {
        if (state.BuildingPlacementActive)
        {
            return Local(
                PresentationInputEffect.CancelBuildingPlacement,
                consumesPointer: true);
        }

        if (state.SelectedResidentId.HasValue)
        {
            return Local(
                PresentationInputEffect.DeselectResident,
                consumesPointer: true,
                actorId: state.SelectedResidentId);
        }

        if (pointer.Surface == PointerInputSurface.World
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

        return None();
    }

    private static ContextInputDecision RouteInventory(
        ContextPointerEvent pointer,
        ContextInputState state,
        ContextPointerTarget target)
    {
        if (state.SelectedResidentId.HasValue && !state.SelectedResidentAlive)
        {
            return Local(
                PresentationInputEffect.DeselectResident
                    | PresentationInputEffect.ShowReason,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                reasonCode: "input.selected_resident.stale_or_dead");
        }

        if (pointer.AltPressed
            && state.HasUsableResidentSelection
            && state.SelectedInventoryStackId.HasValue
            && state.SelectedInventoryItemUsable
            && state.CanUseSelectedInventoryItem)
        {
            return Command(
                ApplicationInputCommandKind.UseInventoryItem,
                actorId: state.SelectedResidentId,
                targetEntityId: state.SelectedInventoryStackId,
                targetCell: target.Cell);
        }

        if (state.HasUsableResidentSelection
            && state.SelectedInventoryStackId.HasValue
            && state.SelectedInventoryItemIsBuildingBox)
        {
            return Local(
                PresentationInputEffect.StartBuildingPlacement,
                consumesPointer: true,
                actorId: state.SelectedResidentId,
                targetEntityId: state.SelectedInventoryStackId);
        }

        return None();
    }

    private static ContextInputDecision Command(
        ApplicationInputCommandKind kind,
        EntityId? actorId,
        EntityId? targetEntityId,
        Dig.Domain.World.CellId? targetCell,
        ExcavationToolKind excavationTool = ExcavationToolKind.None)
    {
        return new ContextInputDecision(
            effects: PresentationInputEffect.None,
            commandKind: kind,
            consumesPointer: true,
            actorId: actorId,
            targetEntityId: targetEntityId,
            targetCell: targetCell,
            excavationTool: excavationTool);
    }

    private static ContextInputDecision Local(
        PresentationInputEffect effects,
        bool consumesPointer,
        EntityId? actorId = null,
        EntityId? targetEntityId = null,
        Dig.Domain.World.CellId? targetCell = null,
        string? reasonCode = null)
    {
        return new ContextInputDecision(
            effects: effects,
            commandKind: ApplicationInputCommandKind.None,
            consumesPointer: consumesPointer,
            actorId: actorId,
            targetEntityId: targetEntityId,
            targetCell: targetCell,
            reasonCode: reasonCode);
    }

    private static ContextInputDecision None()
    {
        return new ContextInputDecision(
            PresentationInputEffect.None,
            ApplicationInputCommandKind.None,
            consumesPointer: false);
    }
}
}