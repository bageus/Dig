using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositSkillIntegrationTests
{
    private static readonly EntityId JobId = EntityId.Parse(
        "6a000000-0000-0000-0000-000000000001");
    private static readonly EntityId WorkerId = EntityId.Parse(
        "6a000000-0000-0000-0000-000000000002");
    private static readonly EntityId OutputId = EntityId.Parse(
        "6a000000-0000-0000-0000-000000000003");
    private static readonly MaterialId Rock = new MaterialId("deposit-skill.rock");
    private static readonly MaterialId Air = new MaterialId("deposit-skill.air");
    private static readonly CellId Target = new CellId(1, 1);

    [Theory]
    [InlineData("deposit.iron_ore", "ore.iron", "metallurgy")]
    [InlineData("deposit.crystal_ore", "ore.crystal", "alchemy")]
    public void Completed_deposit_job_uses_profile_carried_by_definition(
        string depositId,
        string outputItemId,
        string expectedSkill)
    {
        SkillGrantProfile profile = DefaultSkillProgressionContent.Catalog.GetProfile(
            expectedSkill == "metallurgy"
                ? DefaultSkillGrantProfileIds.Metallurgy
                : DefaultSkillGrantProfileIds.Alchemy);
        TerrainDepositDefinition deposit = new TerrainDepositDefinition(
            depositId,
            depositId,
            new ItemId(outputItemId),
            maximumYield: 4,
            generationWeight: 1,
            skillGrantProfile: profile);
        JobSystem jobs = CreateFinalizingJob(deposit.SkillGrantProfile);
        WorldState world = CreateWorld();
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(deposit.OutputItemId, outputItemId, 100, isTool: false),
        }));
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(id: WorkerId)).IsSuccess);
        CompleteTerrainWorkCommandHandler handler = new CompleteTerrainWorkCommandHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryWorldRepository(world),
            new InMemoryInventoryRepository(inventory),
            journal,
            new AgentSkillGrantService(agents, journal));

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            new CompleteTerrainWorkCommand(
                JobId,
                OutputId,
                deposit.OutputItemId,
                outputQuantity: 4,
                Air,
                tick: 5));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        AgentSkillId skillId = expectedSkill == "metallurgy"
            ? AgentSkillCatalog.Metallurgy
            : AgentSkillCatalog.Alchemy;
        AgentSnapshot worker = agents.Get(WorkerId)!.CreateSnapshot(tick: 5);
        Assert.Equal(AgentSkillCatalog.UnitsPerPoint,
            worker.GetSkillLevel(skillId));
        Assert.Equal(AgentSkillCatalog.UnitsPerPoint / 4,
            worker.GetSkillLevel(AgentSkillCatalog.Logistics));
    }

    private static JobSystem CreateFinalizingJob(SkillGrantProfile profile)
    {
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(Target),
            priority: 500,
            createdTick: 0,
            JobRetryPolicy.Default,
            skillGrantProfile: profile)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, tick: 1).IsSuccess);
        Assert.True(jobs.Start(JobId, tick: 2).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        return jobs;
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(3, 3),
            chunkSize: 1,
            materials,
            Rock,
            explored: true).Value;
    }
}

}
