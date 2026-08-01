using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigInventoryItemGhostRenderer? _inventoryItemGhostRenderer;
        private string? _inventoryItemPlacementResidentId;
        private string? _inventoryItemPlacementStackId;
        private string? _inventoryItemPlacementItemId;
        private CellId? _inventoryItemPlacementCell;
        private bool _inventoryItemPlacementValid;

        private bool InventoryItemPlacementActive =>
            _inventoryItemPlacementStackId != null;

        private void BeginInventoryItemPlacement(
            ResidentInventorySlotViewModel slot)
        {
            if (slot == null)
            {
                throw new System.ArgumentNullException(nameof(slot));
            }

            BeginInventoryItemPlacement(
                slot.StackId,
                slot.ItemId,
                slot.CanDrop,
                slot.IsBuildingBox);
        }

        private void BeginInventoryItemPlacement(
            ResidentInventoryLayoutSlotViewModel slot)
        {
            EnsureLayoutSlot(slot);
            BeginInventoryItemPlacement(
                slot.StackId!,
                slot.ItemId!,
                slot.CanDrop,
                slot.IsBuildingBox);
        }

        private void BeginInventoryItemPlacement(
            string stackId,
            string itemId,
            bool canDrop,
            bool isBuildingBox)
        {
            if (_agentRenderer?.SelectedModel == null
                || _terrainSession == null
                || _hud == null
                || !canDrop
                || isBuildingBox
                || string.IsNullOrWhiteSpace(stackId)
                || string.IsNullOrWhiteSpace(itemId))
            {
                _hud?.SetStatus("input.inventory.item_placement_unavailable");
                return;
            }

            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            _inventoryItemPlacementResidentId = _agentRenderer.SelectedModel.Id;
            _inventoryItemPlacementStackId = stackId;
            _inventoryItemPlacementItemId = itemId;
            Cursor.visible = false;
            EnsureInventoryItemGhostRenderer();
            if (TryResolveBuildingPlacementOrigin(GetPointerHits(), out CellId target))
            {
                UpdateInventoryItemPlacement(target);
            }
            else
            {
                var resident = _agentRenderer.SelectedModel;
                UpdateInventoryItemPlacement(new CellId(
                    resident.CellX,
                    resident.CellY,
                    resident.CellZ));
            }

            _hud.SetStatus("Inventory item placement active. RMB cancels.");
        }

        private void UpdateInventoryItemPlacementHover()
        {
            if (!InventoryItemPlacementActive
                || _hud == null
                || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                return;
            }

            if (TryResolveBuildingPlacementOrigin(GetPointerHits(), out CellId target))
            {
                UpdateInventoryItemPlacement(target);
            }
        }

        private void UpdateInventoryItemPlacement(CellId target)
        {
            Result validation = _terrainSession!.ValidateResidentInventoryPlacement(
                _inventoryItemPlacementResidentId!,
                _inventoryItemPlacementStackId!,
                target);
            _inventoryItemPlacementCell = target;
            _inventoryItemPlacementValid = validation.IsSuccess;
            EnsureInventoryItemGhostRenderer();
            _inventoryItemGhostRenderer!.Render(
                _inventoryItemPlacementItemId!,
                target,
                validation.IsSuccess);
        }

        private bool TryHandleInventoryItemPlacementClick()
        {
            if (!InventoryItemPlacementActive
                || !Input.GetMouseButtonDown(0))
            {
                return false;
            }

            if (_hud == null || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                return true;
            }

            UpdateInventoryItemPlacementHover();
            if (!_inventoryItemPlacementValid
                || !_inventoryItemPlacementCell.HasValue)
            {
                _hud.SetStatus("input.inventory.placement.target_blocked");
                return true;
            }

            Result result = _terrainSession!.CreateResidentInventoryPlacement(
                _inventoryItemPlacementResidentId!,
                _inventoryItemPlacementStackId!,
                _inventoryItemPlacementCell.Value,
                _simulation!.CurrentTick);
            _hud.SetCommandResult(result);
            if (result.IsSuccess)
            {
                ClearSelectedInventoryStack();
                CancelInventoryItemPlacement();
                _hud.SetStatus("Inventory item placement order created.");
            }

            return true;
        }

        private void CancelInventoryItemPlacement()
        {
            bool wasActive = InventoryItemPlacementActive;
            _inventoryItemPlacementResidentId = null;
            _inventoryItemPlacementStackId = null;
            _inventoryItemPlacementItemId = null;
            _inventoryItemPlacementCell = null;
            _inventoryItemPlacementValid = false;
            _inventoryItemGhostRenderer?.Clear();
            if (wasActive)
            {
                Cursor.visible = true;
            }
        }

        private void EnsureInventoryItemGhostRenderer()
        {
            _inventoryItemGhostRenderer ??=
                GetComponent<DigInventoryItemGhostRenderer>()
                ?? gameObject.AddComponent<DigInventoryItemGhostRenderer>();
        }
    }
}
