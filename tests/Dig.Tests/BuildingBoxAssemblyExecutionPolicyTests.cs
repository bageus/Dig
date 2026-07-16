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

public sealed class BuildingBoxAssemblyExecutionPolicyTests
{
    [Fact]
    public void World_box_requires_worker_at_source_before_start()
    {
        BuildingBoxHarness harness = CreateAssigned();
        BuildingSnapshot building = Building(harness);
        ItemStackSnapshot box = Box(harness);

        Assert.Equal(
            BuildingBoxAssemblyExecutionStepKind.None,
            Evaluate(harness, building, box, new CellId(0, 0)));
        Assert.Equal(
            BuildingBoxAssemblyExecutionStepKind.StartJob,
            Evaluate(harness, building, box, harness.SourceCell));
    }

    [Fact]
    public void Policy_drives_box_to_completed_building_without_duplication()
    {
        BuildingBoxHarness harness = CreateAssigned();
        long tick = 500;
        for (int step = 0; step < 20 && !harness.Jobs.Get(harness.JobId)!.IsTerminal; step++)
        {
            JobSnapshot job = harness.Jobs.Get(harness.JobId)!;
            BuildingSnapshot building = Building(harness);
            ItemStackSnapshot? box = harness.Inventory.GetStack(harness.SourceStackId);
            CellId workerCell = ResolveWorkerCell(job, building, box);
            BuildingBoxAssemblyExecutionStepKind action =
                Evaluate(harness, building, box, workerCell);
            Assert.NotEqual(BuildingBoxAssemblyExecutionStepKind.None, action);
            Execute(harness, action, workerCell, tick++);
        }

        Assert.Equal(BuildingStatus.Completed, Building(harness).Status);
        Assert.Equal(BuildingBoxCommitState.Consumed, Building(harness).BoxPlan!.CommitState);
        Assert.Null(harness.Inventory.GetStack(harness.SourceStackId));
        Assert.Equal(0, harness.Inventory.GetTotal(harness.BoxItemId));
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Equal(
            BuildingBoxAssemblyExecutionStepKind.None,
            Evaluate(harness, Building(harness), null, Building(harness).WorkPosition));
    }

    private static BuildingBoxHarness CreateAssigned()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(harness.JobId, new[]
        {
            new JobCandidate(harness.WorkerId, 5_000, distanceCost: 1, isAvailable: true),
        });
        JobAssignmentReport assigned = new AssignAvailableJobsHandler(
            harness.JobRepository,
            candidates,
            harness.Journal).Handle(new AssignAvailableJobsCommand(tick: 400));
        Assert.Single(assigned.Assignments);
        return harness;
    }

    private static CellId ResolveWorkerCell(
        JobSnapshot job,
        BuildingSnapshot building,
        ItemStackSnapshot? box)
    {
        bool acquiring = job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.AcquireItem;
        if (acquiring
            && box?.Location.Kind == ItemLocationKind.World
            && box.Location.HasCell)
        {
            return box.Location.CellId;
        }

        return building.WorkPosition;
    }

    private static BuildingBoxAssemblyExecutionStepKind Evaluate(
        BuildingBoxHarness harness,
        BuildingSnapshot building,
        ItemStackSnapshot? box,
        CellId workerCell)
    {
        Result<BuildingBoxAssemblyExecutionStepKind> result =
            BuildingBoxAssemblyExecutionPolicy.Evaluate(
                harness.Jobs.Get(harness.JobId),
                building,
                box,
                workerCell);
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }

    private static void Execute(
        BuildingBoxHarness harness,
        BuildingBoxAssemblyExecutionStepKind step,
        CellId workerCell,
        long tick)
    {
        Result result = step switch
        {
            BuildingBoxAssemblyExecutionStepKind.StartJob => Advance(harness, tick),
            BuildingBoxAssemblyExecutionStepKind.AcquireBox =>
                new AcquireBuildingBoxForAssemblyHandler(
                    harness.BuildingsRepository,
                    harness.InventoryRepository,
                    harness.JobRepository,
                    harness.Journal).Handle(new AcquireBuildingBoxForAssemblyCommand(
                        harness.BuildingId,
                        harness.JobId,
                        workerCell,
                        tick)),
            BuildingBoxAssemblyExecutionStepKind.AdvanceStage => Advance(harness, tick),
            BuildingBoxAssemblyExecutionStepKind.CommitBoxToSite =>
                new CommitBuildingBoxToSiteHandler(
                    harness.BuildingsRepository,
                    harness.InventoryRepository,
                    harness.JobRepository,
                    harness.Journal).Handle(new CommitBuildingBoxToSiteCommand(
                        harness.BuildingId,
                        harness.JobId,
                        tick)),
            BuildingBoxAssemblyExecutionStepKind.AddWork =>
                new AddBuildingBoxAssemblyWorkHandler(
                    harness.BuildingsRepository,
                    harness.JobRepository).Handle(new AddBuildingBoxAssemblyWorkCommand(
                        harness.BuildingId,
                        harness.JobId,
                        workAmount: 1,
                        tick: tick)),
            BuildingBoxAssemblyExecutionStepKind.CompleteAssembly =>
                new CompleteBuildingBoxAssemblyHandler(
                    harness.BuildingsRepository,
                    harness.InventoryRepository,
                    harness.JobRepository,
                    harness.Journal).Handle(new CompleteBuildingBoxAssemblyCommand(
                        harness.BuildingId,
                        harness.JobId,
                        tick)),
            _ => Result.Failure(new DomainError("test.invalid_step", "Unexpected step.")),
        };
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    private static Result Advance(BuildingBoxHarness harness, long tick)
    {
        return new AdvanceJobHandler(
            harness.JobRepository,
            harness.Journal).Handle(new AdvanceJobCommand(harness.JobId, tick));
    }

    private static BuildingSnapshot Building(BuildingBoxHarness harness)
    {
        return harness.Buildings.Get(harness.BuildingId)!;
    }

    private static ItemStackSnapshot Box(BuildingBoxHarness harness)
    {
        return harness.Inventory.GetStack(harness.SourceStackId)!;
    }
}
}
