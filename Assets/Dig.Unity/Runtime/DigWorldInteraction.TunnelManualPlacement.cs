using System;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigTunnelManualPlacementGhostRenderer? _tunnelManualGhost;
        private string? _tunnelManualResidentId;
        private string? _tunnelManualStackId;
        private string? _tunnelManualItemId;
        private CellId? _tunnelManualTarget;
        private TunnelManualWorkKind? _tunnelManualKind;
        private bool _tunnelManualTargetValid;

        private bool TunnelManualPlacementActive =>
            _tunnelManualStackId != null;

        internal void BeginTunnelManualPlacement(
            ResidentInventoryLayoutSlotViewModel slot)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            var resident = _agentRenderer?.SelectedModel;
            if (resident == null
                || string.IsNullOrWhiteSpace(slot.StackId)
                || string.IsNullOrWhiteSpace(slot.ItemId)
                || (slot.ItemId != "material.mushroom_leg"
                    && slot.ItemId != "material.stone"))
            {
                _hud?.SetStatus("tunnel.manual.unsupported_material");
                return;
            }

            CancelInventoryItemPlacement();
            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            _tunnelManualResidentId = resident.Id;
            _tunnelManualStackId = slot.StackId;
            _tunnelManualItemId = slot.ItemId;
            Cursor.visible = false;
            EnsureTunnelManualGhost();
            UpdateTunnelManualPlacementHover();
            _hud!.SetStatus("Manual tunnel placement active. RMB cancels.");
        }

        private void UpdateTunnelManualPlacementHover()
        {
            if (!TunnelManualPlacementActive
                || _hud == null
                || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                return;
            }

            if (!TryResolveBuildingPlacementOrigin(GetPointerHits(), out CellId target))
            {
                return;
            }

            Result<TunnelManualPlacementPlan> validation =
                _terrainSession!.ValidateTunnelManualPlacement(
                    _tunnelManualResidentId!,
                    _tunnelManualStackId!,
                    target);
            _tunnelManualTarget = target;
            _tunnelManualTargetValid = validation.IsSuccess;
            _tunnelManualKind = validation.IsSuccess
                ? validation.Value.Kind
                : ResolvePreviewKind(_tunnelManualItemId!);
            EnsureTunnelManualGhost();
            _tunnelManualGhost!.Render(
                _tunnelManualKind.Value,
                target,
                validation.IsSuccess);
            if (validation.IsFailure)
            {
                _hud.SetStatus(validation.Error!.Code);
            }
        }

        private bool TryHandleTunnelManualPlacementClick()
        {
            if (!TunnelManualPlacementActive
                || !Input.GetMouseButtonDown(0))
            {
                return false;
            }

            if (_hud == null || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                return true;
            }

            UpdateTunnelManualPlacementHover();
            if (!_tunnelManualTargetValid || !_tunnelManualTarget.HasValue)
            {
                _hud.SetStatus("tunnel.manual.target_unavailable");
                return true;
            }

            Result result = _terrainSession!.CreateTunnelManualWork(
                _tunnelManualResidentId!,
                _tunnelManualStackId!,
                _tunnelManualTarget.Value,
                _simulation!.CurrentTick);
            _hud.SetCommandResult(result);
            if (result.IsSuccess)
            {
                ClearSelectedInventoryStack();
                CancelTunnelManualPlacement();
                _hud.SetStatus("Manual tunnel work order created.");
            }

            return true;
        }

        private void CancelTunnelManualPlacement()
        {
            bool wasActive = TunnelManualPlacementActive;
            _tunnelManualResidentId = null;
            _tunnelManualStackId = null;
            _tunnelManualItemId = null;
            _tunnelManualTarget = null;
            _tunnelManualKind = null;
            _tunnelManualTargetValid = false;
            _tunnelManualGhost?.Clear();
            if (wasActive)
            {
                Cursor.visible = true;
            }
        }

        private void EnsureTunnelManualGhost()
        {
            _tunnelManualGhost ??=
                GetComponent<DigTunnelManualPlacementGhostRenderer>()
                ?? gameObject.AddComponent<DigTunnelManualPlacementGhostRenderer>();
        }

        private static TunnelManualWorkKind ResolvePreviewKind(string itemId)
        {
            return itemId == "material.mushroom_leg"
                ? TunnelManualWorkKind.WoodenSupport
                : TunnelManualWorkKind.StoneFloorTrim;
        }
    }
}
