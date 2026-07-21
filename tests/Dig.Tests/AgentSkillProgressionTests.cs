using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentSkillProgressionTests
{
    [Fact]
    public void Catalog_has_exactly_twelve_unique_stable_ids()
    {
        Assert.Equal(12, AgentSkillCatalog.All.Count);
        Assert.Equal(12, AgentSkillCatalog.All.Select(value => value.Id).Distinct().Count());
        Assert.Equal(7, AgentSkillCatalog.All.Count(
            value => value.Category == AgentSkillCategory.Work));
        Assert.Equal(5, AgentSkillCatalog.All.Count(
            value => value.Category == AgentSkillCategory.Combat));
    }

    [Fact]
    public void Work_profiles_cover_all_seven_work_skills()
    {
        AgentSkillId[] covered = DefaultSkillProgressionContent.Catalog.Profiles
            .Where(profile => profile.Id != DefaultSkillGrantProfileIds.Construction)
            .SelectMany(profile => profile.Profile.PerUnit)
            .Select(grant => grant.SkillId)
            .Distinct()
            .ToArray();

        Assert.Equal(7, covered.Length);
        Assert.Equal(
            AgentSkillCatalog.All
                .Where(value => value.Category == AgentSkillCategory.Work)
                .Select(value => value.Id)
                .OrderBy(value => value),
            covered.OrderBy(value => value));
    }

    [Fact]
    public void Free_capacity_applies_gain_without_donor_loss()
    {
        AgentState agent = CreateAgent();

        SkillRedistributionReport report = Apply(
            agent,
            "free",
            new SkillGrant(AgentSkillCatalog.Stonework, 400));

        Assert.Equal(400, report.FreeCapacityGainUnits);
        Assert.Equal(0, report.OverflowUnits);
        Assert.Empty(report.DonorLosses.Where(value => value.LossUnits > 0));
        Assert.Equal(400, agent.CreateSnapshot(1).GetSkillLevel(
            AgentSkillCatalog.Stonework));
    }

    [Fact]
    public void Full_capacity_uses_exact_proportional_largest_remainder_losses()
    {
        AgentState agent = CreateAgent();
        Set(agent, AgentSkillCatalog.Woodworking, 6_000);
        Set(agent, AgentSkillCatalog.Logistics, 3_000);
        Set(agent, AgentSkillCatalog.Cooking, 1_000);

        SkillRedistributionReport report = Apply(
            agent,
            "overflow",
            new SkillGrant(AgentSkillCatalog.Stonework, 400));

        Assert.Equal(400, report.OverflowUnits);
        Assert.Equal(10_000, report.SumAfterUnits);
        Assert.Equal(400, report.DonorLosses.Sum(value => value.LossUnits));
        Assert.True(
            Loss(report, AgentSkillCatalog.Woodworking)
            > Loss(report, AgentSkillCatalog.Cooking));
        Assert.Equal(400, report.Grants.Single().AppliedUnits);
    }

    [Fact]
    public void Mixed_bundle_is_order_independent_and_recipients_are_not_donors()
    {
        AgentState first = FullAgent();
        AgentState second = FullAgent();
        SkillGrant stone = new SkillGrant(AgentSkillCatalog.Stonework, 333);
        SkillGrant cook = new SkillGrant(AgentSkillCatalog.Cooking, 167);

        SkillRedistributionReport firstReport = Apply(first, "mixed", stone, cook);
        SkillRedistributionReport secondReport = Apply(second, "mixed", cook, stone);

        Assert.Equal(
            firstReport.ResultingValues.Select(ValueTuple),
            secondReport.ResultingValues.Select(ValueTuple));
        Assert.DoesNotContain(firstReport.DonorLosses,
            value => value.SkillId == AgentSkillCatalog.Stonework
                || value.SkillId == AgentSkillCatalog.Cooking);
        Assert.Equal(500, firstReport.OverflowUnits);
    }

    [Fact]
    public void Individual_max_clamps_and_reports_rejected_units()
    {
        AgentState agent = CreateAgent();
        Set(agent, AgentSkillCatalog.Stonework, 9_900);

        SkillRedistributionReport report = Apply(
            agent,
            "clamp",
            new SkillGrant(AgentSkillCatalog.Stonework, 500));

        Assert.Equal(100, report.Grants.Single().AppliedUnits);
        Assert.Equal(400, report.Grants.Single().RejectedUnits);
        Assert.Equal(10_000, agent.CreateSnapshot(1).GetSkillLevel(
            AgentSkillCatalog.Stonework));
    }

    [Fact]
    public void University_expands_same_pool_to_two_hundred_points()
    {
        AgentState agent = FullAgent();
        Assert.True(agent.ExpandTotalSkillCapacity(
            AgentSkillCatalog.UniversityCapacityUnits,
            tick: 1).IsSuccess);

        SkillRedistributionReport report = Apply(
            agent,
            "university",
            new SkillGrant(AgentSkillCatalog.Stonework, 500));

        Assert.Equal(500, report.FreeCapacityGainUnits);
        Assert.Equal(0, report.OverflowUnits);
        Assert.Equal(AgentSkillCatalog.UniversityCapacityUnits,
            agent.CreateSnapshot(1).SkillProgression!.TotalCapacityUnits);
    }

    [Fact]
    public void Source_key_is_idempotent_before_and_after_restore()
    {
        AgentState agent = CreateAgent();
        SkillGrantBundle bundle = Bundle(
            agent,
            "stable-source",
            new SkillGrant(AgentSkillCatalog.Logistics, 250));
        Assert.False(agent.ApplySkillGrant(bundle).Value.WasAlreadyApplied);
        Assert.True(agent.ApplySkillGrant(bundle).Value.WasAlreadyApplied);
        AgentSkillProgressionSnapshot saved = agent.CreateSnapshot(1).SkillProgression!;
        AgentState restored = CreateAgent();
        Assert.True(restored.RestoreSkillProgression(saved).IsSuccess);

        SkillRedistributionReport replay = restored.ApplySkillGrant(bundle).Value;

        Assert.True(replay.WasAlreadyApplied);
        Assert.Equal(250, restored.CreateSnapshot(1).GetSkillLevel(
            AgentSkillCatalog.Logistics));
    }

    [Fact]
    public void Legacy_values_over_base_capacity_migrate_proportionally()
    {
        AgentState agent = new AgentState(
            AgentTestFactory.DefaultAgentId,
            "Legacy Skill Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            new[]
            {
                new AgentSkillValue(new AgentSkillId("mining"), 10_000),
                new AgentSkillValue(new AgentSkillId("building"), 10_000),
            });

        AgentSkillProgressionSnapshot migrated =
            agent.CreateSkillProgressionSnapshot();

        Assert.Equal(AgentSkillCatalog.BaseCapacityUnits, migrated.UsedCapacityUnits);
        Assert.Equal(5_000, migrated.GetLevel(AgentSkillCatalog.Stonework));
        Assert.Equal(5_000, migrated.GetLevel(AgentSkillCatalog.Woodworking));
    }

    [Fact]
    public void Legacy_progression_snapshot_is_a_pure_query_with_migration_diagnostics()
    {
        AgentState agent = new AgentState(
            EntityId.Parse("11111111111111111111111111111112"),
            "Legacy Query Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            new[]
            {
                new AgentSkillValue(new AgentSkillId("mining"), 5_000),
                new AgentSkillValue(new AgentSkillId("general.work"), 4_000),
            });
        long version = agent.Version;

        AgentSkillProgressionSnapshot first = agent.CreateSkillProgressionSnapshot();
        AgentSkillProgressionSnapshot second = agent.CreateSkillProgressionSnapshot();

        Assert.Equal(version, agent.Version);
        Assert.Equal(first.Values, second.Values);
        Assert.Equal(5_000, first.GetLevel(AgentSkillCatalog.Stonework));
        Assert.Equal(4_000, first.GetLevel(AgentSkillCatalog.Logistics));
        Assert.Contains(
            "agent-skills.legacy-capabilities-to-catalog",
            first.MigrationSteps);
    }

    [Fact]
    public void Explicit_catalog_mutation_converts_legacy_state_once()
    {
        AgentState agent = new AgentState(
            EntityId.Parse("11111111111111111111111111111113"),
            "Legacy Command Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            new[] { new AgentSkillValue(new AgentSkillId("mining"), 5_000) });
        long version = agent.Version;

        Result changed = agent.SetSkillLevel(AgentSkillCatalog.Cooking, 500);
        AgentSnapshot snapshot = agent.CreateSnapshot(tick: 1);

        Assert.True(changed.IsSuccess);
        Assert.Equal(version + 1, agent.Version);
        Assert.Equal(12, snapshot.Skills.Count);
        Assert.Equal(5_000, snapshot.GetSkillLevel(AgentSkillCatalog.Stonework));
        Assert.Equal(500, snapshot.GetSkillLevel(AgentSkillCatalog.Cooking));
    }

    [Fact]
    public void Documented_example_preserves_exact_sum()
    {
        AgentState agent = CreateAgent();
        AgentSkillId[] ids = AgentSkillCatalog.All.Select(value => value.Id).ToArray();
        int[] points = { 50, 20, 10, 5, 1, 3, 3, 2, 2, 2, 2, 0 };
        for (int index = 0; index < ids.Length; index++)
        {
            Set(agent, ids[index], points[index] * AgentSkillCatalog.UnitsPerPoint);
        }

        SkillRedistributionReport report = Apply(
            agent,
            "documented-example",
            new SkillGrant(ids[4], 4 * AgentSkillCatalog.UnitsPerPoint));

        Assert.Equal(AgentSkillCatalog.BaseCapacityUnits, report.SumAfterUnits);
        Assert.Equal(4 * AgentSkillCatalog.UnitsPerPoint, report.OverflowUnits);
        Assert.Equal(5 * AgentSkillCatalog.UnitsPerPoint,
            agent.CreateSnapshot(1).GetSkillLevel(ids[4]));
    }

    private static AgentState FullAgent()
    {
        AgentState agent = CreateAgent();
        Set(agent, AgentSkillCatalog.Woodworking, 5_000);
        Set(agent, AgentSkillCatalog.Logistics, 3_000);
        Set(agent, AgentSkillCatalog.Defense, 2_000);
        return agent;
    }

    private static AgentState CreateAgent()
    {
        return new AgentState(
            AgentTestFactory.DefaultAgentId,
            "Skill Test Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule());
    }

    private static void Set(AgentState agent, AgentSkillId id, int units)
    {
        Assert.True(agent.SetSkillLevel(id, units).IsSuccess);
    }

    private static SkillRedistributionReport Apply(
        AgentState agent,
        string sourceId,
        params SkillGrant[] grants)
    {
        return agent.ApplySkillGrant(Bundle(agent, sourceId, grants)).Value;
    }

    private static SkillGrantBundle Bundle(
        AgentState agent,
        string sourceId,
        params SkillGrant[] grants)
    {
        return new SkillGrantBundle(
            agent.Id,
            SkillGrantSourceKind.JobCompleted,
            sourceId,
            tick: 1,
            grants);
    }

    private static int Loss(SkillRedistributionReport report, AgentSkillId id)
    {
        return report.DonorLosses.Single(value => value.SkillId == id).LossUnits;
    }

    private static (string Id, int Value) ValueTuple(AgentSkillValue value)
    {
        return (value.Id.ToString(), value.Level);
    }
}

}
