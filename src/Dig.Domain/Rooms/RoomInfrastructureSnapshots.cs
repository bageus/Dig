using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Rooms
{

public sealed class RoomMaterialLedgerSnapshot
{
    public RoomMaterialLedgerSnapshot(
        ItemId itemId,
        int required,
        int delivered,
        int consumed,
        int releasedOnCancel)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Room material ledger item id cannot be empty.", nameof(itemId));
        }

        if (required <= 0
            || delivered < 0
            || consumed < 0
            || releasedOnCancel < 0
            || consumed > delivered
            || delivered > required
            || releasedOnCancel > required)
        {
            throw new ArgumentOutOfRangeException(nameof(required));
        }

        ItemId = itemId;
        Required = required;
        Delivered = delivered;
        Consumed = consumed;
        ReleasedOnCancel = releasedOnCancel;
    }

    public ItemId ItemId { get; }

    public int Required { get; }

    public int Delivered { get; }

    public int Consumed { get; }

    public int ReleasedOnCancel { get; }

    public int RemainingDelivery => Required - Delivered;

    public int RemainingConsumption => Required - Consumed;
}

public sealed class RoomInfrastructureProjectSnapshot
{
    public RoomInfrastructureProjectSnapshot(
        EntityId roomInfrastructureId,
        string templateInstanceId,
        RoomTemplateKind templateKind,
        int upgradeOrderCount,
        RoomImprovementStatus status,
        bool cancellationLocked,
        RoomPurposeKind requestedPurpose,
        RoomPurposeKind activePurpose,
        CellId? temporaryStockCell,
        IEnumerable<RoomMaterialLedgerSnapshot> materials,
        IEnumerable<RoomMaterialUnitId> completedMaterialUnits,
        IEnumerable<EntityId> activeJobIds,
        long version)
    {
        if (roomInfrastructureId.IsEmpty)
        {
            throw new ArgumentException("Room infrastructure id cannot be empty.", nameof(roomInfrastructureId));
        }

        if (string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));
        }

        if (upgradeOrderCount < 0 || upgradeOrderCount > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(upgradeOrderCount));
        }

        if (!Enum.IsDefined(typeof(RoomTemplateKind), templateKind))
        {
            throw new ArgumentOutOfRangeException(nameof(templateKind));
        }

        if (!Enum.IsDefined(typeof(RoomImprovementStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (!Enum.IsDefined(typeof(RoomPurposeKind), requestedPurpose))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedPurpose));
        }

        if (!Enum.IsDefined(typeof(RoomPurposeKind), activePurpose))
        {
            throw new ArgumentOutOfRangeException(nameof(activePurpose));
        }

        if (materials == null || completedMaterialUnits == null || activeJobIds == null)
        {
            throw new ArgumentNullException(nameof(materials));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        RoomMaterialLedgerSnapshot[] orderedMaterials = materials
            .OrderBy(value => value.ItemId)
            .ToArray();
        RoomMaterialUnitId[] orderedUnits = completedMaterialUnits
            .OrderBy(value => value)
            .ToArray();
        EntityId[] orderedJobs = activeJobIds
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (orderedMaterials.Select(value => value.ItemId).Distinct().Count()
                != orderedMaterials.Length
            || orderedUnits.Distinct().Count() != orderedUnits.Length
            || orderedJobs.Any(value => value.IsEmpty)
            || orderedJobs.Distinct().Count() != orderedJobs.Length)
        {
            throw new ArgumentException("Room infrastructure snapshot collections must be unique.");
        }

        RoomInfrastructureId = roomInfrastructureId;
        TemplateInstanceId = templateInstanceId.Trim();
        TemplateKind = templateKind;
        UpgradeOrderCount = upgradeOrderCount;
        Status = status;
        CancellationLocked = cancellationLocked;
        RequestedPurpose = requestedPurpose;
        ActivePurpose = activePurpose;
        TemporaryStockCell = temporaryStockCell;
        Materials = new ReadOnlyCollection<RoomMaterialLedgerSnapshot>(orderedMaterials);
        CompletedMaterialUnits = new ReadOnlyCollection<RoomMaterialUnitId>(orderedUnits);
        ActiveJobIds = new ReadOnlyCollection<EntityId>(orderedJobs);
        Version = version;
    }

    public EntityId RoomInfrastructureId { get; }

    public string TemplateInstanceId { get; }

    public RoomTemplateKind TemplateKind { get; }

    public int UpgradeOrderCount { get; }

    public RoomImprovementStatus Status { get; }

    public bool CancellationLocked { get; }

    public RoomPurposeKind RequestedPurpose { get; }

    public RoomPurposeKind ActivePurpose { get; }

    public CellId? TemporaryStockCell { get; }

    public IReadOnlyList<RoomMaterialLedgerSnapshot> Materials { get; }

    public IReadOnlyList<RoomMaterialUnitId> CompletedMaterialUnits { get; }

    public IReadOnlyList<EntityId> ActiveJobIds { get; }

    public long Version { get; }
}

public sealed class RoomInfrastructureSnapshot
{
    public RoomInfrastructureSnapshot(
        long version,
        IEnumerable<RoomInfrastructureProjectSnapshot> rooms)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (rooms == null)
        {
            throw new ArgumentNullException(nameof(rooms));
        }

        RoomInfrastructureProjectSnapshot[] ordered = rooms
            .OrderBy(value => value.RoomInfrastructureId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (ordered.Select(value => value.RoomInfrastructureId).Distinct().Count()
                != ordered.Length
            || ordered.Select(value => value.TemplateInstanceId)
                .Distinct(StringComparer.Ordinal).Count() != ordered.Length)
        {
            throw new ArgumentException("Room infrastructure identities must be unique.", nameof(rooms));
        }

        Version = version;
        Rooms = new ReadOnlyCollection<RoomInfrastructureProjectSnapshot>(ordered);
    }

    public long Version { get; }

    public IReadOnlyList<RoomInfrastructureProjectSnapshot> Rooms { get; }
}

public sealed class RoomUpgradeCancellationResult
{
    public RoomUpgradeCancellationResult(
        IEnumerable<RoomMaterialLedgerSnapshot> releasedMaterials,
        IEnumerable<EntityId> activeJobIds)
    {
        ReleasedMaterials = new ReadOnlyCollection<RoomMaterialLedgerSnapshot>(
            (releasedMaterials ?? throw new ArgumentNullException(nameof(releasedMaterials)))
                .OrderBy(value => value.ItemId)
                .ToArray());
        ActiveJobIds = new ReadOnlyCollection<EntityId>(
            (activeJobIds ?? throw new ArgumentNullException(nameof(activeJobIds)))
                .OrderBy(value => value.ToString(), StringComparer.Ordinal)
                .ToArray());
    }

    public IReadOnlyList<RoomMaterialLedgerSnapshot> ReleasedMaterials { get; }

    public IReadOnlyList<EntityId> ActiveJobIds { get; }
}

}
