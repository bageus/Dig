using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public static class BuildingSupplyErrors
{
    public static readonly DomainError WorkstationNotFound = new DomainError(
        "production.supply.workstation_not_found",
        "The building is not registered as a production workstation.");
    public static readonly DomainError SupplyAlreadyActive = new DomainError(
        "production.supply.already_active",
        "The building already has an active supply job.");
    public static readonly DomainError CapacityExceeded = new DomainError(
        "production.supply.capacity_exceeded",
        "Incoming supply would exceed internal stock capacity.");
    public static readonly DomainError SupplyJobMismatch = new DomainError(
        "production.supply.job_mismatch",
        "The supply job does not own this workstation supply request.");
}

public readonly struct BuildingStockSnapshot
{
    public BuildingStockSnapshot(
        ItemId itemId,
        int capacity,
        int current,
        int incoming,
        bool deliveryEnabled,
        int priority)
    {
        ItemId = itemId;
        Capacity = capacity;
        Current = current;
        Incoming = incoming;
        DeliveryEnabled = deliveryEnabled;
        Priority = priority;
    }

    public ItemId ItemId { get; }
    public int Capacity { get; }
    public int Current { get; }
    public int Incoming { get; }
    public bool DeliveryEnabled { get; }
    public int Priority { get; }
    public int Missing => Math.Max(0, Capacity - Current - Incoming);
}

public sealed class BuildingSupplySnapshot
{
    public BuildingSupplySnapshot(
        EntityId buildingId,
        ProductionWorkstationDefinition definition,
        IReadOnlyCollection<BuildingStockSnapshot> stocks,
        EntityId? activeSupplyJobId)
    {
        BuildingId = buildingId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Stocks = new ReadOnlyCollection<BuildingStockSnapshot>(stocks
            .OrderByDescending(value => value.Priority)
            .ThenBy(value => value.ItemId)
            .ToArray());
        ActiveSupplyJobId = activeSupplyJobId;
    }

    public EntityId BuildingId { get; }
    public ProductionWorkstationDefinition Definition { get; }
    public IReadOnlyList<BuildingStockSnapshot> Stocks { get; }
    public EntityId? ActiveSupplyJobId { get; }
    public bool HasActiveSupply => ActiveSupplyJobId.HasValue;
}

public sealed class BuildingSupplyState : AggregateRoot
{
    private readonly Dictionary<EntityId, WorkstationSupplyEntry> _entries =
        new Dictionary<EntityId, WorkstationSupplyEntry>();

    public Result Register(
        EntityId buildingId,
        ProductionWorkstationDefinition definition,
        long tick)
    {
        ValidateTick(tick);
        if (buildingId.IsEmpty || definition is null)
        {
            throw new ArgumentException("Building and workstation definition are required.");
        }

        if (_entries.ContainsKey(buildingId))
        {
            return Result.Success();
        }

        _entries.Add(buildingId, new WorkstationSupplyEntry(definition));
        return Result.Success();
    }

    public Result SetDeliveryEnabled(
        EntityId buildingId,
        ItemId itemId,
        bool enabled,
        long tick)
    {
        ValidateTick(tick);
        WorkstationSupplyEntry? entry = Find(buildingId);
        if (entry is null)
        {
            return Result.Failure(BuildingSupplyErrors.WorkstationNotFound);
        }

        entry.SetDeliveryEnabled(itemId, enabled);
        return Result.Success();
    }

    public Result EnableProductionInputDelivery(
        EntityId buildingId,
        IReadOnlyCollection<ItemConsumptionRequest> inputs,
        long tick)
    {
        ValidateTick(tick);
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        WorkstationSupplyEntry? entry = Find(buildingId);
        if (entry is null)
        {
            return Result.Failure(BuildingSupplyErrors.WorkstationNotFound);
        }

        ItemId[] required = inputs
            .Where(value => value.Quantity > 0)
            .Select(value => value.ItemId)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        foreach (ItemId itemId in required)
        {
            entry.Definition.GetStockRule(itemId);
        }

        foreach (ItemId itemId in required)
        {
            entry.SetDeliveryEnabled(itemId, enabled: true);
        }

        return Result.Success();
    }

