using Dig.Presentation.Input;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private void ApplyDecision(
        ContextInputDecision decision,
        DigAgentVisual? agent = null,
        DigCellVisual? cell = null,
        DigBuildingVisual? building = null,
        DigWorldItemVisual? item = null,
        ResidentInventoryLayoutSlotViewModel? inventorySlot = null,
        ResidentInventorySlotViewModel? legacyInventorySlot = null)
    {
        ApplyEffects(
            decision,
            agent,
            building,
            item,
            inventorySlot,
            legacyInventorySlot);
        if (!decision.HasApplicationCommand)
        {
            return;
        }

        switch (decision.CommandKind)
        {
            case ApplicationInputCommandKind.ConfirmBuildingPlacement:
                ConfirmBuildingPlacement();
                break;
            case ApplicationInputCommandKind.UseInventoryItem:
                ApplyResidentInventoryUse(decision);
                break;
            case ApplicationInputCommandKind.DropInventoryStack:
                ApplyResidentInventoryDrop(decision);
                break;
            case ApplicationInputCommandKind.PickupBuildingBox:
                CreateBuildingBoxPickup(decision);
                break;
            case ApplicationInputCommandKind.PickupWorldItem:
                CreateWorldItemPickup(decision);
                break;
            case ApplicationInputCommandKind.EatWorldItem:
                CreateWorldFoodPickup(decision);
                break;
            case ApplicationInputCommandKind.UseProductionPackage:
                ApplyProductionPackageUse(decision);
                break;
            case ApplicationInputCommandKind.MoveResident:
                ApplyMove(decision);
                break;
            case ApplicationInputCommandKind.AttackTarget:
                ApplyAttack(decision);
                break;
            case ApplicationInputCommandKind.AttackBarrel:
                ApplyBarrelAttack(decision);
                break;
            case ApplicationInputCommandKind.ChopMushroom:
                ApplyMushroomChop(decision);
                break;
            case ApplicationInputCommandKind.ApplyExcavation:
                ApplyExcavation(decision, cell);
                break;
            default:
                _hud!.SetStatus(
                    $"Input command '{decision.CommandKind}' is not wired in this demo slice.");
                break;
        }
    }

    private void ApplyEffects(
        ContextInputDecision decision,
        DigAgentVisual? agent,
        DigBuildingVisual? building,
        DigWorldItemVisual? item,
        ResidentInventoryLayoutSlotViewModel? inventorySlot,
        ResidentInventorySlotViewModel? legacyInventorySlot)
    {
        if (decision.Effects.HasFlag(PresentationInputEffect.CancelBuildingPlacement))
        {
            CancelBuildingPlacement();
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.StartBuildingPlacement))
        {
            StartBuildingPlacement(decision, item);
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.StartItemPlacement))
        {
            if (inventorySlot != null)
            {
                BeginInventoryItemPlacement(inventorySlot);
            }
            else if (legacyInventorySlot != null)
            {
                BeginInventoryItemPlacement(legacyInventorySlot);
            }
            else
            {
                _hud!.SetStatus("input.inventory.item_placement_unavailable");
            }
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.SelectBuildingBox))
        {
            if (item != null && item.Model.IsBuildingBox)
            {
                SelectBuildingBox(item.Model, item);
            }
            else if (decision.TargetEntityId.HasValue)
            {
                SelectBuildingBoxFromHud(decision.TargetEntityId.Value.ToString());
            }
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.DeselectResident))
        {
            _agentRenderer!.ClearSelection();
            ClearSelectedInventoryStack();
            _hud!.SetAgentSelection(null, 0);
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.SelectResident))
        {
            ClearBuildingBoxSelection();
            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ClearSelectedInventoryStack();
            DigAgentVisual? selected = agent;
            if (selected == null && decision.TargetEntityId.HasValue)
            {
                selected = _agentRenderer!.SelectById(
                    decision.TargetEntityId.Value.ToString());
            }
            else
            {
                _agentRenderer!.Select(selected);
            }

            _selectedCell = null;
            _renderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _hud!.SetAgentSelection(
                selected?.Model,
                _agentRenderer.SelectedCount);
            _hud.ActivateResidentRosterForSelection();
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.SelectBuilding))
        {
            ClearBuildingBoxSelection();
            ClearSelectedInventoryStack();
            DigBuildingVisual? selected = building;
            if (selected == null && decision.TargetEntityId.HasValue)
            {
                selected = _buildingRenderer!.SelectById(
                    decision.TargetEntityId.Value.ToString());
            }
            else
            {
                _buildingRenderer!.Select(selected);
            }

            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.ClearSelection();
            _jobRenderer!.Select(null);
            _hud!.SetBuildingSelection(selected?.Model);
            _hud.ActivateBuildingRosterForSelection();
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.FocusResident)
            && decision.TargetEntityId.HasValue
            && _agentRenderer!.TryGetWorldPosition(
                decision.TargetEntityId.Value.ToString(),
                out Vector3 position))
        {
            _cameraController!.Focus(position);
        }

        if (decision.Effects.HasFlag(PresentationInputEffect.ShowReason)
            && decision.ReasonCode != null)
        {
            _hud!.SetStatus(decision.ReasonCode);
        }
    }
}

}
