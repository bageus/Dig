using System.Linq;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentSkillSetViewModelTests
{
    [Fact]
    public void Top_five_excludes_zero_skills_and_can_contain_fewer_than_five()
    {
        ResidentSkillSetViewModel skills = new ResidentSkillSetViewModel(new[]
        {
            new ResidentSkillViewModel("skill.stonework", 2500),
            new ResidentSkillViewModel("skill.logistics", 1000),
            new ResidentSkillViewModel("skill.cooking", 0),
            new ResidentSkillViewModel("skill.defense", 0),
            new ResidentSkillViewModel("skill.service", 0),
            new ResidentSkillViewModel("skill.woodworking", 0),
        });

        Assert.Equal(new[]
        {
            "skill.stonework",
            "skill.logistics",
        }, skills.TopFive.Select(item => item.SkillId).ToArray());
        Assert.DoesNotContain(skills.TopFive, item => item.Level == 0);
    }

    [Fact]
    public void Top_five_is_empty_when_all_skills_are_zero()
    {
        ResidentSkillSetViewModel skills = new ResidentSkillSetViewModel(new[]
        {
            new ResidentSkillViewModel("skill.stonework", 0),
            new ResidentSkillViewModel("skill.logistics", 0),
        });

        Assert.Empty(skills.TopFive);
    }
}

}
