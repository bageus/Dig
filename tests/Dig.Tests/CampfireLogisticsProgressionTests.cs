using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Application.Buildings;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireLogisticsProgressionTests
{
    private static readonly EntityId Operation =
        EntityId.Parse("85000000000000000000000000000001");
    private static readonly EntityId Package =
        EntityId.Parse("85000000000000000000000000000002");
    private static readonly EntityId WorkerA =
        EntityId.Parse("85000000000000000000000000000003");
    private static readonly EntityId WorkerB =
        EntityId.Parse("85000000000000000000000000000004");
    private static readonly BuildingDefinitionId Definition =
        new BuildingDefinitionId("building.campfire");

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 5)]
    [InlineData(11, 6)]
    [InlineData(20, 10)]
    [InlineData(21, 11)]
    [InlineData(40, 30)]
    [InlineData(41, 31)]
    [InlineData(60, 40)]
    [InlineData(61, 41)]
    [InlineData(80, 50)]
    [InlineData(81, 51)]
    [InlineData(100, 60)]
    [InlineData(140, 60)]
    public void Reduction_matches_every_skill_band_boundary(int points, int expected)
    {
        int units = points * AgentSkillCatalog.UnitsPerPoint;

        Assert.Equal(expected, CampfireLogisticsTimingPolicy.ResolveReductionPercent(units));
    }

    [Fact]
    public void Effective_duration_is_deterministic_and_never_below_forty_percent()
    {
        Assert.Equal(600, CampfireLogisticsTimingPolicy.ResolveDurationSeconds(0));
        Assert.Equal(240, CampfireLogisticsTimingPolicy.ResolveDurationSeconds(
            150 * AgentSkillCatalog.UnitsPerPoint));
        Assert.Equal(
            CampfireLogisticsTimingPolicy.ResolveDurationSeconds(37 * AgentSkillCatalog.UnitsPerPoint),
            CampfireLogisticsTimingPolicy.ResolveDurationSeconds(37 * AgentSkillCatalog.UnitsPerPoint));
    }

    [Fact]
    public void Completed_iterations_award_point_seven_to_the_authoritative_worker_once()
    {
        RecordingSkillGrants grants = new RecordingSkillGrants();
        CampfireIterationProgressionService progression =
            new CampfireIterationProgressionService(grants);
        PackableBuildingExecutionRegistry executions = CreateExecution();

        Assert.True(executions.StartOrResume(Operation, WorkerA).IsSuccess);
        Assert.True(progression.CompleteIteration(executions, Operation, WorkerA, tick: 10).IsSuccess);
        Assert.True(executions.Interrupt(Operation).IsSuccess);
        Assert.True(executions.StartOrResume(Operation, WorkerB).IsSuccess);
        Assert.True(progression.CompleteIteration(executions, Operation, WorkerB, tick: 20).IsSuccess);

        Assert.Equal(2, grants.Applied.Count);
        Assert.Equal(WorkerA, grants.Applied[0].AgentId);
        Assert.Equal(WorkerB, grants.Applied[1].AgentId);
        Assert.Equal(70, grants.Applied[0].Grants[0].RequestedUnits);
        Assert.Equal("campfire:" + Operation + ":iteration:1", grants.Applied[0].SourceId);
        Assert.Equal("campfire:" + Operation + ":iteration:2", grants.Applied[1].SourceId);
    }

    [Fact]
    public void Stale_worker_cannot_receive_iteration_experience()
    {
        RecordingSkillGrants grants = new RecordingSkillGrants();
        CampfireIterationProgressionService progression =
            new CampfireIterationProgressionService(grants);
        PackableBuildingExecutionRegistry executions = CreateExecution();

        Assert.True(executions.StartOrResume(Operation, WorkerA).IsSuccess);
        Assert.True(executions.Interrupt(Operation).IsSuccess);
        Assert.True(executions.StartOrResume(Operation, WorkerB).IsSuccess);

        Result stale = progression.CompleteIteration(executions, Operation, WorkerA, tick: 15);

        Assert.True(stale.IsFailure);
        Assert.Empty(grants.Applied);
    }

    private static PackableBuildingExecutionRegistry CreateExecution()
    {
        PackableBuildingExecutionRegistry executions = new PackableBuildingExecutionRegistry();
        Assert.True(executions.GetOrCreate(
            Operation,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);
        return executions;
    }

    private sealed class RecordingSkillGrants : IAgentSkillGrantService
    {
        public List<SkillGrantBundle> Applied { get; } = new List<SkillGrantBundle>();

        public Result Validate(SkillGrantBundle bundle)
        {
            return Result.Success();
        }

        public Result<SkillRedistributionReport> ApplyConfirmed(SkillGrantBundle bundle)
        {
            Applied.Add(bundle);
            return Result<SkillRedistributionReport>.Success(null!);
        }
    }
}

}
