using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Inventory
{

public sealed class ResidentInventoryMigrationReport
{
    public ResidentInventoryMigrationReport(
        EntityId residentId,
        int slottedStackCount,
        IReadOnlyCollection<EntityId> spilledStackIds,
        EntityId? restoredHeldStackId)
    {
        ResidentId = residentId;
        SlottedStackCount = slottedStackCount;
        SpilledStackIds = new ReadOnlyCollection<EntityId>(
            (spilledStackIds ?? throw new ArgumentNullException(nameof(spilledStackIds)))
                .OrderBy(id => id.ToString(), StringComparer.Ordinal)
                .ToArray());
        RestoredHeldStackId = restoredHeldStackId;
    }

    public EntityId ResidentId { get; }
    public int SlottedStackCount { get; }
    public IReadOnlyList<EntityId> SpilledStackIds { get; }
    public EntityId? RestoredHeldStackId { get; }
}

public sealed partial class InventoryState
{
    public Result<ResidentInventoryMigrationReport> MigrateLegacyResidentInventory(
        EntityId residentId,
        CellId residentCell,
        long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(residentId);
        ItemStackState[] legacy = _stacks.Values
            .Where(stack => IsLegacyResidentStack(stack.Location, residentId))
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (legacy.Length == 0)
        {
            return Result<ResidentInventoryMigrationReport>.Success(
                new ResidentInventoryMigrationReport(
                    residentId,
                    slottedStackCount: 0,
                    Array.Empty<EntityId>(),
                    restoredHeldStackId: null));
        }

        if (legacy.Any(stack =>
            stack.ReservedQuantity != 0 || stack.HeldQuantity != 0))
        {
            return Result<ResidentInventoryMigrationReport>.Failure(
                InventoryErrors.ResidentInventoryLayoutInvalid);
        }

        EntityId? legacyEquipped = legacy
            .Where(stack => stack.Location.Kind == ItemLocationKind.Equipped)
            .Select(stack => (EntityId?)stack.Id)
            .FirstOrDefault();
        MigrationPlan plan = BuildLegacyMigrationPlan(legacy, residentId, residentCell);
        ApplyLegacyMigrationPlan(plan, tick);

        EntityId? restoredHeld = null;
        if (legacyEquipped.HasValue
            && plan.Destinations.TryGetValue(
                legacyEquipped.Value,
                out ItemLocation heldDestination)
            && heldDestination.Kind == ItemLocationKind.AgentInventory)
        {
            ItemStackState heldStack = Find(legacyEquipped.Value)!;
            if (Catalog.Get(heldStack.ItemId).IsTool)
            {
                Result held = HoldItem(
                    residentId,
                    heldStack.Id,
                    1,
                    HeldItemPurpose.ToolUse,
                    tick);
                if (held.IsFailure)
                {
                    throw new InvalidOperationException(
                        "Legacy held item restoration failed after migration preflight.");
                }

                restoredHeld = heldStack.Id;
            }
        }

        return Result<ResidentInventoryMigrationReport>.Success(
            new ResidentInventoryMigrationReport(
                residentId,
                plan.SlottedCount,
                plan.SpilledStackIds,
                restoredHeld));
    }

    private MigrationPlan BuildLegacyMigrationPlan(
        IReadOnlyCollection<ItemStackState> legacy,
        EntityId residentId,
        CellId residentCell)
    {
        List<ItemStackState> expansions = legacy
            .Where(stack => Catalog.Get(stack.ItemId).IsInventoryExpansion)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        List<ItemStackState> ordinary = legacy
            .Where(stack => !Catalog.Get(stack.ItemId).IsInventoryExpansion)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        Dictionary<EntityId, ItemLocation> destinations =
            new Dictionary<EntityId, ItemLocation>();
        int mainIndex = 0;
        foreach (ItemStackState expansion in expansions.Take(
            ResidentInventoryLayoutSnapshot.MainSlotCount))
        {
            destinations.Add(
                expansion.Id,
                ItemLocation.InResidentSlot(
                    residentId,
                    ResidentInventoryCompartment.Main,
                    mainIndex++));
        }

        int ordinaryMainCount = Math.Min(
            ResidentInventoryLayoutSnapshot.MainSlotCount - mainIndex,
            ordinary.Count);
        for (int index = 0; index < ordinaryMainCount; index++)
        {
            destinations.Add(
                ordinary[index].Id,
                ItemLocation.InResidentSlot(
                    residentId,
                    ResidentInventoryCompartment.Main,
                    mainIndex++));
        }

        ActiveMigrationExpansion cargo = ResolveMigrationExpansion(
            expansions,
            destinations,
            InventoryExpansionGroup.Cargo);
        ActiveMigrationExpansion weapon = ResolveMigrationExpansion(
            expansions,
            destinations,
            InventoryExpansionGroup.Weapon);
        int cargoIndex = 0;
        int weaponIndex = 0;
        foreach (ItemStackState stack in ordinary.Skip(ordinaryMainCount))
        {
            ItemDefinition definition = Catalog.Get(stack.ItemId);
            if (weapon.Accepts(definition) && weaponIndex < weapon.Capacity)
            {
                destinations.Add(
                    stack.Id,
                    ItemLocation.InResidentSlot(
                        residentId,
                        ResidentInventoryCompartment.Weapon,
                        weaponIndex++));
            }
            else if (cargo.Accepts(definition) && cargoIndex < cargo.Capacity)
            {
                destinations.Add(
                    stack.Id,
                    ItemLocation.InResidentSlot(
                        residentId,
                        ResidentInventoryCompartment.Cargo,
                        cargoIndex++));
            }
        }

        ItemLocation world = ItemLocation.InWorld(residentCell);
        EntityId[] spilled = legacy
            .Where(stack => !destinations.ContainsKey(stack.Id))
            .Select(stack => stack.Id)
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < spilled.Length; index++)
        {
            destinations.Add(spilled[index], world);
        }

        return new MigrationPlan(
            destinations,
            legacy.Count - spilled.Length,
            spilled);
    }

    private ActiveMigrationExpansion ResolveMigrationExpansion(
        IEnumerable<ItemStackState> expansions,
        IReadOnlyDictionary<EntityId, ItemLocation> destinations,
        InventoryExpansionGroup group)
    {
        ItemStackState? active = expansions
            .Where(stack => destinations.ContainsKey(stack.Id))
            .Where(stack => Catalog.Get(stack.ItemId).InventoryExpansion!.Group == group)
            .OrderByDescending(stack =>
                Catalog.Get(stack.ItemId).InventoryExpansion!.Tier)
            .ThenBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        return active is null
            ? ActiveMigrationExpansion.Empty
            : new ActiveMigrationExpansion(
                Catalog.Get(active.ItemId).InventoryExpansion!);
    }

    private void ApplyLegacyMigrationPlan(MigrationPlan plan, long tick)
    {
        foreach (KeyValuePair<EntityId, ItemLocation> move in plan.Destinations
            .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            ItemStackState stack = Find(move.Key)!;
            ItemLocation source = stack.Location;
            stack.MoveFull(move.Value);
            Raise(new ItemStackMoved(
                tick,
                stack.Id,
                stack.Id,
                stack.ItemId,
                stack.Quantity,
                source,
                move.Value));
        }

        IncrementVersion();
    }

    private static bool IsLegacyResidentStack(
        ItemLocation location,
        EntityId residentId)
    {
        return location.HasOwner
            && location.OwnerId == residentId
            && (location.Kind == ItemLocationKind.Equipped
                || (location.Kind == ItemLocationKind.AgentInventory
                    && !location.HasResidentSlot));
    }

    private sealed class MigrationPlan
    {
        public MigrationPlan(
            IReadOnlyDictionary<EntityId, ItemLocation> destinations,
            int slottedCount,
            IReadOnlyCollection<EntityId> spilledStackIds)
        {
            Destinations = destinations;
            SlottedCount = slottedCount;
            SpilledStackIds = spilledStackIds;
        }

        public IReadOnlyDictionary<EntityId, ItemLocation> Destinations { get; }
        public int SlottedCount { get; }
        public IReadOnlyCollection<EntityId> SpilledStackIds { get; }
    }

    private readonly struct ActiveMigrationExpansion
    {
        public static ActiveMigrationExpansion Empty => default;

        public ActiveMigrationExpansion(InventoryExpansionDefinition definition)
        {
            Definition = definition;
        }

        private InventoryExpansionDefinition? Definition { get; }
        public int Capacity => Definition?.AddedSlots ?? 0;
        public bool Accepts(ItemDefinition item) => Definition?.Accepts(item) == true;
    }
}

}