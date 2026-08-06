using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Xunit;
using static Dig.Tests.RoomUpgradeExecutionTestFixture;

namespace Dig.Tests
{

public sealed class RoomUpgradeExecutionWorkflowTests
{
    [Fact]
    public void Delivery_work_interruption_and_replay_complete_exactly_once()
    {
        RoomUpgradeExecutionHarness harness = CreateHarness();
        RoomUpgradeJobSynchronizationReport report = Synchronize(harness);
        Assert.Single(report.WorkJobsCreated);
        Assert.Equal(2, report.DeliveriesCreated.Count);
        EntityId workJobId = report.WorkJobsCreated[0];
        Assert.Equal(JobStatus.Created, harness.Jobs.Get().Get(workJobId)!.Status);

        CompleteDeliveries(harness, report);

        RoomInfrastructureProjectSnapshot ready = harness.Rooms.Get().Get(RoomId)!;
        Assert.Equal(RoomImprovementStatus.ReadyForWork, ready.Status);
        Assert.Equal(JobStatus.Available, harness.Jobs.Get().Get(workJobId)!.Status);
        Assert.Equal(4, Reserved(harness, workJobId, RoomUpgradeMaterialIds.Stone));
        Assert.Equal(4, Reserved(
            harness,
            workJobId,
            RoomUpgradeMaterialIds.MushroomLeg));

        BeginWork(harness, workJobId, FirstWorker, tick: 20);
        int stoneBefore = Skill(harness, FirstWorker, AgentSkillCatalog.Stonework);
        RoomMaterialUnitId firstStone = new RoomMaterialUnitId(
            RoomUpgradeMaterialIds.Stone,
            ordinal: 1);
        Result<RoomMaterialCommitResult> first = harness.WorkIntervals.Handle(
            new CommitRoomUpgradeWorkIntervalCommand(workJobId, firstStone, tick: 21));
        Result<RoomMaterialCommitResult> replay = harness.WorkIntervals.Handle(
            new CommitRoomUpgradeWorkIntervalCommand(workJobId, firstStone, tick: 22));

        Assert.True(first.IsSuccess);
        Assert.False(first.Value.AlreadyCommitted);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.AlreadyCommitted);
        Assert.Equal(
            stoneBefore + CommitRoomUpgradeWorkIntervalHandler.SkillGrantUnits,
            Skill(harness, FirstWorker, AgentSkillCatalog.Stonework));
        Assert.True(harness.Cancel.Handle(new CancelRoomUpgradeOperationCommand(
            RoomId,
            "too_late",
            tick: 23)).IsFailure);

        JobSystem jobs = harness.Jobs.Get();
        Assert.True(jobs.ReleaseAssignment(workJobId, tick: 24).IsSuccess);
        Assert.Equal(JobStatus.Available, jobs.Get(workJobId)!.Status);
        BeginWork(harness, workJobId, SecondWorker, tick: 25);

