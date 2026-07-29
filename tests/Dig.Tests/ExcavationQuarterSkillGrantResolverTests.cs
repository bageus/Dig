using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationQuarterSkillGrantResolverTests
{
    private readonly ExcavationQuarterSkillGrantResolver _resolver =
        new ExcavationQuarterSkillGrantResolver();

    [Fact]
    public void Four_quarters_sum_to_full_profile_exactly()
    {
        SkillGrantProfile profile = new SkillGrantProfile(new[]
        {
            new SkillGrant(AgentSkillCatalog.Stonework, 101),
            new SkillGrant(AgentSkillCatalog.Logistics, 25),
        });
        ExcavationQuarter[] quarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };

        int stone = quarters.Sum(quarter => _resolver.Resolve(profile, quarter)
            .Single(value => value.SkillId == AgentSkillCatalog.Stonework)
            .RequestedUnits);
        int logistics = quarters.Sum(quarter => _resolver.Resolve(profile, quarter)
            .Single(value => value.SkillId == AgentSkillCatalog.Logistics)
            .RequestedUnits);

        Assert.Equal(101, stone);
        Assert.Equal(25, logistics);
    }

    [Fact]
    public void Stable_quarter_order_receives_integer_remainder()
    {
        SkillGrantProfile profile = SkillGrantProfile.Single(
            AgentSkillCatalog.Stonework,
            units: 5);

        Assert.Equal(2, Units(profile, ExcavationQuarter.UpperLeft));
        Assert.Equal(1, Units(profile, ExcavationQuarter.LowerLeft));
        Assert.Equal(1, Units(profile, ExcavationQuarter.UpperRight));
        Assert.Equal(1, Units(profile, ExcavationQuarter.LowerRight));
    }

    [Fact]
    public void Profile_entries_smaller_than_four_only_grant_on_earliest_quarters()
    {
        SkillGrantProfile profile = SkillGrantProfile.Single(
            AgentSkillCatalog.Stonework,
            units: 2);

        Assert.Equal(1, Units(profile, ExcavationQuarter.UpperLeft));
        Assert.Equal(1, Units(profile, ExcavationQuarter.LowerLeft));
        Assert.Empty(_resolver.Resolve(profile, ExcavationQuarter.UpperRight));
        Assert.Empty(_resolver.Resolve(profile, ExcavationQuarter.LowerRight));
    }

    private int Units(SkillGrantProfile profile, ExcavationQuarter quarter)
    {
        return _resolver.Resolve(profile, quarter).Single().RequestedUnits;
    }
}

}
