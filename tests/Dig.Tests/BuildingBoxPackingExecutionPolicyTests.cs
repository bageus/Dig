using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPackingExecutionPolicyTests
{
    [Fact]
    public void Available_job_and_worker_away_from_target_produce_no_execution_step()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        BuildingSnapshot building = harness.Buildings.Get(harness.BuildingId)!;

        Assert.Equal(
            BuildingBoxPackingExecutionStepKind.None,
            Evaluate(harness, building.Origin));

        Assign(harness);
        Assert.Equal(
            BuildingBoxPackingExecutionStepKind.None,
            Evaluate(harness, building.Origin));
    }

    [Fact]
    public void Logical_worker_position_drives_the_complete_execution_sequence()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        Assign(harness);
        CellId workPosition = harness.Buildings.Get(harness.BuildingId)!.WorkPosition;

        Assert.Equal(BuildingBoxPackingExecutionStepKind.StartJob, Evaluate(harness, workPosition));
        Advance(harness, tick: 501);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AdvanceStage, Evaluate(harness, workPosition));
        Advance(harness, tick: 502);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AddWork, Evaluate(harness, workPosition));
        Assert.True(harness.AddWork(amount: 1).IsSuccess);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AddWork, Evaluate(harness, workPosition));
        Assert.True(harness.AddWork(amount: 1).IsSuccess);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AdvanceStage, Evaluate(harness, workPosition));
        Advance(harness, tick: 503);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.CompletePacking, Evaluate(harness, workPosition));
    }

    [Fact]
    public void Policy_driven_execution_completes_once_and_creates_one_box()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        Assign(harness);
        CellId workPosition = harness.Buildings.Get(harness.BuildingId)!.WorkPosition;

        for (long tick = 600; tick < 610; tick++)
        {
            BuildingBoxPackingExecutionStepKind step = Evaluate(harness, workPosition);
            Result result = Execute(harness, step, tick);
            Assert.True(result.IsSuccess, result.Error?.ToString());
            if (harness.Jobs.Get(harness.PackingJobId)!.IsTerminal)
            {
                break;
            }
        }

        Assert.Equal(
            BuildingStatus.Removed,
            harness.Buildings.Get(harness.BuildingId)!.Status);
        Assert.Equal(JobStatus.Completed, harness.Jobs.Get(harness.PackingJobId)!.Status);
        Assert.NotNull(harness.Inventory.GetStack(harness.OutputStackId));
        Assert.Equal(1, harness.Inventory.GetTotal(harness.BoxItemId));
        Assert.Empty(harness.Jobs.GetReservations());
        Assert.Equal(
            BuildingBoxPackingExecutionStepKind.None,
            Evaluate(harness, workPosition));
    }

    [Fact]
    public void Terminal_packing_job_produces_no_replay_step()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        harness.AssignAndAdvanceToWork();
        Assert.True(harness.AddWork(amount: 2).IsSuccess);
        harness.AdvanceToFinalize();
        Assert.True(harness.Complete().IsSuccess);
        CellId origin = harness.Buildings.Get(harness.BuildingId)!.Origin;

        Assert.Equal(
            BuildingBoxPackingExecutionStepKind.None,
            Evaluate(harness, origin));
    }

    private static BuildingBoxPackingExecutionStepKind Evaluate(
        BuildingBoxPackingHarness harness,
        CellId workerCell)
    {
        Result<BuildingBoxPackingExecutionStepKind> result =
            BuildingBoxPackingExecutionPolicy.Evaluate(
                harness.Jobs.Get(harness.PackingJobId),
                harness.Buildings.Get(harness.BuildingId),
                workerCell);
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }

    private static Result Execute(
        BuildingBoxPackingHarness harness,
        BuildingBoxPackingExecutionStepKind step,
        long tick)
    {
        return step switch
        {
            BuildingBoxPackingExecutionStepKind.StartJob => AdvanceResult(harness, tick),
            BuildingBoxPackingExecutionStepKind.AdvanceStage => AdvanceResult(harness, tick),
            BuildingBoxPackingExecutionStepKind.AddWork =>
                new AddBuildingBoxPackingWorkHandler(
                    harness.Assembly.BuildingsRepository,
                    harness.Assembly.JobRepository,
                    harness.Assembly.Journal).Handle(new AddBuildingBoxPackingWorkCommand(
                        harness.BuildingId,
                        harness.PackingJobId,
                        workAmount: 1,
                        tick: tick)),
            BuildingBoxPackingExecutionStepKind.CompletePacking =>
                new CompleteBuildingBoxPackingHandler(
                    harness.Assembly.BuildingsRepository,
                    harness.Assembly.InventoryRepository,
                    harness.Assembly.JobRepository,
                    harness.Assembly.Journal,
                    AgentSkillGrantTestFactory.Create(
                        harness.WorkerId,
                        harness.Assembly.Journal))
                    .Handle(new CompleteBuildingBoxPackingCommand(
                        harness.BuildingId,
                        harness.PackingJobId,
                        tick)),
            _ => Result.Success(),
        };
    }

    private static void Assign(BuildingBoxPackingHarness harness)
    {
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(harness.PackingJobId, new[]
        {
            new JobCandidate(
                harness.WorkerId,
                skillLevel: 5_000,
                distanceCost: 1,
                isAvailable: true),
        });
        JobAssignmentReport report = new AssignAvailableJobsHandler(
            harness.Assembly.JobRepository,
            candidates,
            harness.Assembly.Journal).Handle(new AssignAvailableJobsCommand(tick: 500));
        Assert.Single(report.Assignments);
    }

    private static void Advance(BuildingBoxPackingHarness harness, long tick)
    {
        Result result = AdvanceResult(harness, tick);
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    private static Result AdvanceResult(BuildingBoxPackingHarness harness, long tick)
    {
        return new AdvanceJobHandler(
            harness.Assembly.JobRepository,
            harness.Assembly.Journal).Handle(new AdvanceJobCommand(
                harness.PackingJobId,
                tick));
    }
}
}
