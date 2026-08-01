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
            int requiredQuantity = ItemPickupQuantityPolicy
                .ResolveRequestedQuantity(stack);
            if (requiredQuantity <= 0)
            {
                return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
            }

            int capacity = 0;
            for (int index = 0; index < layout.Slots.Count; index++)
            {
                ResidentInventorySlotSnapshot slot = layout.Slots[index];
                if (!CanAcceptPickup(definition, slot.Slot.Compartment, layout))
                {
                    continue;
                }

                if (slot.IsEmpty)
                {
                    capacity = checked(capacity + 1);
                }

                if (capacity >= requiredQuantity)
                {
                    return Result.Success();
                }
            }

            return Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
        }

        private static bool CanAcceptPickup(
            ItemDefinition definition,
            ResidentInventoryCompartment compartment,
            ResidentInventoryLayoutSnapshot layout)
        {
            if (definition.IsInventoryExpansion)
            {
                return compartment == ResidentInventoryCompartment.Main;
            }

            return compartment switch
            {
                ResidentInventoryCompartment.Main => true,
                ResidentInventoryCompartment.Cargo =>
                    layout.ActiveCargoExpansion.HasValue
                    && layout.ActiveCargoExpansion.Value.Definition.Accepts(definition),
                ResidentInventoryCompartment.Weapon =>
                    layout.ActiveWeaponExpansion.HasValue
                    && layout.ActiveWeaponExpansion.Value.Definition.Accepts(definition),
                _ => false,
            };
        }
    }
}
