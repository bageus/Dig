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

public sealed class TerrainDepositSkillIntegrationTests
{
    private static readonly EntityId WorkerId = EntityId.Parse(
        "6a000000-0000-0000-0000-000000000002");
    private static readonly MaterialId Rock = new MaterialId("deposit-skill.rock");
    private static readonly MaterialId Air = new MaterialId("deposit-skill.air");
    private static readonly CellId Target = new CellId(1, 1);

    [Theory]
    [InlineData("metallurgy")]
    [InlineData("alchemy")]
    public void Deposit_profile_is_applied_by_confirmed_quarters_not_job_finalization(
        string expectedSkill)
    {
        SkillGrantProfile profile = DefaultSkillProgressionContent.Catalog.GetProfile(
            expectedSkill == "metallurgy"
                ? DefaultSkillGrantProfileIds.Metallurgy
                : DefaultSkillGrantProfileIds.Alchemy);
        WorldState world = CreateWorld();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(id: WorkerId)).IsSuccess);
        AgentSnapshot before = agents.Get(WorkerId)!.CreateSnapshot(tick: 1);
        int logisticsBefore = before.GetSkillLevel(AgentSkillCatalog.Logistics);
        CommitExcavationQuarterCommandHandler handler =
            new CommitExcavationQuarterCommandHandler(
                new InMemoryWorldRepository(world),
                new AgentSkillGrantService(agents, journal),
                journal);
        ExcavationQuarter[] quarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };

        for (int index = 0; index < quarters.Length; index++)
        {
            Result<WorldMutationResult> result = handler.Handle(
                new CommitExcavationQuarterCommand(
                    Target,
                    quarters[index],
                    ExcavationCutPattern.VerticalColumns,
                    Air,
                    WorkerId,
                    profile,
                    tick: index + 2));
            Assert.True(result.IsSuccess, result.Error?.ToString());
        }

        AgentSkillId skillId = expectedSkill == "metallurgy"
            ? AgentSkillCatalog.Metallurgy
            : AgentSkillCatalog.Alchemy;
        AgentSnapshot worker = agents.Get(WorkerId)!.CreateSnapshot(tick: 6);
        Assert.Equal(
            AgentSkillCatalog.UnitsPerPoint,
            worker.GetSkillLevel(skillId));
        Assert.Equal(
            logisticsBefore + (AgentSkillCatalog.UnitsPerPoint / 4),
            worker.GetSkillLevel(AgentSkillCatalog.Logistics));
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
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