    public Result ReserveIncoming(
        EntityId buildingId,
        EntityId jobId,
        IReadOnlyCollection<ItemConsumptionRequest> quantities,
        IReadOnlyDictionary<ItemId, int> currentQuantities,
        long tick)
    {
        ValidateTick(tick);
        WorkstationSupplyEntry? entry = Find(buildingId);
        if (entry is null)
        {
            return Result.Failure(BuildingSupplyErrors.WorkstationNotFound);
        }

        if (entry.ActiveJobId.HasValue)
        {
            return Result.Failure(BuildingSupplyErrors.SupplyAlreadyActive);
        }

        Dictionary<ItemId, int> normalized = quantities
            .GroupBy(value => value.ItemId)
            .ToDictionary(group => group.Key, group => group.Sum(value => value.Quantity));
        foreach (KeyValuePair<ItemId, int> pair in normalized)
        {
            InternalStockRuleDefinition rule = entry.Definition.GetStockRule(pair.Key);
            int current = currentQuantities.TryGetValue(pair.Key, out int value) ? value : 0;
            if (current + pair.Value > rule.Capacity)
            {
                return Result.Failure(BuildingSupplyErrors.CapacityExceeded);
            }
        }

        entry.Reserve(jobId, normalized);
        return Result.Success();
    }

    public Result CompleteSupply(EntityId buildingId, EntityId jobId, long tick)
    {
        return EndSupply(buildingId, jobId, tick);
    }

    public Result ReleaseSupply(EntityId buildingId, EntityId jobId, long tick)
    {
        return EndSupply(buildingId, jobId, tick);
    }

    public BuildingSupplySnapshot? Get(
        EntityId buildingId,
        InventorySnapshot inventory)
    {
        WorkstationSupplyEntry? entry = Find(buildingId);
        return entry?.CreateSnapshot(buildingId, inventory);
    }

    public IReadOnlyList<BuildingSupplySnapshot> GetAll(InventorySnapshot inventory)
    {
        return new ReadOnlyCollection<BuildingSupplySnapshot>(_entries
            .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
            .Select(pair => pair.Value.CreateSnapshot(pair.Key, inventory))
            .ToArray());
    }

    public bool IsProtectedAutomaticSource(ItemLocation location)
    {
        return location.Kind == ItemLocationKind.BuildingInventory
            && location.HasOwner
            && _entries.ContainsKey(location.OwnerId);
    }

    private Result EndSupply(EntityId buildingId, EntityId jobId, long tick)
    {
        ValidateTick(tick);
        WorkstationSupplyEntry? entry = Find(buildingId);
        if (entry is null)
        {
            return Result.Failure(BuildingSupplyErrors.WorkstationNotFound);
        }

        if (entry.ActiveJobId != jobId)
        {
            return Result.Failure(BuildingSupplyErrors.SupplyJobMismatch);
        }

        entry.Clear();
        return Result.Success();
    }

    private WorkstationSupplyEntry? Find(EntityId buildingId)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        return _entries.TryGetValue(buildingId, out WorkstationSupplyEntry? value)
            ? value
            : null;
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
    }

    private sealed class WorkstationSupplyEntry
    {
        private readonly Dictionary<ItemId, bool> _delivery =
            new Dictionary<ItemId, bool>();
        private readonly Dictionary<ItemId, int> _incoming =
            new Dictionary<ItemId, int>();

        public WorkstationSupplyEntry(ProductionWorkstationDefinition definition)
        {
            Definition = definition;
            foreach (InternalStockRuleDefinition rule in definition.StockRules)
            {
                _delivery.Add(rule.ItemId, rule.DefaultDeliveryEnabled);
            }
        }

        public ProductionWorkstationDefinition Definition { get; }
        public EntityId? ActiveJobId { get; private set; }

        public void SetDeliveryEnabled(ItemId itemId, bool enabled)
        {
            Definition.GetStockRule(itemId);
            _delivery[itemId] = enabled;
        }

        public void Reserve(EntityId jobId, IReadOnlyDictionary<ItemId, int> quantities)
        {
            ActiveJobId = jobId;
            _incoming.Clear();
            foreach (KeyValuePair<ItemId, int> pair in quantities)
            {
                _incoming[pair.Key] = pair.Value;
            }
        }

        public void Clear()
        {
            ActiveJobId = null;
            _incoming.Clear();
        }

        public BuildingSupplySnapshot CreateSnapshot(
            EntityId buildingId,
            InventorySnapshot inventory)
        {
            ItemLocation location = ItemLocation.InBuilding(buildingId);
            return new BuildingSupplySnapshot(
                buildingId,
                Definition,
                Definition.StockRules.Select(rule => new BuildingStockSnapshot(
                    rule.ItemId,
                    rule.Capacity,
                    inventory.GetQuantityAt(rule.ItemId, location),
                    _incoming.TryGetValue(rule.ItemId, out int incoming) ? incoming : 0,
                    _delivery[rule.ItemId],
                    rule.Priority)).ToArray(),
                ActiveJobId);
        }
    }
}

}
