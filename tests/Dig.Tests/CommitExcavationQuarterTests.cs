using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class CommitExcavationQuarterTests
{
    private static readonly EntityId Worker = EntityId.Parse(
        "72000000-0000-0000-0000-000000000001");
    private static readonly MaterialId Rock = new MaterialId("quarter-test.rock");
    private static readonly MaterialId Air = new MaterialId("quarter-test.air");
    private static readonly CellId Target = new CellId(1, 1, 0);

    [Fact]
    public void Committed_quarter_mutates_world_and_grants_its_profile_share()
    {
        Harness harness = new Harness(addWorker: true);

        Result<WorldMutationResult> result = harness.Commit(
            ExcavationQuarter.UpperLeft,
            DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.StoneExtraction),
            tick: 2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(1, result.Value.ChangedCellCount);
        Assert.Equal(
            ExcavationQuarter.UpperLeft,
            harness.World.GetCell(Target).Value.State.CompletedExcavationQuarters);
        Assert.Equal(25, harness.Skill(AgentSkillCatalog.Stonework));
        SkillProgressionResultConfirmed confirmed = Assert.Single(
            harness.Journal.Events.OfType<SkillProgressionResultConfirmed>());
        Assert.Equal(
            SkillGrantSourceKind.ExcavationQuarterCommitted,
            confirmed.Bundle.SourceKind);
    }

    [Fact]
    public void Duplicate_quarter_commit_is_idempotent_for_world_and_skill()
    {
        Harness harness = new Harness(addWorker: true);
        SkillGrantProfile profile = DefaultSkillProgressionContent.Catalog.GetProfile(
            DefaultSkillGrantProfileIds.StoneExtraction);
        Assert.True(harness.Commit(ExcavationQuarter.UpperLeft, profile, 2).IsSuccess);

        Result<WorldMutationResult> retry = harness.Commit(
            ExcavationQuarter.UpperLeft,
            profile,
            tick: 3);

        Assert.True(retry.IsSuccess, retry.Error?.ToString());
        Assert.Equal(0, retry.Value.ChangedCellCount);
        Assert.Equal(25, harness.Skill(AgentSkillCatalog.Stonework));
        Assert.Single(harness.Journal.Events.OfType<SkillProgressionResultConfirmed>());
    }

    [Fact]
    public void Missing_skill_recipient_rejects_before_world_mutation()
    {
        Harness harness = new Harness(addWorker: false);

        Result<WorldMutationResult> result = harness.Commit(
            ExcavationQuarter.UpperLeft,
            DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.StoneExtraction),
            tick: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ExcavationQuarter.None,
            harness.World.GetCell(Target).Value.State.CompletedExcavationQuarters);
    }

    [Theory]
    [InlineData("metallurgy")]
    [InlineData("alchemy")]
    public void Four_commits_grant_exact_deposit_profile_and_open_cell(string kind)
    {
        Harness harness = new Harness(addWorker: true);
        SkillGrantProfile profile = DefaultSkillProgressionContent.Catalog.GetProfile(
            kind == "metallurgy"
                ? DefaultSkillGrantProfileIds.Metallurgy
                : DefaultSkillGrantProfileIds.Alchemy);
        ExcavationQuarter[] quarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };

        for (int index = 0; index < quarters.Length; index++)
        {
            Result<WorldMutationResult> committed = harness.Commit(
                quarters[index],
                profile,
                tick: index + 2);
            Assert.True(committed.IsSuccess, committed.Error?.ToString());
        }

        AgentSkillId primary = kind == "metallurgy"
            ? AgentSkillCatalog.Metallurgy
            : AgentSkillCatalog.Alchemy;
        Assert.Equal(AgentSkillCatalog.UnitsPerPoint, harness.Skill(primary));
        Assert.Equal(
            AgentSkillCatalog.UnitsPerPoint / 4,
            harness.Skill(AgentSkillCatalog.Logistics));
        CellSnapshot cell = harness.World.GetCell(Target).Value;
        Assert.False(cell.IsSolid);
        Assert.Equal(ExcavationQuarter.All, cell.State.CompletedExcavationQuarters);
        Assert.Equal(4, harness.Journal.Events
            .OfType<SkillProgressionResultConfirmed>()
            .Count());
    }

    private sealed class Harness
    {
        private readonly InMemoryAgentRepository _agents = new InMemoryAgentRepository();
        private readonly CommitExcavationQuarterCommandHandler _handler;

        internal Harness(bool addWorker)
        {
            World = CreateWorld();
            Journal = new InMemoryExecutionJournal();
            if (addWorker)
            {
                Assert.True(_agents.Add(AgentTestFactory.CreateAgent(id: Worker)).IsSuccess);
            }

            _handler = new CommitExcavationQuarterCommandHandler(
                new InMemoryWorldRepository(World),
                new AgentSkillGrantService(_agents, Journal),
                Journal);
        }

        internal WorldState World { get; }
        internal InMemoryExecutionJournal Journal { get; }

        internal Result<WorldMutationResult> Commit(
            ExcavationQuarter quarter,
            SkillGrantProfile profile,
            long tick)
        {
            return _handler.Handle(new CommitExcavationQuarterCommand(
                Target,
                quarter,
                ExcavationCutPattern.VerticalColumns,
                Air,
                Worker,
                profile,
                tick));
        }

        internal int Skill(AgentSkillId skill)
        {
            return _agents.Get(Worker)!.CreateSnapshot(10).GetSkillLevel(skill);
        }
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 120),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(3, 3),
            chunkSize: 1,
            materials,
            Rock,
            explored: true).Value;
        Assert.True(world.SetDigDesignation(Target, designated: true, tick: 1).IsSuccess);
        world.DequeueUncommittedEvents();
        return world;
    }
}

}
