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
        JobRetryPolicy retryPolicy)
        : base(id, priority, createdTick, retryPolicy, SupplyStages, dependencies: null)
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

        _transitStackIds = NormalizeIds(transitStackIds, nameof(transitStackIds));
        _depositStackIds = NormalizeIds(depositStackIds, nameof(depositStackIds));
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

    public EntityId BuildingId { get; }
    public CellId WorkPosition { get; }

    public IReadOnlyList<ItemReservationAllocation> Allocations =>
        new ReadOnlyCollection<ItemReservationAllocation>(_allocations);

    public IReadOnlyList<EntityId> TransitStackIds =>
        new ReadOnlyCollection<EntityId>(_transitStackIds);

    public IReadOnlyList<EntityId> DepositStackIds =>
        new ReadOnlyCollection<EntityId>(_depositStackIds);


    private static EntityId[] NormalizeIds(
        IEnumerable<EntityId> ids,
        string parameterName)
    {
        if (ids is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        EntityId[] values = ids.ToArray();
        if (values.Length == 0
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
        $"SupplyBuilding:{BuildingId}:{_allocations.Sum(value => value.Quantity)}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForDestination(BuildingId),
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}

}
