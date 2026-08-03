using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Rooms
{

internal sealed partial class RoomInfrastructureProjectState
{
    public static Result<RoomInfrastructureProjectState> Restore(
        RoomInfrastructureProjectSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        RoomInfrastructureProjectState state = new RoomInfrastructureProjectState(
            snapshot.RoomInfrastructureId,
            snapshot.TemplateInstanceId,
            snapshot.TemplateKind);
        if (!state.RestoreCore(snapshot))
        {
            return Result<RoomInfrastructureProjectState>.Failure(
                RoomInfrastructureErrors.InvalidSnapshot);
        }

        return Result<RoomInfrastructureProjectState>.Success(state);
    }

    private bool RestoreCore(RoomInfrastructureProjectSnapshot snapshot)
    {
        RoomMaterialRequirement[] expected = RoomUpgradeCostCatalog.Get(TemplateKind).ToArray();
        if (snapshot.Materials.Count != expected.Length
            || snapshot.Materials.Any(saved => !expected.Any(required =>
                required.ItemId == saved.ItemId && required.Quantity == saved.Required))
            || snapshot.CompletedMaterialUnits.Any(unit =>
                !_materials.TryGetValue(unit.ItemId, out RoomMaterialLedgerState? material)
                || unit.Ordinal > material.Required)
            || !ValidateLifecycle(snapshot))
        {
            return false;
        }

        UpgradeOrderCount = snapshot.UpgradeOrderCount;
        Status = snapshot.Status;
        CancellationLocked = snapshot.CancellationLocked;
        RequestedPurpose = snapshot.RequestedPurpose;
        ActivePurpose = snapshot.ActivePurpose;
        TemporaryStockCell = snapshot.TemporaryStockCell;
        Version = snapshot.Version;
        foreach (RoomMaterialLedgerSnapshot saved in snapshot.Materials)
        {
            _materials[saved.ItemId].Restore(saved);
        }

        _completedMaterialUnits.UnionWith(snapshot.CompletedMaterialUnits);
        _activeJobIds.UnionWith(snapshot.ActiveJobIds);
        return _materials.All(pair =>
            _completedMaterialUnits.Count(unit => unit.ItemId == pair.Key)
                == pair.Value.Consumed);
    }

    private static bool ValidateLifecycle(RoomInfrastructureProjectSnapshot snapshot)
    {
        bool allDelivered = snapshot.Materials.All(value => value.Delivered == value.Required);
        bool allConsumed = snapshot.Materials.All(value => value.Consumed == value.Required);
        bool noConsumed = snapshot.Materials.All(value => value.Consumed == 0);
        bool noProgress = snapshot.Materials.All(value => value.Delivered == 0 && value.Consumed == 0);
        bool hasDelivery = snapshot.Materials.Any(value => value.Delivered > 0);
        bool noReleased = snapshot.Materials.All(value => value.ReleasedOnCancel == 0);
        return snapshot.Status switch
        {
            RoomImprovementStatus.Unimproved => snapshot.UpgradeOrderCount == 0
                && !snapshot.CancellationLocked
                && !snapshot.TemporaryStockCell.HasValue
                && snapshot.RequestedPurpose == RoomPurposeKind.None
                && snapshot.ActivePurpose == RoomPurposeKind.None
                && noProgress
                && snapshot.CompletedMaterialUnits.Count == 0
                && snapshot.ActiveJobIds.Count == 0,
            RoomImprovementStatus.AwaitingMaterials => snapshot.UpgradeOrderCount == 1
                && !snapshot.CancellationLocked
                && noReleased
                && !allDelivered
                && noConsumed
                && (!hasDelivery || snapshot.TemporaryStockCell.HasValue)
                && snapshot.ActivePurpose == RoomPurposeKind.None
                && snapshot.CompletedMaterialUnits.Count == 0,
            RoomImprovementStatus.ReadyForWork => snapshot.UpgradeOrderCount == 1
                && !snapshot.CancellationLocked
                && noReleased
                && snapshot.TemporaryStockCell.HasValue
                && allDelivered
                && noConsumed
                && snapshot.ActivePurpose == RoomPurposeKind.None
                && snapshot.CompletedMaterialUnits.Count == 0,
            RoomImprovementStatus.Improving => snapshot.UpgradeOrderCount == 1
                && snapshot.CancellationLocked
                && noReleased
                && snapshot.TemporaryStockCell.HasValue
                && allDelivered
                && !allConsumed
                && snapshot.ActivePurpose == RoomPurposeKind.None,
            RoomImprovementStatus.Improved => snapshot.UpgradeOrderCount == 1
                && snapshot.CancellationLocked
                && noReleased
                && !snapshot.TemporaryStockCell.HasValue
                && allConsumed
                && snapshot.ActivePurpose == snapshot.RequestedPurpose
                && snapshot.ActiveJobIds.Count == 0,
            _ => false,
        };
    }

    private sealed class RoomMaterialLedgerState
    {
        public RoomMaterialLedgerState(ItemId itemId, int required)
        {
            ItemId = itemId;
            Required = required;
        }

        public ItemId ItemId { get; }
        public int Required { get; }
        public int Delivered { get; private set; }
        public int Consumed { get; private set; }
        public int ReleasedOnCancel { get; private set; }

        public Result Deliver(int quantity)
        {
            if (checked(Delivered + quantity) > Required)
            {
                return Result.Failure(RoomInfrastructureErrors.DeliveryExceedsRequirement);
            }

            Delivered += quantity;
            return Result.Success();
        }

        public Result ConsumeOne()
        {
            if (Consumed >= Delivered)
            {
                return Result.Failure(RoomInfrastructureErrors.DeliveredMaterialUnavailable);
            }

            Consumed++;
            return Result.Success();
        }

        public void ResetForOrder()
        {
            Delivered = 0;
            Consumed = 0;
            ReleasedOnCancel = 0;
        }

        public void ReleaseOnCancel()
        {
            ReleasedOnCancel = Delivered;
            Delivered = 0;
            Consumed = 0;
        }

        public void Restore(RoomMaterialLedgerSnapshot snapshot)
        {
            Delivered = snapshot.Delivered;
            Consumed = snapshot.Consumed;
            ReleasedOnCancel = snapshot.ReleasedOnCancel;
        }

        public RoomMaterialLedgerSnapshot CreateSnapshot(int? releasedOverride = null)
        {
            return new RoomMaterialLedgerSnapshot(
                ItemId,
                Required,
                Delivered,
                Consumed,
                releasedOverride ?? ReleasedOnCancel);
        }
    }
}

}
