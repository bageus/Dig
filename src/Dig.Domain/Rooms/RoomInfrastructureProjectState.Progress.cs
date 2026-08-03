using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Rooms
{

internal sealed partial class RoomInfrastructureProjectState
{
    public Result RecordDelivery(EntityId jobId, ItemId itemId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (Status != RoomImprovementStatus.AwaitingMaterials
            || !TemporaryStockCell.HasValue)
        {
            return Result.Failure(RoomInfrastructureErrors.InvalidStatus);
        }

        if (!_activeJobIds.Contains(jobId))
        {
            return Result.Failure(RoomInfrastructureErrors.JobNotAttached);
        }

        if (!_materials.TryGetValue(itemId, out RoomMaterialLedgerState? material))
        {
            return Result.Failure(RoomInfrastructureErrors.MaterialNotRequired);
        }

        Result delivered = material.Deliver(quantity);
        if (delivered.IsFailure)
        {
            return delivered;
        }

        _activeJobIds.Remove(jobId);
        if (_materials.Values.All(value => value.Delivered == value.Required))
        {
            Status = RoomImprovementStatus.ReadyForWork;
        }

        IncrementVersion();
        return Result.Success();
    }

    public Result StartImprovementWork(EntityId jobId)
    {
        if (!_activeJobIds.Contains(jobId))
        {
            return Result.Failure(RoomInfrastructureErrors.JobNotAttached);
        }

        if (Status == RoomImprovementStatus.Improving)
        {
            return Result.Success();
        }

        if (Status != RoomImprovementStatus.ReadyForWork)
        {
            return Result.Failure(RoomInfrastructureErrors.MaterialsIncomplete);
        }

        Status = RoomImprovementStatus.Improving;
        CancellationLocked = true;
        IncrementVersion();
        return Result.Success();
    }

    public Result<RoomMaterialCommitResult> CommitMaterialUnit(
        EntityId jobId,
        RoomMaterialUnitId unitId)
    {
        if (_completedMaterialUnits.Contains(unitId))
        {
            return Result<RoomMaterialCommitResult>.Success(
                new RoomMaterialCommitResult(
                    alreadyCommitted: true,
                    improvementCompleted: Status == RoomImprovementStatus.Improved));
        }

        if (Status != RoomImprovementStatus.Improving)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomInfrastructureErrors.InvalidStatus);
        }

        if (!_activeJobIds.Contains(jobId))
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomInfrastructureErrors.JobNotAttached);
        }

        if (!_materials.TryGetValue(unitId.ItemId, out RoomMaterialLedgerState? material))
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomInfrastructureErrors.MaterialNotRequired);
        }

        RoomMaterialUnitId? nextUnit = ResolveNextMaterialUnit();
        if (unitId.Ordinal > material.Required
            || !nextUnit.HasValue
            || nextUnit.Value != unitId)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomInfrastructureErrors.InvalidMaterialUnit);
        }

        Result consumed = material.ConsumeOne();
        if (consumed.IsFailure)
        {
            return Result<RoomMaterialCommitResult>.Failure(consumed.Error!);
        }

        _completedMaterialUnits.Add(unitId);
        bool completed = _materials.Values.All(value => value.Consumed == value.Required);
        if (completed)
        {
            Status = RoomImprovementStatus.Improved;
            ActivePurpose = RequestedPurpose;
            TemporaryStockCell = null;
            _activeJobIds.Clear();
        }

        IncrementVersion();
        return Result<RoomMaterialCommitResult>.Success(
            new RoomMaterialCommitResult(
                alreadyCommitted: false,
                improvementCompleted: completed));
    }

    private RoomMaterialUnitId? ResolveNextMaterialUnit()
    {
        foreach (RoomMaterialRequirement requirement in
            RoomUpgradeCostCatalog.Get(TemplateKind))
        {
            RoomMaterialLedgerState material = _materials[requirement.ItemId];
            if (material.Consumed < material.Required)
            {
                return new RoomMaterialUnitId(
                    requirement.ItemId,
                    material.Consumed + 1);
            }
        }

        return null;
    }

    public Result<RoomUpgradeCancellationResult> CancelBeforeWork(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Room upgrade cancellation reason is required.", nameof(reason));
        }

        if (CancellationLocked
            || (Status != RoomImprovementStatus.AwaitingMaterials
                && Status != RoomImprovementStatus.ReadyForWork))
        {
            return Result<RoomUpgradeCancellationResult>.Failure(
                RoomInfrastructureErrors.CancellationLocked);
        }

        RoomMaterialLedgerSnapshot[] released = _materials.Values
            .Select(value => value.CreateSnapshot(releasedOverride: value.Delivered))
            .ToArray();
        EntityId[] jobs = _activeJobIds.ToArray();
        UpgradeOrderCount = 0;
        Status = RoomImprovementStatus.Unimproved;
        CancellationLocked = false;
        RequestedPurpose = RoomPurposeKind.None;
        ActivePurpose = RoomPurposeKind.None;
        TemporaryStockCell = null;
        _activeJobIds.Clear();
        _completedMaterialUnits.Clear();
        foreach (RoomMaterialLedgerState material in _materials.Values)
        {
            material.ReleaseOnCancel();
        }

        IncrementVersion();
        return Result<RoomUpgradeCancellationResult>.Success(
            new RoomUpgradeCancellationResult(released, jobs));
    }
}

}
