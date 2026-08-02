using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class BuildingSupplyJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] SupplyStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.AcquireItem,
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
    };

    private readonly ItemReservationAllocation[] _allocations;
    private readonly ItemConsumptionRequest[] _requestedItems;
    private readonly EntityId[] _transitStackIds;
    private readonly EntityId[] _depositStackIds;

    public BuildingSupplyJobDefinition(
        EntityId id,
        EntityId buildingId,
        CellId workPosition,
        IEnumerable<ItemReservationAllocation> allocations,
        IEnumerable<EntityId> transitStackIds,
        IEnumerable<EntityId> depositStackIds,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, SupplyStages, dependencies)
    {
        if (buildingId.IsEmpty || allocations is null)
        {
            throw new ArgumentException("Building and supply allocations are required.");
        }

        _allocations = allocations
            .OrderBy(value => value.ItemId)
            .ThenBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (_allocations.Length == 0
            || _allocations.Select(value => value.StackId).Distinct().Count()
                != _allocations.Length)
        {
            throw new ArgumentException(
                "Building supply allocations must be non-empty and source-unique.",
                nameof(allocations));
        }

        _requestedItems = _allocations
            .GroupBy(value => value.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                group.Sum(value => value.Quantity)))
            .OrderBy(value => value.ItemId)
            .ToArray();

        _transitStackIds = NormalizeIds(
            transitStackIds,
            nameof(transitStackIds),
            allowEmpty: true);
        _depositStackIds = NormalizeIds(
            depositStackIds,
            nameof(depositStackIds),
            allowEmpty: false);
        if (_depositStackIds.Length != _allocations.Select(value => value.ItemId)
            .Distinct().Count())
        {
            throw new ArgumentException(
                "One deposit stack id is required per supplied item type.",
                nameof(depositStackIds));
        }

        BuildingId = buildingId;
        WorkPosition = workPosition;
    }

    public BuildingSupplyJobDefinition(
        EntityId id,
        EntityId buildingId,
        CellId workPosition,
        IEnumerable<ItemConsumptionRequest> requestedItems,
        IEnumerable<EntityId> transitStackIds,
        IEnumerable<EntityId> depositStackIds,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId> dependencies)
        : base(id, priority, createdTick, retryPolicy, SupplyStages, dependencies)
    {
        if (buildingId.IsEmpty || requestedItems is null)
        {
            throw new ArgumentException("Building and requested supply are required.");
        }

        _requestedItems = requestedItems
            .GroupBy(value => value.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                group.Sum(value => value.Quantity)))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (_requestedItems.Length == 0
            || _requestedItems.Any(value => value.ItemId.IsEmpty || value.Quantity <= 0)
            || Dependencies.Count == 0)
        {
            throw new ArgumentException(
                "Deferred building supply needs requested items and dependencies.",
                nameof(requestedItems));
        }

        _allocations = Array.Empty<ItemReservationAllocation>();
        _transitStackIds = NormalizeIds(
            transitStackIds,
            nameof(transitStackIds),
            allowEmpty: true);
        _depositStackIds = NormalizeIds(
            depositStackIds,
            nameof(depositStackIds),
            allowEmpty: false);
        if (_depositStackIds.Length != _requestedItems.Length)
        {
            throw new ArgumentException(
                "One deposit stack id is required per requested item type.",
                nameof(depositStackIds));
        }

        BuildingId = buildingId;
        WorkPosition = workPosition;
    }

    public EntityId BuildingId { get; }
    public CellId WorkPosition { get; }

    public IReadOnlyList<ItemReservationAllocation> Allocations =>
        new ReadOnlyCollection<ItemReservationAllocation>(_allocations);

    public IReadOnlyList<ItemConsumptionRequest> RequestedItems =>
        new ReadOnlyCollection<ItemConsumptionRequest>(_requestedItems);

    public bool IsSourceResolved => _allocations.Length > 0;

    public IReadOnlyList<EntityId> TransitStackIds =>
        new ReadOnlyCollection<EntityId>(_transitStackIds);

    public IReadOnlyList<EntityId> DepositStackIds =>
        new ReadOnlyCollection<EntityId>(_depositStackIds);


    private static EntityId[] NormalizeIds(
        IEnumerable<EntityId> ids,
        string parameterName,
        bool allowEmpty)
    {
        if (ids is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        EntityId[] values = ids.ToArray();
        if ((!allowEmpty && values.Length == 0)
            || values.Any(value => value.IsEmpty)
            || values.Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Stack ids must be non-empty and unique.",
                parameterName);
        }

        return values;
    }

    public override string Description =>
        $"SupplyBuilding:{BuildingId}:{_requestedItems.Sum(value => value.Quantity)}"
        + (IsSourceResolved ? string.Empty : ":AwaitingDependencyOutput");

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        // Production and supply share the building destination reservation so one
        // workstation has exactly one active operation owner. Supply does not need
        // the craft-position reservation because movement occupancy remains runtime-owned.
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForDestination(BuildingId),
        });
    }
}

}
