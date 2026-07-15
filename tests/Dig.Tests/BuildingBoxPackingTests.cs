using System;
using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPackingTests
{
    [Fact]
    public void Start_packing_creates_available_job_without_creating_box()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();

        Result result = harness.Start();

        Assert.True(result.IsSuccess, result.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.Completed, building.Status);
        Assert.Equal(BuildingPackingCommitState.Active, building.PackingPlan!.CommitState);
        Assert.Equal(harness.PackingJobId, building.PackingPlan.JobId);
        Assert.Equal(harness.OutputStackId, building.PackingPlan.OutputStackId);
        Assert.Equal(JobStatus.Available, harness.Jobs.Get(harness.PackingJobId)!.Status);
        Assert.Null(harness.Inventory.GetStack(harness.OutputStackId));
        Assert.Contains(building.Origin, harness.Buildings.GetOccupiedCells());
    }

    [Fact]
    public void Full_packing_job_creates_one_box_and_releases_footprint()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        harness.AssignAndAdvanceToWork();
        Assert.True(harness.AddWork(amount: 2).IsSuccess);
        harness.AdvanceToFinalize();

        Result result = harness.Complete();

        Assert.True(result.IsSuccess, result.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.Removed, building.Status);
        Assert.Equal(BuildingPackingCommitState.Completed, building.PackingPlan!.CommitState);
        Assert.Equal(2, building.PackingPlan.CompletedWork);
        ItemStackSnapshot output = harness.Inventory.GetStack(harness.OutputStackId)!;
        Assert.Equal(harness.BoxItemId, output.ItemId);
        Assert.Equal(1, output.Quantity);
        Assert.Equal(ItemLocation.InWorld(building.Origin), output.Location);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.PackingJobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.DoesNotContain(building.Origin, harness.Buildings.GetOccupiedCells());
    }

    [Fact]
    public void Cancelled_packing_keeps_completed_building_and_creates_no_box()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        harness.AssignAndAdvanceToWork();
        Assert.True(harness.AddWork(amount: 1).IsSuccess);

        Result result = harness.Cancel("player_cancelled");

        Assert.True(result.IsSuccess, result.Error?.ToString());
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;
        Assert.Equal(BuildingStatus.Completed, building.Status);
        Assert.Equal(BuildingPackingCommitState.Cancelled, building.PackingPlan!.CommitState);
        Assert.Equal(1, building.PackingPlan.CompletedWork);
        Assert.Equal(JobStatus.Cancelled, harness.Jobs.Get(harness.PackingJobId)!.Status);
        Assert.Null(harness.Inventory.GetStack(harness.OutputStackId));
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Contains(building.Origin, harness.Buildings.GetOccupiedCells());
    }

    [Fact]
    public void Completion_replay_cannot_create_a_second_box()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        harness.AssignAndAdvanceToWork();
        Assert.True(harness.AddWork(amount: 2).IsSuccess);
        harness.AdvanceToFinalize();
        Assert.True(harness.Complete().IsSuccess);

        Result replay = harness.Complete();

        Assert.True(replay.IsFailure);
        Assert.Equal(BuildingBoxPackingErrors.InvalidJobStage, replay.Error);
        Assert.Equal(1, harness.Inventory.GetTotal(harness.BoxItemId));
        Assert.Single(harness.Inventory.CreateSnapshot().Stacks);
    }

    [Fact]
    public void Active_packing_blocks_damage_and_direct_removal()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);

        Result damage = harness.Buildings.Damage(harness.BuildingId, amount: 1, tick: 200);
        Result remove = harness.Buildings.Remove(
            harness.BuildingId,
            "external_remove",
            tick: 201);

        Assert.Equal(BuildingErrors.InvalidStatus, damage.Error);
        Assert.Equal(BuildingErrors.InvalidStatus, remove.Error);
        Assert.Equal(BuildingStatus.Completed, harness.Buildings.Get(harness.BuildingId)!.Status);
    }
}

