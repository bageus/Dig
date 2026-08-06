using System;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static partial class RoomInfrastructureSaveAdapter
{
    private static RoomInfrastructureProjectSaveData EncodeProject(
        RoomInfrastructureProjectSnapshot room)
    {
        RoomInfrastructureProjectSaveData data =
            new RoomInfrastructureProjectSaveData
            {
                RoomInfrastructureId = room.RoomInfrastructureId.ToString(),
                TemplateInstanceId = room.TemplateInstanceId,
                TemplateKind = (int)room.TemplateKind,
                UpgradeOrderCount = room.UpgradeOrderCount,
                Status = (int)room.Status,
                CancellationLocked = room.CancellationLocked,
                RequestedPurpose = (int)room.RequestedPurpose,
                ActivePurpose = (int)room.ActivePurpose,
                TemporaryStockCell = room.TemporaryStockCell.HasValue
                    ? EncodeCell(room.TemporaryStockCell.Value)
                    : null,
                Version = room.Version,
            };
        foreach (RoomMaterialLedgerSnapshot material in room.Materials)
        {
            data.Materials.Add(new RoomMaterialLedgerSaveData
            {
                ItemId = material.ItemId.ToString(),
                Required = material.Required,
                Delivered = material.Delivered,
                Consumed = material.Consumed,
                ReleasedOnCancel = material.ReleasedOnCancel,
            });
        }

        foreach (RoomMaterialUnitId unit in room.CompletedMaterialUnits)
        {
            data.CompletedMaterialUnits.Add(new RoomMaterialUnitSaveData
            {
                ItemId = unit.ItemId.ToString(),
                Ordinal = unit.Ordinal,
            });
        }

        foreach (EntityId jobId in room.ActiveJobIds)
        {
            data.ActiveJobIds.Add(jobId.ToString());
        }

        return data;
    }

    private static RoomInfrastructureProjectSnapshot DecodeProject(
        RoomInfrastructureProjectSaveData data)
    {
        if (data == null
            || data.Materials == null
            || data.CompletedMaterialUnits == null
            || data.ActiveJobIds == null
            || data.Materials.Any(value => value == null)
            || data.CompletedMaterialUnits.Any(value => value == null)
            || data.ActiveJobIds.Any(string.IsNullOrWhiteSpace)
            || !Enum.IsDefined(typeof(RoomTemplateKind), data.TemplateKind)
            || !Enum.IsDefined(typeof(RoomImprovementStatus), data.Status)
            || !Enum.IsDefined(typeof(RoomPurposeKind), data.RequestedPurpose)
            || !Enum.IsDefined(typeof(RoomPurposeKind), data.ActivePurpose))
        {
            throw new InvalidOperationException(
                "Invalid room infrastructure project save data.");
        }

        return new RoomInfrastructureProjectSnapshot(
            EntityId.Parse(data.RoomInfrastructureId),
            data.TemplateInstanceId,
            (RoomTemplateKind)data.TemplateKind,
            data.UpgradeOrderCount,
            (RoomImprovementStatus)data.Status,
            data.CancellationLocked,
            (RoomPurposeKind)data.RequestedPurpose,
            (RoomPurposeKind)data.ActivePurpose,
            data.TemporaryStockCell == null
                ? (CellId?)null
                : DecodeCell(data.TemporaryStockCell),
            data.Materials.Select(DecodeMaterial),
            data.CompletedMaterialUnits.Select(DecodeUnit),
            data.ActiveJobIds.Select(EntityId.Parse),
            data.Version);
    }

    private static RoomMaterialLedgerSnapshot DecodeMaterial(
        RoomMaterialLedgerSaveData data)
    {
        return new RoomMaterialLedgerSnapshot(
            new ItemId(data.ItemId),
            data.Required,
            data.Delivered,
            data.Consumed,
            data.ReleasedOnCancel);
    }

    private static RoomMaterialUnitId DecodeUnit(RoomMaterialUnitSaveData data)
    {
        return new RoomMaterialUnitId(
            new ItemId(data.ItemId),
            data.Ordinal);
    }

    private static RoomInfrastructureProvenanceSaveData EncodeProvenance(
        CompletedRoomInfrastructureProvenance provenance)
    {
        RoomInfrastructureProvenanceSaveData data =
            new RoomInfrastructureProvenanceSaveData
            {
                RoomInfrastructureId = provenance.RoomInfrastructureId.ToString(),
                TemplateInstanceId = provenance.TemplateInstanceId,
                TemplateKind = (int)provenance.TemplateKind,
            };
        foreach (CellId cell in provenance.OrderedRoomCells)
        {
            data.OrderedRoomCells.Add(EncodeCell(cell));
        }

        return data;
    }

    private static CompletedRoomInfrastructureProvenance DecodeProvenance(
        RoomInfrastructureProvenanceSaveData data)
    {
        if (data == null
            || data.OrderedRoomCells == null
            || data.OrderedRoomCells.Any(value => value == null)
            || !Enum.IsDefined(typeof(RoomTemplateKind), data.TemplateKind))
        {
            throw new InvalidOperationException(
                "Invalid room infrastructure provenance save data.");
        }

        return new CompletedRoomInfrastructureProvenance(
            EntityId.Parse(data.RoomInfrastructureId),
            data.TemplateInstanceId,
            (RoomTemplateKind)data.TemplateKind,
            data.OrderedRoomCells.Select(DecodeCell));
    }

    private static RoomCellSaveData EncodeCell(CellId cell)
    {
        return new RoomCellSaveData { X = cell.X, Y = cell.Y, Z = cell.Z };
    }

    private static CellId DecodeCell(RoomCellSaveData data)
    {
        return new CellId(data.X, data.Y, data.Z);
    }
}

}
