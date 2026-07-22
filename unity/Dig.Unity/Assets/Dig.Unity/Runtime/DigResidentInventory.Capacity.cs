using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal Result ValidateResidentCanPickupStack(
            string residentId,
            string stackId)
        {
            EntityId resident = ParseInventoryEntityId(residentId, nameof(residentId));
            EntityId stackEntity = ParseInventoryEntityId(stackId, nameof(stackId));

            InventoryState? inventory = null;
            ItemStackSnapshot? stack = _buildingInventoryRepository?.Get().GetStack(stackEntity);
            if (stack != null)
            {
                inventory = _buildingInventoryRepository!.Get();
            }
            else
            {
                stack = _inventoryRepository.Get().GetStack(stackEntity);
                if (stack != null)
                {
                    inventory = _inventoryRepository.Get();
                }
            }

            if (inventory == null || stack == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            ResidentInventoryLayoutSnapshot layout =
                inventory.GetResidentInventoryLayout(resident);
            ItemDefinition definition = inventory.Catalog.Get(stack.ItemId);

            if (definition.IsInventoryExpansion)
            {
                return HasEmptySlot(layout, ResidentInventoryCompartment.Main)
                    ? Result.Success()
                    : Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
            }

            if (layout.ActiveWeaponExpansion.HasValue
                && layout.ActiveWeaponExpansion.Value.Definition.Accepts(definition)
                && HasEmptySlot(layout, ResidentInventoryCompartment.Weapon))
            {
                return Result.Success();
            }

            if (HasEmptySlot(layout, ResidentInventoryCompartment.Main))
            {
                return Result.Success();
            }

            if (layout.ActiveCargoExpansion.HasValue
                && layout.ActiveCargoExpansion.Value.Definition.Accepts(definition)
                && HasEmptySlot(layout, ResidentInventoryCompartment.Cargo))
            {
                return Result.Success();
            }

            return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
        }

        private static bool HasEmptySlot(
            ResidentInventoryLayoutSnapshot layout,
            ResidentInventoryCompartment compartment)
        {
            for (int index = 0; index < layout.Slots.Count; index++)
            {
                ResidentInventorySlotSnapshot slot = layout.Slots[index];
                if (slot.Slot.Compartment == compartment && slot.IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
