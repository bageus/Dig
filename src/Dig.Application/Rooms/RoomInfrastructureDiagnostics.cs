using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public enum RoomInfrastructureBlockReason
{
    None = 0,
    TemporaryStockCellUnavailable = 1,
    MaterialsIncomplete = 2,
    WaitingForWorker = 3,
}

public sealed class RoomMaterialDiagnostic
{
    public RoomMaterialDiagnostic(
        ItemId itemId,
        int required,
        int delivered,
        int consumed,
        int releasedOnCancel)
    {
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
}

public sealed class RoomInfrastructureDiagnostic
{
    public RoomInfrastructureDiagnostic(
        EntityId roomInfrastructureId,
        string templateInstanceId,
        RoomTemplateKind templateKind,
        int upgradeOrderCount,
        RoomImprovementStatus status,
        RoomPurposeKind requestedPurpose,
        RoomPurposeKind activePurpose,
        CellId? temporaryStockCell,
        RoomInfrastructureBlockReason blockReason,
        bool cancellationAllowed,
        IEnumerable<RoomMaterialDiagnostic> materials,
        int activeJobCount,
        long version)
    {
        RoomInfrastructureId = roomInfrastructureId;
        TemplateInstanceId = templateInstanceId;
        TemplateKind = templateKind;
        UpgradeOrderCount = upgradeOrderCount;
        Status = status;
        RequestedPurpose = requestedPurpose;
        ActivePurpose = activePurpose;
        TemporaryStockCell = temporaryStockCell;
        BlockReason = blockReason;
        CancellationAllowed = cancellationAllowed;
        Materials = new ReadOnlyCollection<RoomMaterialDiagnostic>(
            materials.OrderBy(value => value.ItemId).ToArray());
        ActiveJobCount = activeJobCount;
        Version = version;
    }

    public EntityId RoomInfrastructureId { get; }
    public string TemplateInstanceId { get; }
    public RoomTemplateKind TemplateKind { get; }
    public int UpgradeOrderCount { get; }
    public RoomImprovementStatus Status { get; }
    public RoomPurposeKind RequestedPurpose { get; }
    public RoomPurposeKind ActivePurpose { get; }
    public CellId? TemporaryStockCell { get; }
    public RoomInfrastructureBlockReason BlockReason { get; }
    public bool CancellationAllowed { get; }
    public IReadOnlyList<RoomMaterialDiagnostic> Materials { get; }
    public int ActiveJobCount { get; }
    public long Version { get; }
}

public sealed class RoomInfrastructureDiagnosticsProjector
{
    public IReadOnlyList<RoomInfrastructureDiagnostic> Project(
        RoomInfrastructureSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new ReadOnlyCollection<RoomInfrastructureDiagnostic>(
            snapshot.Rooms.Select(ProjectRoom).ToArray());
    }

    private static RoomInfrastructureDiagnostic ProjectRoom(
        RoomInfrastructureProjectSnapshot room)
    {
        return new RoomInfrastructureDiagnostic(
            room.RoomInfrastructureId,
            room.TemplateInstanceId,
            room.TemplateKind,
            room.UpgradeOrderCount,
            room.Status,
            room.RequestedPurpose,
            room.ActivePurpose,
            room.TemporaryStockCell,
            ResolveBlockReason(room),
            !room.CancellationLocked
                && (room.Status == RoomImprovementStatus.AwaitingMaterials
                    || room.Status == RoomImprovementStatus.ReadyForWork),
            room.Materials.Select(value => new RoomMaterialDiagnostic(
                value.ItemId,
                value.Required,
                value.Delivered,
                value.Consumed,
                value.ReleasedOnCancel)),
            room.ActiveJobIds.Count,
            room.Version);
    }

    private static RoomInfrastructureBlockReason ResolveBlockReason(
        RoomInfrastructureProjectSnapshot room)
    {
        if (room.Status == RoomImprovementStatus.AwaitingMaterials
            && !room.TemporaryStockCell.HasValue)
        {
            return RoomInfrastructureBlockReason.TemporaryStockCellUnavailable;
        }

        if (room.Status == RoomImprovementStatus.AwaitingMaterials)
        {
            return RoomInfrastructureBlockReason.MaterialsIncomplete;
        }

        if ((room.Status == RoomImprovementStatus.ReadyForWork
                || room.Status == RoomImprovementStatus.Improving)
            && room.ActiveJobIds.Count == 0)
        {
            return RoomInfrastructureBlockReason.WaitingForWorker;
        }

        return RoomInfrastructureBlockReason.None;
    }
}

}
