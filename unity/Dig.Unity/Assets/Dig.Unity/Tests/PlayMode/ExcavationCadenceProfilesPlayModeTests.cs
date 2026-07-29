using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ExcavationCadenceProfilesPlayModeTests
{
    [Test]
    public void Cadence_and_quarter_grants_are_deterministic()
    {
        ExcavationCadenceResolver cadence = new ExcavationCadenceResolver(
            ExcavationCadenceProfile.CreateLegacyDeterministic());
        Assert.That(cadence.Resolve(
            120,
            miningSkill: 0,
            equipmentIntervalTicks: 3,
            TerrainWorkPosture.Standing,
            tick: 1).IntervalTicks, Is.EqualTo(9));
        Assert.That(cadence.Resolve(
            120,
            miningSkill: 100,
            equipmentIntervalTicks: 3,
            TerrainWorkPosture.Standing,
            tick: 1).IntervalTicks, Is.EqualTo(1));

        MaterialId rock = new MaterialId("playmode.cadence.rock");
        MaterialId air = new MaterialId("playmode.cadence.air");
        CellId target = new CellId(1, 1);
        EntityId workerId = EntityId.Parse(
            "73000000-0000-0000-0000-000000000001");
        WorldState world = WorldState.CreateFilled(
            new WorldSize(3, 3),
            chunkSize: 1,
            new MaterialCatalog(new[]
            {
                new MaterialDefinition(rock, isSolid: true, hardness: 120),
                new MaterialDefinition(air, isSolid: false, hardness: 0),
            }),
            rock,
            explored: true).Value;
        Assert.That(world.SetDigDesignation(
            target,
            designated: true,
            tick: 1).IsSuccess, Is.True);
        world.DequeueUncommittedEvents();

        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.That(agents.Add(CreateAgent(workerId)).IsSuccess, Is.True);
        CommitExcavationQuarterCommandHandler handler =
            new CommitExcavationQuarterCommandHandler(
                new InMemoryWorldRepository(world),
                new AgentSkillGrantService(agents, journal),
                journal);
        SkillGrantProfile profile = DefaultSkillProgressionContent.Catalog.GetProfile(
            DefaultSkillGrantProfileIds.StoneExtraction);
        ExcavationQuarter[] quarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };

        for (int index = 0; index < quarters.Length; index++)
        {
            Result<WorldMutationResult> committed = handler.Handle(
                new CommitExcavationQuarterCommand(
                    target,
                    quarters[index],
                    ExcavationCutPattern.VerticalColumns,
                    air,
                    workerId,
                    profile,
                    tick: index + 2));
            Assert.That(committed.IsSuccess, Is.True, committed.Error?.ToString());
        }

        Assert.That(world.GetCell(target).Value.IsSolid, Is.False);
        Assert.That(
            agents.Get(workerId)!.CreateSnapshot(6)
                .GetSkillLevel(AgentSkillCatalog.Stonework),
            Is.EqualTo(AgentSkillCatalog.UnitsPerPoint));
    }

    private static AgentState CreateAgent(EntityId id)
    {
        return new AgentState(
            id,
            "Cadence Dwarf",
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(10_000)),
            new DailySchedule(
                12,
                new[] { new ScheduleSegment(0, 12, ScheduleActivity.Work) }));
    }
}

}
