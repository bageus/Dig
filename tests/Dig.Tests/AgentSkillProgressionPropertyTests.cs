using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentSkillProgressionPropertyTests
{
    [Fact]
    public void Random_bundles_preserve_capacity_bounds_and_recipient_monotonicity()
    {
        Random random = new Random(10_307);
        AgentSkillId[] ids = AgentSkillCatalog.All.Select(value => value.Id).ToArray();
        for (int sample = 0; sample < 512; sample++)
        {
            AgentState agent = CreateEmptyAgent(sample);
            SeedValues(agent, ids, random);
            AgentSkillProgressionSnapshot before = agent.CreateSkillProgressionSnapshot();
            SkillGrant[] grants = CreateGrants(ids, random);
            SkillGrantBundle bundle = new SkillGrantBundle(
                agent.Id,
                SkillGrantSourceKind.TrainingCompleted,
                "property-" + sample,
                tick: sample,
                grants);

            SkillRedistributionReport report = agent.ApplySkillGrant(bundle).Value;
            AgentSkillProgressionSnapshot after = agent.CreateSkillProgressionSnapshot();

            Assert.InRange(after.UsedCapacityUnits, 0, after.TotalCapacityUnits);
            Assert.All(after.Values, value => Assert.InRange(
                value.Level,
                0,
                AgentSkillCatalog.IndividualMaximumUnits));
            foreach (SkillGrant grant in grants)
            {
                Assert.True(after.GetLevel(grant.SkillId) >= before.GetLevel(grant.SkillId));
            }

            Assert.Equal(after.UsedCapacityUnits, report.SumAfterUnits);
            Assert.Equal(report.OverflowUnits, report.DonorLosses.Sum(value => value.LossUnits));
            Assert.Equal(
                report.FreeCapacityGainUnits + report.OverflowUnits,
                report.Grants.Sum(value => value.AppliedUnits));
            long version = agent.Version;
            Assert.True(agent.ApplySkillGrant(bundle).Value.WasAlreadyApplied);
            Assert.Equal(version, agent.Version);
        }
    }

    [Fact]
    public void Sixty_four_residents_apply_repeated_grants_within_regression_budget()
    {
        AgentState[] agents = Enumerable.Range(0, 64)
            .Select(CreateEmptyAgent)
            .ToArray();
        Stopwatch elapsed = Stopwatch.StartNew();
        int applied = 0;
        for (int round = 0; round < 64; round++)
        {
            for (int index = 0; index < agents.Length; index++)
            {
                SkillRedistributionReport report = agents[index].ApplySkillGrant(
                    new SkillGrantBundle(
                        agents[index].Id,
                        SkillGrantSourceKind.TrainingCompleted,
                        $"load-{round}-{index}",
                        round,
                        new[]
                        {
                            new SkillGrant(AgentSkillCatalog.Logistics, 10),
                            new SkillGrant(AgentSkillCatalog.Stonework, 5),
                        })).Value;
                applied += report.WasAlreadyApplied ? 0 : 1;
            }
        }

        elapsed.Stop();
        Assert.Equal(4_096, applied);
        Assert.True(
            elapsed.Elapsed < TimeSpan.FromSeconds(30),
            $"Skill progression load regression: {elapsed.Elapsed}.");
    }

    [Fact]
    public void Zero_donor_pool_rejects_growth_without_lowering_recipients()
    {
        AgentState agent = CreateEmptyAgent(700);
        Assert.True(agent.SetSkillLevel(AgentSkillCatalog.Stonework, 5_000).IsSuccess);
        Assert.True(agent.SetSkillLevel(AgentSkillCatalog.Cooking, 5_000).IsSuccess);

        SkillRedistributionReport report = agent.ApplySkillGrant(
            new SkillGrantBundle(
                agent.Id,
                SkillGrantSourceKind.TrainingCompleted,
                "zero-donor-pool",
                tick: 1,
                new[]
                {
                    new SkillGrant(AgentSkillCatalog.Stonework, 100),
                    new SkillGrant(AgentSkillCatalog.Cooking, 100),
                })).Value;

        Assert.Equal(0, report.Grants.Sum(value => value.AppliedUnits));
        Assert.Empty(report.DonorLosses);
        Assert.Equal(10_000, report.SumAfterUnits);
    }

    [Fact]
    public void Equal_fractional_remainders_use_stable_skill_id_tie_break()
    {
        AgentState agent = CreateEmptyAgent(701);
        Assert.True(agent.SetSkillLevel(AgentSkillCatalog.Stonework, 9_998).IsSuccess);
        Assert.True(agent.SetSkillLevel(AgentSkillCatalog.Cooking, 1).IsSuccess);
        Assert.True(agent.SetSkillLevel(AgentSkillCatalog.Defense, 1).IsSuccess);

        SkillRedistributionReport report = agent.ApplySkillGrant(
            new SkillGrantBundle(
                agent.Id,
                SkillGrantSourceKind.TrainingCompleted,
                "stable-remainder",
                tick: 1,
                new[] { new SkillGrant(AgentSkillCatalog.Stonework, 1) })).Value;

        SkillDonorLoss rounded = report.DonorLosses.Single(
            value => value.ReceivedRoundingUnit);
        Assert.Equal(AgentSkillCatalog.Cooking, rounded.SkillId);
        Assert.Equal(1, rounded.LossUnits);
        Assert.Equal(0, report.DonorLosses.Single(
            value => value.SkillId == AgentSkillCatalog.Defense).LossUnits);
    }

    private static AgentState CreateEmptyAgent(int index)
    {
        return new AgentState(
            EntityId.Parse((index + 1).ToString("x32")),
            "Property Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule());
    }

    private static void SeedValues(
        AgentState agent,
        IReadOnlyList<AgentSkillId> ids,
        Random random)
    {
        int remaining = AgentSkillCatalog.BaseCapacityUnits;
        for (int index = 0; index < ids.Count && remaining > 0; index++)
        {
            int value = random.Next(0, Math.Min(remaining, 2_000) + 1);
            Assert.True(agent.SetSkillLevel(ids[index], value).IsSuccess);
            remaining -= value;
        }
    }

    private static SkillGrant[] CreateGrants(
        IReadOnlyList<AgentSkillId> ids,
        Random random)
    {
        return ids
            .OrderBy(_ => random.Next())
            .Take(random.Next(1, 4))
            .Select(id => new SkillGrant(id, random.Next(1, 801)))
            .ToArray();
    }
}

}
