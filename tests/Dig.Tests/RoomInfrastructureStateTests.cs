using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomInfrastructureStateTests
{
    private static readonly EntityId RoomId = Id(1);

    [Theory]
    [InlineData(RoomTemplateKind.Small, 4, 4, 0, 0)]
    [InlineData(RoomTemplateKind.Medium, 8, 8, 0, 0)]
    [InlineData(RoomTemplateKind.Large, 12, 8, 4, 0)]
    [InlineData(RoomTemplateKind.Tall, 10, 6, 4, 4)]
    public void Cost_catalog_matches_confirmed_material_requirements(
        RoomTemplateKind kind,
        int stone,
        int mushroomLeg,
        int iron,
        int crystal)
    {
        var cost = RoomUpgradeCostCatalog.Get(kind)
            .ToDictionary(value => value.ItemId, value => value.Quantity);

        Assert.Equal(stone, cost[RoomUpgradeMaterialIds.Stone]);
        Assert.Equal(mushroomLeg, cost[RoomUpgradeMaterialIds.MushroomLeg]);
        Assert.Equal(iron, cost.TryGetValue(RoomUpgradeMaterialIds.Iron, out int i) ? i : 0);
        Assert.Equal(crystal, cost.TryGetValue(RoomUpgradeMaterialIds.Crystal, out int c) ? c : 0);
    }

    [Fact]
    public void Order_count_is_zero_or_one_and_prework_cancel_releases_delivered_ledger()
    {
        RoomInfrastructureState state = Registered(RoomTemplateKind.Small);
        Assert.True(state.OrderUpgrade(RoomId, RoomPurposeKind.Bedroom, tick: 1).IsSuccess);
        Assert.True(state.OrderUpgrade(RoomId, RoomPurposeKind.Farm, tick: 2).IsFailure);
        Assert.True(state.AssignTemporaryStockCell(RoomId, new CellId(5, 5, 0), tick: 3).IsSuccess);
        Deliver(state, RoomUpgradeMaterialIds.Stone, quantity: 2, jobNumber: 10, tick: 4);
        EntityId pending = Id(20);
        Assert.True(state.AttachJob(RoomId, pending, tick: 5).IsSuccess);

        Result<RoomUpgradeCancellationResult> cancelled =
            state.CancelUpgradeBeforeWork(RoomId, "player cancelled", tick: 6);

        Assert.True(cancelled.IsSuccess);
        Assert.Contains(cancelled.Value.ActiveJobIds, value => value == pending);
        Assert.Equal(2, cancelled.Value.ReleasedMaterials
            .Single(value => value.ItemId == RoomUpgradeMaterialIds.Stone)
            .ReleasedOnCancel);
        RoomInfrastructureProjectSnapshot room = state.Get(RoomId)!;
        Assert.Equal(0, room.UpgradeOrderCount);
        Assert.Equal(RoomImprovementStatus.Unimproved, room.Status);
        Assert.Equal(2, room.Materials
            .Single(value => value.ItemId == RoomUpgradeMaterialIds.Stone)
            .ReleasedOnCancel);
        Assert.Null(room.TemporaryStockCell);
    }

    [Fact]
    public void Work_start_locks_cancellation_and_latest_requested_purpose_activates_on_completion()
    {
        RoomInfrastructureState state = ReadyForWork(RoomTemplateKind.Small);
        EntityId workJob = Id(100);
        Assert.True(state.AttachJob(RoomId, workJob, tick: 20).IsSuccess);
        Assert.True(state.StartImprovementWork(RoomId, workJob, tick: 21).IsSuccess);
        Assert.True(state.ChangeRequestedPurpose(
            RoomId,
            RoomPurposeKind.KitchenDining,
            tick: 22).IsSuccess);
        Assert.True(state.CancelUpgradeBeforeWork(
            RoomId,
            "too late",
            tick: 23).IsFailure);

        RoomMaterialUnitId last = default;
        long tick = 30;
        foreach (RoomMaterialRequirement material in
            RoomUpgradeCostCatalog.Get(RoomTemplateKind.Small))
        {
            for (int ordinal = 1; ordinal <= material.Quantity; ordinal++)
            {
                last = new RoomMaterialUnitId(material.ItemId, ordinal);
                Result<RoomMaterialCommitResult> commit = state.CommitMaterialUnit(
                    RoomId,
                    workJob,
                    last,
                    tick++);
                Assert.True(commit.IsSuccess);
            }
        }

        RoomInfrastructureProjectSnapshot completed = state.Get(RoomId)!;
        Assert.Equal(RoomImprovementStatus.Improved, completed.Status);
        Assert.Equal(RoomPurposeKind.KitchenDining, completed.ActivePurpose);
        Assert.Null(completed.TemporaryStockCell);
        long version = state.Version;
        Result<RoomMaterialCommitResult> replay = state.CommitMaterialUnit(
            RoomId,
            workJob,
            last,
            tick: 100);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.AlreadyCommitted);
        Assert.Equal(version, state.Version);
    }

    [Fact]
    public void Partial_progress_round_trips_and_invalid_awaiting_consumption_is_rejected()
    {
        RoomInfrastructureState state = ReadyForWork(RoomTemplateKind.Small);
        EntityId job = Id(200);
        Assert.True(state.AttachJob(RoomId, job, tick: 20).IsSuccess);
        Assert.True(state.StartImprovementWork(RoomId, job, tick: 21).IsSuccess);
        Assert.True(state.CommitMaterialUnit(
            RoomId,
            job,
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.Stone, 1),
            tick: 22).IsSuccess);

        RoomInfrastructureSnapshot snapshot = state.CaptureSnapshot();
        Result<RoomInfrastructureState> restored = RoomInfrastructureState.Restore(snapshot);

        Assert.True(restored.IsSuccess);
        Assert.Equal(
            snapshot.Rooms[0].CompletedMaterialUnits,
            restored.Value.CaptureSnapshot().Rooms[0].CompletedMaterialUnits);

        RoomInfrastructureProjectSnapshot source = snapshot.Rooms[0];
        RoomInfrastructureProjectSnapshot invalidRoom = new RoomInfrastructureProjectSnapshot(
            source.RoomInfrastructureId,
            source.TemplateInstanceId,
            source.TemplateKind,
            upgradeOrderCount: 1,
            RoomImprovementStatus.AwaitingMaterials,
            cancellationLocked: false,
            RoomPurposeKind.None,
            RoomPurposeKind.None,
            source.TemporaryStockCell,
            source.Materials,
            source.CompletedMaterialUnits,
            Array.Empty<EntityId>(),
            source.Version);
        Assert.True(RoomInfrastructureState.Restore(
            new RoomInfrastructureSnapshot(snapshot.Version, new[] { invalidRoom }))
            .IsFailure);
    }

    private static RoomInfrastructureState Registered(RoomTemplateKind kind)
    {
        RoomInfrastructureState state = new RoomInfrastructureState();
        Assert.True(state.RegisterCompletedTemplateRoom(
            RoomId,
            "room.template.1",
            kind,
            tick: 0).IsSuccess);
        return state;
    }

    private static RoomInfrastructureState ReadyForWork(RoomTemplateKind kind)
    {
        RoomInfrastructureState state = Registered(kind);
        Assert.True(state.OrderUpgrade(RoomId, RoomPurposeKind.Bedroom, tick: 1).IsSuccess);
        Assert.True(state.AssignTemporaryStockCell(RoomId, new CellId(5, 5, 0), tick: 2).IsSuccess);
        int job = 10;
        long tick = 3;
        foreach (RoomMaterialRequirement material in RoomUpgradeCostCatalog.Get(kind))
        {
            Deliver(state, material.ItemId, material.Quantity, job++, tick++);
        }

        Assert.Equal(RoomImprovementStatus.ReadyForWork, state.Get(RoomId)!.Status);
        return state;
    }

    private static void Deliver(
        RoomInfrastructureState state,
        ItemId itemId,
        int quantity,
        int jobNumber,
        long tick)
    {
        EntityId job = Id(jobNumber);
        Assert.True(state.AttachJob(RoomId, job, tick).IsSuccess);
        Assert.True(state.RecordDelivery(RoomId, job, itemId, quantity, tick + 1).IsSuccess);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
