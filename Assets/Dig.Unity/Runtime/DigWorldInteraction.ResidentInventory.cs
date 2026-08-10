using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        internal void ActivateResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Left,
                altPressed: false,
                dropPressed: Input.GetKey(KeyCode.C));
        }

        internal void BeginResidentInventoryBuildingPlacement(
            ResidentInventoryLayoutSlotViewModel slot)
        {
            InteractResidentInventoryLayoutSlot(
                slot,
                altPressed: false,
                dropPressed: false);
        }

        internal void UseResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Left,
                altPressed: true,
                dropPressed: false);
        }

        internal void DropResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Left,
                altPressed: false,
                dropPressed: true);
        }

        private void ResetInventoryClickSequence()
        {
        }

        private void RouteResidentInventorySlot(
            ResidentInventorySlotViewModel slot,
            PointerButtonKind button,
            bool altPressed,
            bool dropPressed)
        {
            if (slot == null)
            {
                throw new System.ArgumentNullException(nameof(slot));
            }

            var resident = _agentRenderer?.SelectedModel;
            if (resident == null || _hud == null)
            {
                _hud?.SetStatus("input.inventory.resident_not_selected");
                return;
            }

            string? residentIdValue = resident.Id;
            string? stackIdValue = slot.StackId;
            if (string.IsNullOrWhiteSpace(residentIdValue)
                || string.IsNullOrWhiteSpace(stackIdValue))
            {
                _hud.SetStatus("input.inventory.resident_not_selected");
                return;
            }

            EntityId residentId = EntityId.Parse(residentIdValue ?? string.Empty);
            EntityId stackId = EntityId.Parse(stackIdValue ?? string.Empty);
            ContextInputState state = new ContextInputState(
                selectedResidentId: residentId,
                selectedResidentAlive: resident.IsAlive,
                selectedInventoryStackId: stackId,
                selectedInventoryItemUsable:
                    slot.InteractionProfile.SupportsInventoryAction(
                        ItemInventoryInteractionAction.DirectUse),
                selectedInventoryItemIsBuildingBox: slot.IsBuildingBox,
                canUseSelectedInventoryItem: slot.CanUse,
                canDropSelectedInventoryItem: slot.CanDrop,
                canPlaceSelectedInventoryItem: slot.CanPlace);
            ContextPointerTarget target = new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stackId,
                new CellId(resident.CellX, resident.CellY, resident.CellZ));
            ContextInputDecision decision = _inputRouter.Route(
                new ContextPointerEvent(
                    PointerInputSurface.ResidentInventory,
                    button,
                    altPressed: altPressed,
                    dropPressed: dropPressed),
                state,
                target);
            ApplyDecision(decision, legacyInventorySlot: slot);
        }
    }
}
