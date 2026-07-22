using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Xunit;

namespace Dig.Tests
{

public sealed class SkillProgressionContentTests
{
    [Fact]
    public void Default_profiles_are_validated_and_use_stable_unique_ids()
    {
        SkillProgressionContentCatalog catalog = DefaultSkillProgressionContent.Catalog;

        Assert.Equal(8, catalog.Profiles.Count);
        Assert.Equal(8, catalog.Profiles.Select(value => value.Id).Distinct().Count());
        Assert.All(catalog.Profiles, profile => Assert.All(
            profile.Profile.PerUnit,
            grant => Assert.True(AgentSkillCatalog.Contains(grant.SkillId))));
    }

    [Fact]
    public void Duplicate_profile_ids_are_rejected_before_runtime_catalog_creation()
    {
        SkillGrantProfileId id = new SkillGrantProfileId("skill-profile.duplicate");
        SkillProgressionContentValidationResult result =
            SkillProgressionContentCatalog.ValidateAndCreate(new[]
            {
                new SkillGrantProfileDefinition(id, new[]
                {
                    new SkillGrant(AgentSkillCatalog.Cooking, 10),
                }),
                new SkillGrantProfileDefinition(id, new[]
                {
                    new SkillGrant(AgentSkillCatalog.Alchemy, 20),
                }),
            });

        Assert.False(result.IsValid);
        Assert.Null(result.Catalog);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "content.duplicate_skill_profile");
    }
}

}
