using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigWorldInteraction
{
    private DigTunnelReinforcementGhostRenderer? _tunnelReinforcementGhostRenderer;
    private string? _tunnelReinforcementResidentId;
    private string? _tunnelReinforcementStackId;
    private string? _tunnelReinforcementItemId;
    private TunnelManualReinforcementPlan? _tunnelReinforcementPlan;
    private CellId? _tunnelReinforcementCell;
    private bool _tunnelReinforcementValid;

    private bool TunnelReinforcementPlacementActive =>
        _tunnelReinforcementStackId != null;

    private static bool IsReinforcementModifierPressed()
    {
        return Input.GetKey(KeyCode.B);
    }

    private bool TryBeginTunnelReinforcementPlacement(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        EnsureLayoutSlot(slot);
        return TryBeginTunnelReinforcementPlacement(
            slot.StackId!,
            slot.ItemId!);
    }

    private bool TryBeginTunnelReinforcementPlacement(
        ResidentInventorySlotViewModel slot)
    {
        return TryBeginTunnelReinforcementPlacement(slot.StackId, slot.ItemId);
    }

    private bool TryBeginTunnelReinforcementPlacement(
        string stackId,
        string itemId)
    {
        if (!IsReinforcementModifierPressed()
            || (itemId != "material.mushroom_leg" && itemId != "material.stone"))
        {
            return false;
        }

        var resident = _agentRenderer?.SelectedModel;
        if (resident == null || !resident.IsAlive || _terrainSession == null || _hud == null)
        {
            _hud?.SetStatus("input.inventory.resident_not_selected");
            return true;
        }

        CancelInventoryItemPlacement();
        if (_buildingPlacementMode.HasValue)
        {
            CancelBuildingPlacement();
        }

        DisableExcavationDrawing();
        DisableCaveRoomPlanning();
        ClearRoomPurposeSelection();
        _tunnelReinforcementResidentId = resident.Id;
        _tunnelReinforcementStackId = stackId;
        _tunnelReinforcementItemId = itemId;
        Cursor.visible = false;
        EnsureTunnelReinforcementGhostRenderer();
        UpdateTunnelReinforcementPlacementHover();
        _hud.SetStatus("Tunnel reinforcement placement active. LMB confirms; RMB cancels.");
        return true;
    }

    private void UpdateTunnelReinforcementPlacementHover()
    {
        if (!TunnelReinforcementPlacementActive
            || _hud == null
            || _hud.ContainsScreenPoint(Input.mousePosition))
        {
            return;
        }

        if (!TryResolveBuildingPlacementOrigin(GetPointerHits(), out CellId target))
        {
            return;
        }

        Result<TunnelManualReinforcementPlan> validation =
            _terrainSession!.ValidateTunnelManualReinforcement(
                _tunnelReinforcementResidentId!,
                _tunnelReinforcementStackId!,
                target);
        _tunnelReinforcementCell = target;
        _tunnelReinforcementPlan = validation.IsSuccess ? validation.Value : null;
        _tunnelReinforcementValid = validation.IsSuccess;
        EnsureTunnelReinforcementGhostRenderer();
        _tunnelReinforcementGhostRenderer!.Render(
            _tunnelReinforcementPlan,
            target,
            validation.IsSuccess);
    }

    private bool TryHandleTunnelReinforcementPlacementClick()
    {
        if (!TunnelReinforcementPlacementActive || !Input.GetMouseButtonDown(0))
        {
            return false;
        }

        if (_hud == null || _hud.ContainsScreenPoint(Input.mousePosition))
        {
            return true;
        }

        UpdateTunnelReinforcementPlacementHover();
        if (!_tunnelReinforcementValid || !_tunnelReinforcementCell.HasValue)
        {
            _hud.SetStatus("tunnel.manual_reinforcement.target_unavailable");
            return true;
        }

        Result result = _terrainSession!.CreateTunnelManualReinforcement(
            _tunnelReinforcementResidentId!,
            _tunnelReinforcementStackId!,
            _tunnelReinforcementCell.Value,
            _simulation!.CurrentTick);
        _hud.SetCommandResult(result);
        if (result.IsSuccess)
        {
            ClearSelectedInventoryStack();
            CancelTunnelReinforcementPlacement();
            _hud.SetStatus("Tunnel reinforcement order created.");
        }

        return true;
    }

    private void CancelTunnelReinforcementPlacement()
    {
        bool active = TunnelReinforcementPlacementActive;
        _tunnelReinforcementResidentId = null;
        _tunnelReinforcementStackId = null;
        _tunnelReinforcementItemId = null;
        _tunnelReinforcementPlan = null;
        _tunnelReinforcementCell = null;
        _tunnelReinforcementValid = false;
        _tunnelReinforcementGhostRenderer?.Clear();
        if (active)
        {
            Cursor.visible = true;
        }
    }

    private void EnsureTunnelReinforcementGhostRenderer()
    {
        _tunnelReinforcementGhostRenderer ??=
            GetComponent<DigTunnelReinforcementGhostRenderer>()
            ?? gameObject.AddComponent<DigTunnelReinforcementGhostRenderer>();
    }
}
}