internal sealed class BuildingBoxPackingHarness
{
    private readonly InMemoryJobCandidateProvider _candidates =
        new InMemoryJobCandidateProvider();
    private long _tick = 100;

    public BuildingBoxPackingHarness()
    {
        Assembly = new BuildingBoxHarness();
        CompleteAssembly();
        PackingJobId = BuildingBoxHarness.Id(30);
        OutputStackId = BuildingBoxHarness.Id(31);
    }

    public BuildingBoxHarness Assembly { get; }
    public EntityId BuildingId => Assembly.BuildingId;
    public EntityId WorkerId => Assembly.WorkerId;
    public ItemId BoxItemId => Assembly.BoxItemId;
    public EntityId PackingJobId { get; }
    public EntityId OutputStackId { get; }
    public InventoryState Inventory => Assembly.Inventory;
    public BuildingsState Buildings => Assembly.Buildings;
    public JobSystem Jobs => Assembly.Jobs;

    public Result Start()
    {
        return new StartBuildingBoxPackingHandler(
            Assembly.BuildingsRepository,
            Assembly.InventoryRepository,
            Assembly.JobRepository,
            Assembly.Journal).Handle(new StartBuildingBoxPackingCommand(
                BuildingId,
                PackingJobId,
                OutputStackId,
                priority: 650,
                tick: _tick++));
    }

    public void AssignAndAdvanceToWork()
    {
        _candidates.SetCandidates(PackingJobId, new[]
        {
            new JobCandidate(WorkerId, 5000, distanceCost: 1, isAvailable: true),
        });
        Assert.Single(new AssignAvailableJobsHandler(
            Assembly.JobRepository,
            _candidates,
            Assembly.Journal).Handle(new AssignAvailableJobsCommand(_tick++)).Assignments);
        Advance();
        Assert.Equal(JobStageKind.TravelToDestination, Jobs.Get(PackingJobId)!.Stage);
        Advance();
        Assert.Equal(JobStageKind.PerformWork, Jobs.Get(PackingJobId)!.Stage);
    }

    public Result AddWork(int amount)
    {
        return new AddBuildingBoxPackingWorkHandler(
            Assembly.BuildingsRepository,
            Assembly.JobRepository,
            Assembly.Journal).Handle(new AddBuildingBoxPackingWorkCommand(
                BuildingId,
                PackingJobId,
                amount,
                _tick++));
    }

    public void AdvanceToFinalize()
    {
        Advance();
        Assert.Equal(JobStageKind.Finalize, Jobs.Get(PackingJobId)!.Stage);
    }

    public Result Complete()
    {
        return new CompleteBuildingBoxPackingHandler(
            Assembly.BuildingsRepository,
            Assembly.InventoryRepository,
            Assembly.JobRepository,
            Assembly.Journal).Handle(new CompleteBuildingBoxPackingCommand(
                BuildingId,
                PackingJobId,
                _tick++));
    }

    public Result Cancel(string reason)
    {
        return new CancelBuildingBoxPackingHandler(
            Assembly.BuildingsRepository,
            Assembly.JobRepository,
            Assembly.Journal).Handle(new CancelBuildingBoxPackingCommand(
                BuildingId,
                reason,
                _tick++));
    }

    private void CompleteAssembly()
    {
        Assert.True(Assembly.Confirm(
            Assembly.BuildingId,
            Assembly.JobId,
            new CellId(3, 3)).IsSuccess);
        Assembly.AssignAndAdvanceToDeposit();
        Assert.True(Assembly.CommitToSite().IsSuccess);
        Assembly.AdvanceToPerformWork();
        Assert.True(Assembly.AddWork(amount: 3).IsSuccess);
        Assembly.AdvanceToFinalize();
        Assert.True(Assembly.Complete().IsSuccess);
    }

    private void Advance()
    {
        Assert.True(new AdvanceJobHandler(
            Assembly.JobRepository,
            Assembly.Journal).Handle(new AdvanceJobCommand(
                PackingJobId,
                _tick++)).IsSuccess);
    }
}
}
