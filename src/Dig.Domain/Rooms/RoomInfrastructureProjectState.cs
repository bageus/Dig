using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Rooms
{

internal sealed partial class RoomInfrastructureProjectState
{
    private readonly Dictionary<ItemId, RoomMaterialLedgerState> _materials;
    private readonly HashSet<RoomMaterialUnitId> _completedMaterialUnits =
        new HashSet<RoomMaterialUnitId>();
    private readonly HashSet<EntityId> _activeJobIds = new HashSet<EntityId>();

    public RoomInfrastructureProjectState(
        EntityId roomInfrastructureId,
        string templateInstanceId,
        RoomTemplateKind templateKind)
    {
        if (roomInfrastructureId.IsEmpty)
        {
            throw new ArgumentException("Room infrastructure id cannot be empty.", nameof(roomInfrastructureId));
        }

        if (string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));
        }

        if (!Enum.IsDefined(typeof(RoomTemplateKind), templateKind))
        {
            throw new ArgumentOutOfRangeException(nameof(templateKind));
        }

        RoomInfrastructureId = roomInfrastructureId;
        TemplateInstanceId = templateInstanceId.Trim();
        TemplateKind = templateKind;
        Status = RoomImprovementStatus.Unimproved;
        RequestedPurpose = RoomPurposeKind.None;
        ActivePurpose = RoomPurposeKind.None;
        _materials = RoomUpgradeCostCatalog.Get(templateKind)
            .ToDictionary(
                value => value.ItemId,
                value => new RoomMaterialLedgerState(value.ItemId, value.Quantity));
    }

    public EntityId RoomInfrastructureId { get; }

    public string TemplateInstanceId { get; }

    public RoomTemplateKind TemplateKind { get; }

    public int UpgradeOrderCount { get; private set; }

    public RoomImprovementStatus Status { get; private set; }

    public bool CancellationLocked { get; private set; }

    public RoomPurposeKind RequestedPurpose { get; private set; }

    public RoomPurposeKind ActivePurpose { get; private set; }

    public CellId? TemporaryStockCell { get; private set; }

    public long Version { get; private set; }

    public Result Order(RoomPurposeKind requestedPurpose)
    {
        ValidatePurpose(requestedPurpose);
        if (Status != RoomImprovementStatus.Unimproved || UpgradeOrderCount != 0)
        {
            return Result.Failure(RoomInfrastructureErrors.UpgradeAlreadyOrdered);
        }

        UpgradeOrderCount = 1;
        Status = RoomImprovementStatus.AwaitingMaterials;
        CancellationLocked = false;
        RequestedPurpose = requestedPurpose;
        ActivePurpose = RoomPurposeKind.None;
        TemporaryStockCell = null;
        _completedMaterialUnits.Clear();
        _activeJobIds.Clear();
        foreach (RoomMaterialLedgerState material in _materials.Values)
        {
            material.ResetForOrder();
        }

        IncrementVersion();
        return Result.Success();
    }

    public Result AssignTemporaryStockCell(CellId cell)
    {
        if (UpgradeOrderCount != 1
            || Status == RoomImprovementStatus.Unimproved
            || Status == RoomImprovementStatus.Improved)
        {
            return Result.Failure(RoomInfrastructureErrors.InvalidStatus);
        }

        if (TemporaryStockCell.HasValue)
        {
            return TemporaryStockCell.Value == cell
                ? Result.Success()
                : Result.Failure(RoomInfrastructureErrors.StockCellAlreadyAssigned);
        }

        TemporaryStockCell = cell;
        IncrementVersion();
        return Result.Success();
    }

    public Result ChangeRequestedPurpose(RoomPurposeKind purpose)
    {
        ValidatePurpose(purpose);
        if (UpgradeOrderCount != 1 || Status == RoomImprovementStatus.Unimproved)
        {
            return Result.Failure(RoomInfrastructureErrors.InvalidStatus);
        }

        if (RequestedPurpose == purpose
            && (Status != RoomImprovementStatus.Improved || ActivePurpose == purpose))
        {
            return Result.Success();
        }

        RequestedPurpose = purpose;
        if (Status == RoomImprovementStatus.Improved)
        {
            ActivePurpose = purpose;
        }

        IncrementVersion();
        return Result.Success();
    }

    public Result AttachJob(EntityId jobId)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Room job id cannot be empty.", nameof(jobId));
        }

        if (UpgradeOrderCount != 1
            || Status == RoomImprovementStatus.Unimproved
            || Status == RoomImprovementStatus.Improved)
        {
            return Result.Failure(RoomInfrastructureErrors.InvalidStatus);
        }

        if (_activeJobIds.Add(jobId))
        {
            IncrementVersion();
        }

        return Result.Success();
    }

    public Result DetachJob(EntityId jobId)
    {
        if (!_activeJobIds.Remove(jobId))
        {
            return Result.Failure(RoomInfrastructureErrors.JobNotAttached);
        }

        IncrementVersion();
        return Result.Success();
    }

    public RoomInfrastructureProjectSnapshot CreateSnapshot()
    {
        return new RoomInfrastructureProjectSnapshot(
            RoomInfrastructureId,
            TemplateInstanceId,
            TemplateKind,
            UpgradeOrderCount,
            Status,
            CancellationLocked,
            RequestedPurpose,
            ActivePurpose,
            TemporaryStockCell,
            _materials.Values.Select(value => value.CreateSnapshot()),
            _completedMaterialUnits,
            _activeJobIds,
            Version);
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }

    private static void ValidatePurpose(RoomPurposeKind purpose)
    {
        if (!Enum.IsDefined(typeof(RoomPurposeKind), purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }
    }
}

}
