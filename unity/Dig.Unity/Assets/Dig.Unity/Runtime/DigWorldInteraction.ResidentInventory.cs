using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;

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
                altPressed: false);
        }

        internal void UseResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Left,
                altPressed: true);
        }

        internal void DropResidentInventorySlot(
            ResidentInventorySlotViewModel slot)
        {
            RouteResidentInventorySlot(
                slot,
                PointerButtonKind.Right,
                altPressed: false);
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
