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
        Advance(harness);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AdvanceStage, Evaluate(harness, workPosition));
        Advance(harness);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AddWork, Evaluate(harness, workPosition));
        Assert.True(harness.AddWork(amount: 1).IsSuccess);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AddWork, Evaluate(harness, workPosition));
        Assert.True(harness.AddWork(amount: 1).IsSuccess);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.AdvanceStage, Evaluate(harness, workPosition));
        Advance(harness);
        Assert.Equal(BuildingBoxPackingExecutionStepKind.CompletePacking, Evaluate(harness, workPosition));
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

    private static void Advance(BuildingBoxPackingHarness harness)
    {
        Result result = new AdvanceJobHandler(
            harness.Assembly.JobRepository,
            harness.Assembly.Journal).Handle(new AdvanceJobCommand(
                harness.PackingJobId,
                tick: 501));
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }
}
}
