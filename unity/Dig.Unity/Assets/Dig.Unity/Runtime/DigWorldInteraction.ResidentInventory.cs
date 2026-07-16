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
            if (slot == null)
            {
                throw new System.ArgumentNullException(nameof(slot));
            }

            if (_agentRenderer?.SelectedModel == null || _hud == null)
            {
                _hud?.SetStatus("input.inventory.resident_not_selected");
                return;
            }

            if (!slot.CanStartPlacement)
            {
                _hud.SetStatus("input.inventory.stack_unavailable");
                return;
            }

            var resident = _agentRenderer.SelectedModel;
            EntityId residentId = EntityId.Parse(resident.Id);
            EntityId stackId = EntityId.Parse(slot.StackId);
            ContextInputState state = new ContextInputState(
                selectedResidentId: residentId,
                selectedResidentAlive: resident.IsAlive,
                selectedInventoryStackId: stackId,
                selectedInventoryItemIsBuildingBox: slot.IsBuildingBox);
            ContextPointerTarget target = new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                stackId,
                new CellId(resident.CellX, resident.CellY));
            ContextInputDecision decision = _inputRouter.Route(
                new ContextPointerEvent(
                    PointerInputSurface.ResidentInventory,
                    PointerButtonKind.Left),
                state,
                target);
            ApplyDecision(decision);
        }
    }
}
