using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result MoveAvailableToResidentSlot(
        EntityId stackId,
        int quantity,
        EntityId residentId,
        ResidentInventorySlot slot,
        EntityId splitStackId,
        long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(residentId);
        ItemStackState? source = Find(stackId);
        if (source is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (quantity <= 0 || quantity > source.AvailableQuantity)
        {
            return Result.Failure(quantity <= 0
                ? InventoryErrors.InvalidQuantity
                : InventoryErrors.InsufficientAvailableQuantity);
        }

        ItemLocation destination = ItemLocation.InResidentSlot(
            residentId,
            slot.Compartment,
            slot.Index);
        Result sourceValidation = ValidateResidentSourceChange(source, destination);
        if (sourceValidation.IsFailure)
        {
            return sourceValidation;
        }

        Result destinationValidation = ValidateResidentDestination(
            source,
            destination,
            allowCompatibleOccupant: true);
        if (destinationValidation.IsFailure)
        {
            return destinationValidation;
        }

        ItemStackState? target = FindStackAt(destination, source.Id);
        if (target is null)
        {
            return MoveAvailable(
                stackId,
                quantity,
                destination,
                splitStackId,
                tick);
        }

        if (target.ItemId != source.ItemId)
        {
            return Result.Failure(InventoryErrors.ResidentSlotOccupied);
        }

        ItemDefinition definition = Catalog.Get(source.ItemId);
        if (target.Quantity + quantity > definition.MaximumStackSize)
        {
            return Result.Failure(InventoryErrors.StackSizeExceeded);
        }

        ItemLocation sourceLocation = source.Location;
        source.ConsumeAvailable(quantity);
        target.AddQuantity(quantity);
        if (source.Quantity == 0)
        {
            _stacks.Remove(source.Id);
        }

        IncrementVersion();
        Raise(new ItemStackMoved(
            tick,
            source.Id,
            target.Id,
            source.ItemId,
            quantity,
            sourceLocation,
            destination));
        return Result.Success();
    }

    private Result ValidateResidentMove(
        ItemStackState stack,
        ItemLocation destination,
        bool allowCompatibleOccupant = false)
    {
        Result source = ValidateResidentSourceChange(stack, destination);
        return source.IsFailure
            ? source
            : ValidateResidentDestination(stack, destination, allowCompatibleOccupant);
    }

    private Result ValidateResidentDestination(
        ItemStackState stack,
        ItemLocation destination,
        bool allowCompatibleOccupant)
    {
        if (destination.Kind != ItemLocationKind.AgentInventory
            || !destination.HasResidentSlot)
        {
            return Result.Success();
        }

        EntityId residentId = destination.OwnerId;
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(residentId);
        if (stack.Location.Kind == ItemLocationKind.AgentInventory
            && stack.Location.HasOwner
            && stack.Location.OwnerId == residentId
            && stack.Location.HasResidentSlot)
        {
            occupied.Remove(stack.Location.ResidentSlot);
        }

        ResidentInventorySlot destinationSlot = destination.ResidentSlot;
        if (occupied.TryGetValue(destinationSlot, out ItemStackState? existing))
        {
            bool compatible = allowCompatibleOccupant
                && existing.ItemId == stack.ItemId
                && !Catalog.Get(stack.ItemId).IsInventoryExpansion;
            if (!compatible)
            {
                return Result.Failure(InventoryErrors.ResidentSlotOccupied);
            }
        }
        else
        {
            occupied.Add(destinationSlot, stack);
        }

        ActiveInventoryExpansionSnapshot? activeCargo = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Cargo);
        ActiveInventoryExpansionSnapshot? activeWeapon = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Weapon);
        return ValidatePlacedStack(
            stack,
            destinationSlot,
            activeCargo?.Definition.AddedSlots ?? 0,
            activeWeapon?.Definition.AddedSlots ?? 0,
            activeCargo,
            activeWeapon);
    }

    private Result ValidateResidentSourceChange(
        ItemStackState stack,
        ItemLocation destination)
    {
        ItemLocation source = stack.Location;
        if (source.Kind != ItemLocationKind.AgentInventory
            || !source.HasResidentSlot
            || source == destination)
        {
            return Result.Success();
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (!definition.IsInventoryExpansion
            || source.ResidentCompartment != ResidentInventoryCompartment.Main)
        {
            return Result.Success();
        }

        if (destination.Kind == ItemLocationKind.AgentInventory
            && destination.HasOwner
            && destination.OwnerId == source.OwnerId
            && destination.HasResidentSlot
            && destination.ResidentCompartment == ResidentInventoryCompartment.Main)
        {
            return Result.Success();
        }

        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(source.OwnerId);
        ActiveInventoryExpansionSnapshot? active = ResolveActiveExpansion(
            occupied,
            definition.InventoryExpansion!.Group);
        if (!active.HasValue || active.Value.StackId != stack.Id)
        {
            return Result.Success();
        }

        ResidentInventoryCompartment compartment =
            definition.InventoryExpansion.Group == InventoryExpansionGroup.Cargo
                ? ResidentInventoryCompartment.Cargo
                : ResidentInventoryCompartment.Weapon;
        bool hasClaims = _residentSlotClaims.Any(claim =>
            claim.ResidentId == source.OwnerId
            && claim.Slot.Compartment == compartment);
        if (hasClaims)
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimConflict);
        }

        bool hasContents = occupied.Keys.Any(slot => slot.Compartment == compartment);
        return hasContents
            ? Result.Failure(InventoryErrors.ResidentInventorySpillRequired)
            : Result.Success();
    }

    private ItemStackState? FindStackAt(ItemLocation location, EntityId excludedStackId)
    {
        return _stacks.Values.FirstOrDefault(stack =>
            stack.Id != excludedStackId && stack.Location == location);
    }

    private Result ValidateResidentLocationForNewStack(
        ItemDefinition definition,
        ItemLocation destination)
    {
        if (destination.Kind != ItemLocationKind.AgentInventory
            || !destination.HasResidentSlot)
        {
            return Result.Success();
        }

        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(destination.OwnerId);
        ResidentInventorySlot slot = destination.ResidentSlot;
        if (occupied.ContainsKey(slot))
        {
            return Result.Failure(InventoryErrors.ResidentSlotOccupied);
        }

        int cargoCapacity = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Cargo)?.Definition.AddedSlots ?? 0;
        int weaponCapacity = ResolveActiveExpansion(
            occupied,
            InventoryExpansionGroup.Weapon)?.Definition.AddedSlots ?? 0;
        int capacity = GetCompartmentCapacity(
            slot.Compartment,
            cargoCapacity,
            weaponCapacity);
        if (slot.Index >= capacity)
        {
            return Result.Failure(InventoryErrors.ResidentSlotOutOfRange);
        }

        if (definition.IsInventoryExpansion)
        {
            return slot.Compartment == ResidentInventoryCompartment.Main
                ? Result.Success()
                : Result.Failure(InventoryErrors.InventoryExpansionMainOnly);
        }

        ActiveInventoryExpansionSnapshot? active = slot.Compartment switch
        {
            ResidentInventoryCompartment.Cargo => ResolveActiveExpansion(
                occupied,
                InventoryExpansionGroup.Cargo),
            ResidentInventoryCompartment.Weapon => ResolveActiveExpansion(
                occupied,
                InventoryExpansionGroup.Weapon),
            _ => null,
        };
        return slot.Compartment == ResidentInventoryCompartment.Main
            || (active.HasValue && active.Value.Definition.Accepts(definition))
                ? Result.Success()
                : Result.Failure(InventoryErrors.ResidentSlotCategoryRejected);
    }
}

}