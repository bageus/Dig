using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Rooms;

namespace Dig.Presentation.Rooms
{

public sealed class RoomMaterialProgressViewModel
{
    public RoomMaterialProgressViewModel(
        string itemId,
        int required,
        int delivered,
        int consumed)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Room material item id is required.", nameof(itemId));
        }

        if (required <= 0
            || delivered < 0
            || consumed < 0
            || consumed > delivered
            || delivered > required)
        {
            throw new ArgumentOutOfRangeException(nameof(required));
        }

        ItemId = itemId.Trim();
        Required = required;
        Delivered = delivered;
        Consumed = consumed;
    }

    public string ItemId { get; }
    public int Required { get; }
    public int Delivered { get; }
    public int Consumed { get; }
    public int RemainingDelivery => Required - Delivered;
    public int RemainingWork => Required - Consumed;
}

public sealed class RoomMaterialUnitProgressViewModel
{
    public RoomMaterialUnitProgressViewModel(string itemId, int ordinal)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Room material unit item id is required.", nameof(itemId));
        }

        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        ItemId = itemId.Trim();
        Ordinal = ordinal;
    }

    public string ItemId { get; }
    public int Ordinal { get; }
    public string StableId => $"{ItemId}:{Ordinal}";
}

public sealed class RoomInfrastructureViewModel
{
    public RoomInfrastructureViewModel(
        string id,
        string templateInstanceId,
        RoomTemplateKind templateKind,
        int upgradeOrderCount,
        RoomImprovementStatus status,
        RoomPurposeKind requestedPurpose,
        RoomPurposeKind activePurpose,
        bool cancellationAllowed,
        RoomInfrastructureBlockReason blockReason,
        float markerX,
        int markerY,
        int markerZ,
        int minX,
        int maxX,
        int minY,
        int maxY,
        IEnumerable<RoomMaterialProgressViewModel> materials,
        IEnumerable<RoomMaterialUnitProgressViewModel> completedUnits,
        long version)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Room presentation identity is required.");
        }

        if (upgradeOrderCount < 0 || upgradeOrderCount > 1 || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(upgradeOrderCount));
        }

        if (minX > maxX || minY > maxY)
        {
            throw new ArgumentOutOfRangeException(nameof(minX));
        }

        RoomMaterialProgressViewModel[] orderedMaterials = (materials
            ?? throw new ArgumentNullException(nameof(materials)))
            .OrderBy(value => value.ItemId, StringComparer.Ordinal)
            .ToArray();
        RoomMaterialUnitProgressViewModel[] orderedUnits = (completedUnits
            ?? throw new ArgumentNullException(nameof(completedUnits)))
            .OrderBy(value => value.ItemId, StringComparer.Ordinal)
            .ThenBy(value => value.Ordinal)
            .ToArray();
        if (orderedMaterials.Select(value => value.ItemId)
                .Distinct(StringComparer.Ordinal).Count() != orderedMaterials.Length
            || orderedUnits.Select(value => value.StableId)
                .Distinct(StringComparer.Ordinal).Count() != orderedUnits.Length)
        {
            throw new ArgumentException("Room presentation collections must be unique.");
        }

        Id = id.Trim();
        TemplateInstanceId = templateInstanceId.Trim();
        TemplateKind = templateKind;
        UpgradeOrderCount = upgradeOrderCount;
        Status = status;
        RequestedPurpose = requestedPurpose;
        ActivePurpose = activePurpose;
        CancellationAllowed = cancellationAllowed;
        BlockReason = blockReason;
        MarkerX = markerX;
        MarkerY = markerY;
        MarkerZ = markerZ;
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
        Materials = new ReadOnlyCollection<RoomMaterialProgressViewModel>(
            orderedMaterials);
        CompletedUnits = new ReadOnlyCollection<RoomMaterialUnitProgressViewModel>(
            orderedUnits);
        Version = version;
    }

    public string Id { get; }
    public string TemplateInstanceId { get; }
    public RoomTemplateKind TemplateKind { get; }
    public int UpgradeOrderCount { get; }
    public RoomImprovementStatus Status { get; }
    public RoomPurposeKind RequestedPurpose { get; }
    public RoomPurposeKind ActivePurpose { get; }
    public bool CancellationAllowed { get; }
    public RoomInfrastructureBlockReason BlockReason { get; }
    public float MarkerX { get; }
    public int MarkerY { get; }
    public int MarkerZ { get; }
    public int MinX { get; }
    public int MaxX { get; }
    public int MinY { get; }
    public int MaxY { get; }
    public IReadOnlyList<RoomMaterialProgressViewModel> Materials { get; }
    public IReadOnlyList<RoomMaterialUnitProgressViewModel> CompletedUnits { get; }
    public long Version { get; }

    public int RequiredUnits => Materials.Sum(value => value.Required);
    public int DeliveredUnits => Materials.Sum(value => value.Delivered);
    public int ConsumedUnits => Materials.Sum(value => value.Consumed);
    public int DeliveryProgressBasisPoints => RequiredUnits == 0
        ? 0
        : (DeliveredUnits * 10000) / RequiredUnits;
    public int WorkProgressBasisPoints => RequiredUnits == 0
        ? 0
        : (ConsumedUnits * 10000) / RequiredUnits;
    public bool CanOrderUpgrade => UpgradeOrderCount == 0
        && Status == RoomImprovementStatus.Unimproved;
    public bool CanChangePurpose => UpgradeOrderCount == 1
        && Status != RoomImprovementStatus.Unimproved;
}

}
