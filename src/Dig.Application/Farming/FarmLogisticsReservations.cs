using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Farming;

namespace Dig.Application.Farming
{

public enum FarmLogisticsDirection
{
    Incoming = 0,
    Outgoing = 1,
}

public readonly struct FarmLogisticsReservation
{
    public FarmLogisticsReservation(
        EntityId jobId,
        EntityId buildingId,
        FarmDeliveryKind kind,
        int quantity,
        FarmLogisticsDirection direction)
    {
        if (jobId.IsEmpty) throw new ArgumentException("Job id is required.", nameof(jobId));
        if (buildingId.IsEmpty) throw new ArgumentException("Building id is required.", nameof(buildingId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        JobId = jobId;
        BuildingId = buildingId;
        Kind = kind;
        Quantity = quantity;
        Direction = direction;
    }

    public EntityId JobId { get; }
    public EntityId BuildingId { get; }
    public FarmDeliveryKind Kind { get; }
    public int Quantity { get; }
    public FarmLogisticsDirection Direction { get; }
}

/// <summary>
/// Tracks farm stock/product quantities already assigned to transport jobs.
/// Farm delivery demands are dynamic, so the farm must not create a second
/// hauling job while the first resident is still carrying the same stock.
/// </summary>
public sealed class FarmLogisticsReservations
{
    private readonly Dictionary<EntityId, FarmLogisticsReservation> _byJob =
        new Dictionary<EntityId, FarmLogisticsReservation>();

    public IReadOnlyCollection<FarmLogisticsReservation> GetAll()
    {
        return _byJob.Values
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public bool TryGet(EntityId jobId, out FarmLogisticsReservation reservation)
    {
        return _byJob.TryGetValue(jobId, out reservation);
    }

    public int GetReserved(
        EntityId buildingId,
        FarmDeliveryKind kind,
        FarmLogisticsDirection direction)
    {
        return _byJob.Values
            .Where(value => value.BuildingId == buildingId
                && value.Kind == kind
                && value.Direction == direction)
            .Sum(value => value.Quantity);
    }

    public int GetUnreservedIncoming(
        EntityId buildingId,
        FarmDeliveryKind kind,
        int demandedQuantity)
    {
        if (demandedQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(demandedQuantity));
        }

        return Math.Max(
            0,
            demandedQuantity - GetReserved(
                buildingId,
                kind,
                FarmLogisticsDirection.Incoming));
    }

    public int GetUnreservedOutgoing(
        EntityId buildingId,
        FarmDeliveryKind kind,
        int collectableQuantity)
    {
        if (collectableQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(collectableQuantity));
        }

        return Math.Max(
            0,
            collectableQuantity - GetReserved(
                buildingId,
                kind,
                FarmLogisticsDirection.Outgoing));
    }

    public bool TryReserveIncoming(
        EntityId jobId,
        EntityId buildingId,
        FarmDeliveryKind kind,
        int demandedQuantity,
        int quantity)
    {
        return TryReserve(
            jobId,
            buildingId,
            kind,
            demandedQuantity,
            quantity,
            FarmLogisticsDirection.Incoming);
    }

    public bool TryReserveOutgoing(
        EntityId jobId,
        EntityId buildingId,
        FarmDeliveryKind kind,
        int collectableQuantity,
        int quantity)
    {
        return TryReserve(
            jobId,
            buildingId,
            kind,
            collectableQuantity,
            quantity,
            FarmLogisticsDirection.Outgoing);
    }

    public bool Release(EntityId jobId)
    {
        return _byJob.Remove(jobId);
    }

    public int ReleaseForFarm(EntityId buildingId)
    {
        EntityId[] jobIds = _byJob.Values
            .Where(value => value.BuildingId == buildingId)
            .Select(value => value.JobId)
            .ToArray();
        foreach (EntityId jobId in jobIds)
        {
            _byJob.Remove(jobId);
        }

        return jobIds.Length;
    }

    private bool TryReserve(
        EntityId jobId,
        EntityId buildingId,
        FarmDeliveryKind kind,
        int availableQuantity,
        int quantity,
        FarmLogisticsDirection direction)
    {
        if (jobId.IsEmpty) throw new ArgumentException("Job id is required.", nameof(jobId));
        if (buildingId.IsEmpty) throw new ArgumentException("Building id is required.", nameof(buildingId));
        if (availableQuantity < 0) throw new ArgumentOutOfRangeException(nameof(availableQuantity));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (_byJob.ContainsKey(jobId)) return false;

        int alreadyReserved = GetReserved(buildingId, kind, direction);
        if (quantity > Math.Max(0, availableQuantity - alreadyReserved))
        {
            return false;
        }

        _byJob.Add(
            jobId,
            new FarmLogisticsReservation(jobId, buildingId, kind, quantity, direction));
        return true;
    }
}

}