        RoomMaterialUnitId[] remaining =
        {
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.Stone, 2),
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.Stone, 3),
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.Stone, 4),
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.MushroomLeg, 1),
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.MushroomLeg, 2),
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.MushroomLeg, 3),
            new RoomMaterialUnitId(RoomUpgradeMaterialIds.MushroomLeg, 4),
        };
        for (int index = 0; index < remaining.Length; index++)
        {
            Result<RoomMaterialCommitResult> committed = harness.WorkIntervals.Handle(
                new CommitRoomUpgradeWorkIntervalCommand(
                    workJobId,
                    remaining[index],
                    tick: 26 + index));
            Assert.True(committed.IsSuccess, committed.Error?.ToString());
        }

        RoomInfrastructureProjectSnapshot completed = harness.Rooms.Get().Get(RoomId)!;
        Assert.Equal(RoomImprovementStatus.Improved, completed.Status);
        Assert.Equal(RoomPurposeKind.Workshop, completed.ActivePurpose);
        Assert.Null(completed.TemporaryStockCell);
        Assert.Empty(completed.ActiveJobIds);
        Assert.Equal(JobStageKind.Finalize, harness.Jobs.Get().Get(workJobId)!.Stage);
        Assert.Equal(0, harness.Inventory.Get().GetTotalQuantityAt(
            ItemLocation.InWorld(StockCell)));
        Assert.True(harness.CompleteWork.Handle(
            new CompleteRoomUpgradeWorkCommand(workJobId, tick: 40)).IsSuccess);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get().Get(workJobId)!.Status);
    }

    [Fact]
    public void Prework_cancel_releases_room_stock_and_all_job_reservations()
    {
        RoomUpgradeExecutionHarness harness = CreateHarness();
        RoomUpgradeJobSynchronizationReport report = Synchronize(harness);
        RoomUpgradeDeliveryJobPlan stone = report.DeliveriesCreated.Single(
            value => harness.Inventory.Get().GetStack(value.SourceStackId)!.ItemId
                == RoomUpgradeMaterialIds.Stone);
        CompleteDelivery(harness, stone, tick: 10);

        Result<RoomUpgradeCancellationResult> cancelled = harness.Cancel.Handle(
            new CancelRoomUpgradeOperationCommand(RoomId, "player_cancel", tick: 11));

        Assert.True(cancelled.IsSuccess);
        RoomInfrastructureProjectSnapshot room = harness.Rooms.Get().Get(RoomId)!;
        Assert.Equal(RoomImprovementStatus.Unimproved, room.Status);
        Assert.Equal(0, room.UpgradeOrderCount);
        Assert.Null(room.TemporaryStockCell);
        Assert.All(cancelled.Value.ActiveJobIds, jobId =>
            Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get().Get(jobId)!.Status));
        Assert.Equal(4, harness.Inventory.Get().GetAvailableQuantityAt(
            RoomUpgradeMaterialIds.Stone,
            ItemLocation.InWorld(StockCell)));
        Assert.Equal(0, harness.Inventory.Get().CreateSnapshot().Stacks
            .Sum(stack => stack.Reservations.Sum(value => value.Quantity)));
    }

    [Fact]
    public void Material_units_must_commit_in_catalog_order()
    {
        RoomUpgradeExecutionHarness harness = CreateHarness();
        RoomUpgradeJobSynchronizationReport report = Synchronize(harness);
        CompleteDeliveries(harness, report);
        EntityId workJobId = report.WorkJobsCreated[0];
        BeginWork(harness, workJobId, FirstWorker, tick: 20);

        Result<RoomMaterialCommitResult> outOfOrder = harness.WorkIntervals.Handle(
            new CommitRoomUpgradeWorkIntervalCommand(
                workJobId,
                new RoomMaterialUnitId(RoomUpgradeMaterialIds.Stone, 2),
                tick: 21));

        Assert.True(outOfOrder.IsFailure);
        Assert.Equal(RoomInfrastructureErrors.InvalidMaterialUnit, outOfOrder.Error);
        Assert.Equal(4, Reserved(harness, workJobId, RoomUpgradeMaterialIds.Stone));
    }

    [Fact]
    public void Synchronization_is_idempotent_for_existing_jobs_and_reservations()
    {
        RoomUpgradeExecutionHarness harness = CreateHarness();
        RoomUpgradeJobSynchronizationReport first = Synchronize(harness);
        RoomUpgradeJobSynchronizationReport second = Synchronize(harness);

        Assert.Single(first.WorkJobsCreated);
        Assert.Equal(2, first.DeliveriesCreated.Count);
        Assert.Empty(second.WorkJobsCreated);
        Assert.Empty(second.DeliveriesCreated);
        Assert.Equal(3, harness.Jobs.Get().GetAll().Count);
        Assert.Equal(8, harness.Inventory.Get().CreateSnapshot().Stacks
            .Sum(stack => stack.Reservations.Sum(value => value.Quantity)));
    }
}

}
