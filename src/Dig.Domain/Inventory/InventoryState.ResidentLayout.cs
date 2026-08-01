using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public ResidentInventoryLayoutSnapshot GetResidentInventoryLayout(EntityId residentId)
    {
        ValidateResidentId(residentId);
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(residentId);
        ActiveInventoryExpansionSnapshot? activeCargo = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Cargo);
        ActiveInventoryExpansionSnapshot? activeWeapon = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Weapon);
        int cargoCapacity = activeCargo?.Definition.AddedSlots ?? 0;
        int weaponCapacity = activeWeapon?.Definition.AddedSlots ?? 0;
        List<ResidentInventorySlotSnapshot> slots = new List<ResidentInventorySlotSnapshot>();
        AddSlotSnapshots(
            ResidentInventoryCompartment.Weapon,
            weaponCapacity,
            occupied,
            activeCargo,
            activeWeapon,
            slots);
        AddSlotSnapshots(
            ResidentInventoryCompartment.Main,
            ResidentInventoryLayoutSnapshot.MainSlotCount,
            occupied,
            activeCargo,
            activeWeapon,
            slots);
        AddSlotSnapshots(
            ResidentInventoryCompartment.Cargo,
            cargoCapacity,
            occupied,
            activeCargo,
            activeWeapon,
            slots);
        return new ResidentInventoryLayoutSnapshot(
            residentId,
            cargoCapacity,
            weaponCapacity,
            activeCargo,
            activeWeapon,
            slots);
    }

    public double GetResidentMoveSpeedMultiplier(EntityId residentId)
    {
        ValidateResidentId(residentId);
        ItemStackState? activeCargoStack = null;
        InventoryExpansionDefinition? activeCargoDefinition = null;
        bool cargoOccupied = false;
        foreach (ItemStackState stack in _stacks.Values)
        {
            ItemLocation location = stack.Location;
            if (location.Kind != ItemLocationKind.AgentInventory
                || !location.HasOwner
                || location.OwnerId != residentId
                || !location.HasResidentSlot)
            {
                continue;
            }

            if (location.ResidentCompartment == ResidentInventoryCompartment.Cargo)
            {
                cargoOccupied = true;
                continue;
            }

            if (location.ResidentCompartment != ResidentInventoryCompartment.Main)
            {
                continue;
            }

            InventoryExpansionDefinition? expansion =
                Catalog.Get(stack.ItemId).InventoryExpansion;
            if (expansion?.Group != InventoryExpansionGroup.Cargo)
            {
                continue;
            }

            if (activeCargoDefinition is null
                || expansion.Tier > activeCargoDefinition.Tier
                || (expansion.Tier == activeCargoDefinition.Tier
                    && string.Compare(
                        stack.Id.ToString(),
                        activeCargoStack!.Id.ToString(),
                        StringComparison.Ordinal) < 0))
            {
                activeCargoStack = stack;
                activeCargoDefinition = expansion;
            }
        }

        return !cargoOccupied || activeCargoDefinition is null
            ? 1d
            : activeCargoDefinition.MoveSpeedMultiplierWhenOccupied;
    }
}

}
