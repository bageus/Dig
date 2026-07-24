using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Buildings;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireEpicAcceptanceTests
{
    private static readonly EntityId Operation =
        EntityId.Parse("89000000000000000000000000000001");
    private static readonly EntityId Package =
        EntityId.Parse("89000000000000000000000000000002");
    private static readonly EntityId WorkerA =
        EntityId.Parse("89000000000000000000000000000003");
    private static readonly EntityId WorkerB =
        EntityId.Parse("89000000000000000000000000000004");

    [Fact]
    public void Interrupted_campfire_operation_survives_save_and_completes_with_attributed_xp()
    {
        PackableBuildingContentDefinition campfire =
            CampfireBuildingBoxContent.Definition;
        Assert.Equal(3, campfire.Work.AssemblyIterations);
        Assert.Equal(3, campfire.Work.PackingIterations);
        Assert.Equal(10m, campfire.Work.BaseMinutesPerIteration);
        Assert.Equal(0.7m, campfire.Work.LogisticsPerCompletedIteration);

        RecordingSkills skills = new RecordingSkills(new Dictionary<EntityId, int>
        {
            [WorkerA] = 0,
            [WorkerB] = 100 * AgentSkillCatalog.UnitsPerPoint,
        });
        CampfireIterationProgressionService progression =
            new CampfireIterationProgressionService(skills, skills);
        PackableBuildingExecutionRegistry executions =
            new PackableBuildingExecutionRegistry();
        Assert.True(executions.GetOrCreate(
            Operation,
            Package,
            CampfireBuildingBoxContent.CampfireBuildingId,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);

        Assert.True(executions.StartOrResume(Operation, WorkerA).IsSuccess);
        Assert.Equal(600, progression.ResolveDurationSeconds(WorkerA).Value);
        Assert.True(executions.BeginIteration(
            Operation,
            WorkerA,
            startTick: 10,
            durationSeconds: 600).IsSuccess);
        Assert.False(executions.IsIterationReady(Operation, WorkerA, tick: 609).Value);
        Assert.True(executions.IsIterationReady(Operation, WorkerA, tick: 610).Value);
        Assert.True(progression.CompleteIteration(
            executions,
            Operation,
            WorkerA,
            tick: 610).IsSuccess);

        Assert.True(executions.Interrupt(Operation).IsSuccess);
        Assert.True(executions.StartOrResume(Operation, WorkerB).IsSuccess);
        Assert.Equal(240, progression.ResolveDurationSeconds(WorkerB).Value);
        Assert.True(executions.BeginIteration(
            Operation,
            WorkerB,
            startTick: 700,
            durationSeconds: 240).IsSuccess);
        Assert.True(executions.IsIterationReady(Operation, WorkerB, tick: 940).Value);
        Assert.True(progression.CompleteIteration(
            executions,
            Operation,
            WorkerB,
            tick: 940).IsSuccess);

        PackableBuildingExecutionsSaveData saved =
            PackableBuildingExecutionSaveDataAdapter.Encode(executions);
        Result<PackableBuildingExecutionRegistry> loaded =
            PackableBuildingExecutionSaveDataAdapter.Decode(saved);
        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        Assert.Equal(2, loaded.Value.Get(Operation)!.CompletedIterations);
        Assert.Equal(WorkerB, loaded.Value.Get(Operation)!.ActiveWorkerId);

        Assert.True(loaded.Value.Interrupt(Operation).IsSuccess);
        Assert.True(loaded.Value.StartOrResume(Operation, WorkerA).IsSuccess);
        Assert.True(progression.CompleteIteration(
            loaded.Value,
            Operation,
            WorkerA,
            tick: 1000).IsSuccess);

        PackableBuildingExecutionState completed = loaded.Value.Get(Operation)!;
        Assert.Equal(PackableBuildingExecutionStatus.Completed, completed.Status);
        Assert.Equal(3, completed.CompletedIterations);
        Assert.Equal(new[] { WorkerA, WorkerB, WorkerA },
            completed.CreateSnapshot().CompletedByWorkers);
        Assert.Equal(3, skills.Applied.Count);
        Assert.Equal(210, skills.Applied.Sum(bundle =>
            bundle.Grants.Single().RequestedUnits));
        Assert.Equal(3, skills.Applied.Select(bundle => bundle.SourceId).Distinct().Count());
        Assert.Equal(new[] { WorkerA, WorkerB, WorkerA },
            skills.Applied.Select(bundle => bundle.AgentId).ToArray());

        Result replay = progression.CompleteIteration(
            loaded.Value,
            Operation,
            WorkerA,
            tick: 1001);
        Assert.True(replay.IsFailure);
        Assert.Equal(3, skills.Applied.Count);

        PackableBuildingExecutionViewModel model = Assert.Single(
            CreatePresenter().Load(loaded.Value, tick: 1001));
        Assert.Equal("visual.campfire.active", model.VisualId);
        Assert.Equal("building.packable.unpack.completed", model.StatusLabelKey);
        Assert.True(model.IsTerminal);
    }

    private static PackableBuildingExecutionPresenter CreatePresenter()
    {
        return new PackableBuildingExecutionPresenter(
            CampfireBuildingBoxContent.Catalog,
            new PackableBuildingVisualCatalog(new[]
            {
                new PackableBuildingVisualProfile(
                    CampfireBuildingBoxContent.CampfireBuildingId,
                    "visual.campfire.active",
                    "visual.campfire.box.world",
                    "visual.campfire.box.inventory",
                    "visual.campfire.site.planned",
                    "visual.campfire.unpack.partial",
                    "visual.campfire.pack.partial",
                    "effect.campfire.iteration"),
            }));
    }

    private sealed class RecordingSkills : IAgentSkillGrantService, IAgentSkillLevelReader
    {
        private readonly IReadOnlyDictionary<EntityId, int> _levels;

        public RecordingSkills(IReadOnlyDictionary<EntityId, int> levels)
        {
            _levels = levels;
        }

        public List<SkillGrantBundle> Applied { get; } = new List<SkillGrantBundle>();

        public Result Validate(SkillGrantBundle bundle)
        {
            return _levels.ContainsKey(bundle.AgentId)
                ? Result.Success()
                : Result.Failure(new DomainError("test.agent_missing", "Agent is missing."));
        }

        public Result<int> GetSkillUnits(EntityId agentId, AgentSkillId skillId)
        {
            return _levels.TryGetValue(agentId, out int level)
                ? Result<int>.Success(level)
                : Result<int>.Failure(new DomainError(
                    "test.agent_missing",
                    "Agent is missing."));
        }

        public Result<SkillRedistributionReport> ApplyConfirmed(SkillGrantBundle bundle)
        {
            Applied.Add(bundle);
            return Result<SkillRedistributionReport>.Success(null!);
        }
    }
}

}