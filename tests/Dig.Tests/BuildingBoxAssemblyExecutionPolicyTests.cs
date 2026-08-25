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
    public void Box_already_carried_by_worker_skips_external_pickup()
    {
        BuildingBoxHarness harness = CreateAssigned(carriedByResident: true);
        CellId arbitraryWorkerCell = new CellId(0, 0);

        Assert.Equal(
            BuildingBoxAssemblyExecutionStepKind.StartJob,
            Evaluate(harness, Building(harness), Box(harness), arbitraryWorkerCell));
        Assert.True(Advance(harness, tick: 450).IsSuccess);
        Assert.Equal(
            BuildingBoxAssemblyExecutionStepKind.AdvanceStage,
            Evaluate(harness, Building(harness), Box(harness), arbitraryWorkerCell));
        Assert.Equal(1, Box(harness).ReservedQuantity);
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

    [Theory]
    [InlineData(
        BuildingBoxAssemblyExecutionStepKind.StartJob,
        BuildingBoxAssemblyTickDisposition.ContinueCurrentTick)]
    [InlineData(
        BuildingBoxAssemblyExecutionStepKind.AdvanceStage,
        BuildingBoxAssemblyTickDisposition.ContinueCurrentTick)]
    [InlineData(
        BuildingBoxAssemblyExecutionStepKind.CommitBoxToSite,
        BuildingBoxAssemblyTickDisposition.StopCurrentTick)]
    [InlineData(
        BuildingBoxAssemblyExecutionStepKind.AddWork,
        BuildingBoxAssemblyTickDisposition.StopCurrentTick)]
    [InlineData(
        BuildingBoxAssemblyExecutionStepKind.CompleteAssembly,
        BuildingBoxAssemblyTickDisposition.Completed)]
    public void Tick_boundary_policy_preserves_observable_assembly_states(
        BuildingBoxAssemblyExecutionStepKind step,
        BuildingBoxAssemblyTickDisposition expected)
    {
        Assert.Equal(expected, BuildingBoxAssemblyTickBoundaryPolicy.AfterSuccessfulStep(step));
    }

    [Fact]
    public void Carried_box_at_site_exposes_five_fast_states_and_finalizes_on_next_tick()
    {
        BuildingBoxHarness harness = CreateAssigned(carriedByResident: true);
        CellId workPosition = Building(harness).WorkPosition;
        long tick = 700;

        ExecuteDemoTick(harness, workPosition, tick++);
        AssertAssemblyState(harness, BuildingStatus.ReadyToBuild, completedWork: 0);
        ItemStackSnapshot siteBox = Box(harness);
        Assert.Equal(ItemLocation.InBuilding(harness.BuildingId), siteBox.Location);
        Assert.Equal(JobStageKind.DepositItem, harness.Jobs.Get(harness.JobId)!.Stage);

        ExecuteDemoTick(harness, workPosition, tick++);
        AssertAssemblyState(harness, BuildingStatus.UnderConstruction, completedWork: 1);

        ExecuteDemoTick(harness, workPosition, tick++);
        AssertAssemblyState(harness, BuildingStatus.UnderConstruction, completedWork: 2);

        ExecuteDemoTick(harness, workPosition, tick++);
        AssertAssemblyState(harness, BuildingStatus.ReadyToComplete, completedWork: 3);
        Assert.Equal(JobStageKind.PerformWork, harness.Jobs.Get(harness.JobId)!.Stage);
        Assert.NotNull(harness.Inventory.GetStack(harness.SourceStackId));

        ExecuteDemoTick(harness, workPosition, tick);
        BuildingSnapshot completed = Building(harness);
        Assert.Equal(BuildingStatus.Completed, completed.Status);
        Assert.Equal(completed.Definition.RequiredWork, completed.CompletedWork);
        Assert.Equal(BuildingBoxCommitState.Consumed, completed.BoxPlan!.CommitState);
        Assert.Null(harness.Inventory.GetStack(harness.SourceStackId));
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.JobId)!.Status);
        Assert.Empty(harness.Jobs.GetReservations());
    }

    private static void ExecuteDemoTick(
        BuildingBoxHarness harness,
        CellId workerCell,
        long tick)
    {
        for (int stepIndex = 0; stepIndex < 16; stepIndex++)
        {
            JobSnapshot? job = harness.Jobs.Get(harness.JobId);
            if (job == null || job.IsTerminal)
            {
                return;
            }

            BuildingSnapshot building = Building(harness);
            ItemStackSnapshot? box = harness.Inventory.GetStack(harness.SourceStackId);
            BuildingBoxAssemblyExecutionStepKind step = Evaluate(
                harness,
                building,
                box,
                workerCell);
            Assert.NotEqual(BuildingBoxAssemblyExecutionStepKind.None, step);
            Execute(harness, step, workerCell, tick);

            BuildingBoxAssemblyTickDisposition disposition =
                BuildingBoxAssemblyTickBoundaryPolicy.AfterSuccessfulStep(step);
            if (disposition != BuildingBoxAssemblyTickDisposition.ContinueCurrentTick)
            {
                return;
            }
        }

        Assert.Fail("BuildingBox assembly exceeded the immediate transition limit.");
    }

    private static void AssertAssemblyState(
        BuildingBoxHarness harness,
        BuildingStatus expectedStatus,
        int completedWork)
    {
        BuildingSnapshot building = Building(harness);
        Assert.Equal(expectedStatus, building.Status);
        Assert.Equal(completedWork, building.CompletedWork);
        Assert.Equal(BuildingBoxCommitState.AtSite, building.BoxPlan!.CommitState);
        Assert.Equal(JobStatus.InProgress, harness.Jobs.Get(harness.JobId)!.Status);
    }

    private static BuildingBoxHarness CreateAssigned(bool carriedByResident = false)
    {
        BuildingBoxHarness harness = new BuildingBoxHarness(carriedByResident);
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        JobSnapshot job = harness.Jobs.Get(harness.JobId)!;
        if (job.Status == JobStatus.Available)
        {
            InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
            candidates.SetCandidates(harness.JobId, new[]
            {
                new JobCandidate(
                    harness.WorkerId,
                    5_000,
                    distanceCost: 1,
                    isAvailable: true),
            });
            JobAssignmentReport assigned = new AssignAvailableJobsHandler(
                harness.JobRepository,
                candidates,
                harness.Journal).Handle(new AssignAvailableJobsCommand(tick: 400));
            Assert.Single(assigned.Assignments);
        }
        else
        {
            Assert.Equal(JobStatus.Claimed, job.Status);
            Assert.Equal(harness.WorkerId, job.AssignedAgentId);
        }
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
                    harness.WorldRepository,
                    harness.BuildingsRepository,
                    harness.InventoryRepository,
                    harness.JobRepository,
                    new BuildingPlacementValidator(),
                    new PackableBuildingPlacementPolicyValidator(),
                    Dig.Domain.Content.CampfireBuildingBoxContent.Catalog,
                    harness.Journal).Handle(new CommitBuildingBoxToSiteCommand(
                        harness.BuildingId,
                        harness.JobId,
                        tick)),
            BuildingBoxAssemblyExecutionStepKind.AddWork =>
                new AddBuildingBoxAssemblyWorkHandler(
                    harness.BuildingsRepository,
                    harness.JobRepository,
                    harness.Journal).Handle(new AddBuildingBoxAssemblyWorkCommand(
                        harness.BuildingId,
                        harness.JobId,
                        workAmount: 1,
                        tick: tick)),
            BuildingBoxAssemblyExecutionStepKind.CompleteAssembly =>
                new CompleteBuildingBoxAssemblyHandler(
                    harness.BuildingsRepository,
                    harness.InventoryRepository,
                    harness.JobRepository,
                    harness.Journal,
                    AgentSkillGrantTestFactory.Create(
                        harness.WorkerId,
                        harness.Journal))
                    .Handle(new CompleteBuildingBoxAssemblyCommand(
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
