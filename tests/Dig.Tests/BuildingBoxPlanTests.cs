using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPlanTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Confirm_reserves_one_box_and_creates_available_job(bool carriedByResident)
    {
        BuildingBoxHarness harness = new BuildingBoxHarness(carriedByResident);

        Result result = harness.Confirm(harness.BuildingId, harness.JobId, new CellId(3, 3));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.AwaitingBox, building.Status);
        Assert.Equal(BuildingBoxCommitState.Reserved, building.BoxPlan!.CommitState);
        Assert.Equal(harness.SourceStackId, building.BoxPlan.SourceStackId);
        Assert.Equal(harness.JobId, building.BoxPlan.JobId);
        Assert.Equal(1, harness.Inventory.GetStack(harness.SourceStackId)!.ReservedQuantity);
        JobSnapshot job = harness.Jobs.Get(harness.JobId)!;
        Assert.Equal(JobStatus.Available, job.Status);
        Assert.IsType<BuildingBoxAssemblyJobDefinition>(job.Definition);
    }

    [Fact]
    public void Invalid_placement_leaves_box_plan_and_job_absent()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();

        Result result = harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(99, 99));

        Assert.True(result.IsFailure);
        Assert.Equal(BuildingErrors.PlacementOutOfBounds, result.Error);
        Assert.Null(harness.Buildings.Get(harness.BuildingId));
        Assert.Null(harness.Jobs.Get(harness.JobId));
        Assert.Equal(0, harness.Inventory.GetStack(harness.SourceStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Competing_plans_cannot_reserve_the_same_box()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        EntityId secondBuilding = BuildingBoxHarness.Id(20);
        EntityId secondJob = BuildingBoxHarness.Id(21);

        Result second = harness.Confirm(
            secondBuilding,
            secondJob,
            new CellId(5, 5));

        Assert.True(second.IsFailure);
        Assert.Equal(InventoryErrors.InsufficientAvailableQuantity, second.Error);
        Assert.Null(harness.Buildings.Get(secondBuilding));
        Assert.Null(harness.Jobs.Get(secondJob));
        Assert.Equal(1, harness.Inventory.GetStack(harness.SourceStackId)!.ReservedQuantity);
    }

    [Fact]
    public void Full_job_lifecycle_consumes_exact_box_and_completes_building()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        harness.AssignAndAdvanceToDeposit();
        Assert.True(harness.CommitToSite().IsSuccess);
        harness.AdvanceToPerformWork();
        Assert.True(harness.AddWork(amount: 3).IsSuccess);
        harness.AdvanceToFinalize();

        Result completed = harness.Complete();

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.Completed, building.Status);
        Assert.Equal(BuildingBoxCommitState.Consumed, building.BoxPlan!.CommitState);
        Assert.Null(harness.Inventory.GetStack(harness.SourceStackId));
        Assert.Equal(0, harness.Inventory.GetTotal(harness.BoxItemId));
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Cancel_before_delivery_releases_box_and_job()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);

        Result cancelled = harness.Cancel("player_cancelled");

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        Assert.Equal(BuildingStatus.Cancelled, harness.Buildings.Get(harness.BuildingId)!.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        ItemStackSnapshot box = harness.Inventory.GetStack(harness.SourceStackId)!;
        Assert.Equal(1, box.Quantity);
        Assert.Equal(0, box.ReservedQuantity);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    [Fact]
    public void Cancel_after_site_commit_returns_box_to_world()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness(carriedByResident: true);
        CellId origin = new CellId(3, 3);
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            origin).IsSuccess);
        harness.AssignAndAdvanceToDeposit();
        Assert.True(harness.CommitToSite().IsSuccess);

        Result cancelled = harness.Cancel("placement_changed");

        Assert.True(cancelled.IsSuccess, cancelled.Error?.ToString());
        ItemStackSnapshot box = harness.Inventory.GetStack(harness.SourceStackId)!;
        Assert.Equal(ItemLocation.InWorld(origin), box.Location);
        Assert.Equal(1, box.Quantity);
        Assert.Equal(0, box.ReservedQuantity);
        Assert.Equal(BuildingStatus.Cancelled, harness.Buildings.Get(harness.BuildingId)!.Status);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }
}
}
