using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const float InventoryDoubleClickSeconds = 0.36f;
        private string? _lastActivatedInventoryStackId;
        private float _lastInventoryActivationTime = float.NegativeInfinity;

        internal void ActivateResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            if (slot == null)
            {
                throw new System.ArgumentNullException(nameof(slot));
            }

            float now = Time.unscaledTime;
            bool doubleClick = string.Equals(
                    _lastActivatedInventoryStackId,
                    slot.StackId,
                    System.StringComparison.Ordinal)
                && now - _lastInventoryActivationTime <= InventoryDoubleClickSeconds;
            _lastActivatedInventoryStackId = doubleClick ? null : slot.StackId;
            _lastInventoryActivationTime = doubleClick
                ? float.NegativeInfinity
                : now;

            if (doubleClick)
            {
                DropResidentInventorySlot(slot);
                return;
            }

            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Left,
                altPressed: false);
        }

        internal void BeginResidentInventoryBuildingPlacement(
            ResidentInventoryLayoutSlotViewModel slot)
        {
            if (slot == null)
            {
                throw new System.ArgumentNullException(nameof(slot));
            }

            ResetInventoryClickSequence();
            if (!slot.CanStartPlacement
                || _agentRenderer?.SelectedModel == null
                || _hud == null)
            {
                _hud?.SetStatus("input.inventory.building_placement_unavailable");
                return;
            }

            var resident = _agentRenderer.SelectedModel;
            EntityId residentId = EntityId.Parse(resident.Id);
            EntityId stackId = EntityId.Parse(slot.StackId);
            ContextInputState state = new ContextInputState(
                selectedResidentId: residentId,
                selectedResidentAlive: resident.IsAlive,
                selectedInventoryStackId: stackId,
                selectedInventoryItemUsable: false,
                selectedInventoryItemIsBuildingBox: true,
                canUseSelectedInventoryItem: false,
                canDropSelectedInventoryItem: slot.CanDrop);
            ContextPointerTarget target = new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stackId,
                new CellId(resident.CellX, resident.CellY, resident.CellZ));
            ApplyDecision(_inputRouter.Route(
                new ContextPointerEvent(
                    PointerInputSurface.ResidentInventory,
                    PointerButtonKind.Left,
                    altPressed: false),
                state,
                target));
        }

        internal void UseResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            ResetInventoryClickSequence();
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Left,
                altPressed: true);
        }

        internal void DropResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            ResetInventoryClickSequence();
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Right,
                altPressed: false);
        }

        private void ResetInventoryClickSequence()
        {
            _lastActivatedInventoryStackId = null;
            _lastInventoryActivationTime = float.NegativeInfinity;
        }

        private void RouteResidentInventorySlot(
            ResidentInventorySlotViewModel slot,
            PointerButtonKind button,
            bool altPressed)
        {
            if (slot == null)
            {
                throw new System.ArgumentNullException(nameof(slot));
            }

            if (_agentRenderer?.SelectedModel == null || _hud == null)
            {
                _hud?.SetStatus("input.inventory.resident_not_selected");
                return;
            }

            var resident = _agentRenderer.SelectedModel;
            EntityId residentId = EntityId.Parse(resident.Id);
            EntityId stackId = EntityId.Parse(slot.StackId);
            ContextInputState state = new ContextInputState(
                selectedResidentId: residentId,
                selectedResidentAlive: resident.IsAlive,
                selectedInventoryStackId: stackId,
                selectedInventoryItemUsable: slot.IsTool,
                selectedInventoryItemIsBuildingBox: slot.IsBuildingBox,
                canUseSelectedInventoryItem: slot.CanUse,
                canDropSelectedInventoryItem: slot.CanDrop);
            ContextPointerTarget target = new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stackId,
                new CellId(resident.CellX, resident.CellY, resident.CellZ));
            ContextInputDecision decision = _inputRouter.Route(
                new ContextPointerEvent(
                    PointerInputSurface.ResidentInventory,
                    button,
                    altPressed: altPressed),
                state,
                target);
            ApplyDecision(decision);
        }
    }
}