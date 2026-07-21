using System;
using System.Collections.Generic;
using Dig.Domain.Agents;

namespace Dig.Domain.Content
{

public static class DefaultSkillGrantProfileIds
{
    public static readonly SkillGrantProfileId StoneExtraction =
        new SkillGrantProfileId("skill-profile.work.stone-extraction");
    public static readonly SkillGrantProfileId MushroomHarvest =
        new SkillGrantProfileId("skill-profile.work.mushroom-harvest");
    public static readonly SkillGrantProfileId Cooking =
        new SkillGrantProfileId("skill-profile.work.cooking");
    public static readonly SkillGrantProfileId Logistics =
        new SkillGrantProfileId("skill-profile.work.logistics");
    public static readonly SkillGrantProfileId Metallurgy =
        new SkillGrantProfileId("skill-profile.work.metallurgy");
    public static readonly SkillGrantProfileId Alchemy =
        new SkillGrantProfileId("skill-profile.work.alchemy");
    public static readonly SkillGrantProfileId Service =
        new SkillGrantProfileId("skill-profile.work.service");
    public static readonly SkillGrantProfileId Construction =
        new SkillGrantProfileId("skill-profile.work.construction");
}

public static class DefaultSkillProgressionContent
{
    private static readonly SkillProgressionContentCatalog RuntimeCatalog = Build();

    public static SkillProgressionContentCatalog Catalog => RuntimeCatalog;

    private static SkillProgressionContentCatalog Build()
    {
        SkillProgressionContentValidationResult result =
            SkillProgressionContentCatalog.ValidateAndCreate(CreateDefinitions());
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                "Default skill progression content failed validation.");
        }

        return result.Catalog!;
    }

    private static IEnumerable<SkillGrantProfileDefinition> CreateDefinitions()
    {
        yield return Single(
            DefaultSkillGrantProfileIds.StoneExtraction,
            AgentSkillCatalog.Stonework,
            AgentSkillCatalog.UnitsPerPoint);
        yield return Single(
            DefaultSkillGrantProfileIds.MushroomHarvest,
            AgentSkillCatalog.Woodworking,
            AgentSkillCatalog.UnitsPerPoint);
        yield return Single(
            DefaultSkillGrantProfileIds.Cooking,
            AgentSkillCatalog.Cooking,
            AgentSkillCatalog.UnitsPerPoint);
        yield return Single(
            DefaultSkillGrantProfileIds.Logistics,
            AgentSkillCatalog.Logistics,
            AgentSkillCatalog.UnitsPerPoint / 2);
        yield return WithLogistics(
            DefaultSkillGrantProfileIds.Metallurgy,
            AgentSkillCatalog.Metallurgy);
        yield return WithLogistics(
            DefaultSkillGrantProfileIds.Alchemy,
            AgentSkillCatalog.Alchemy);
        yield return Single(
            DefaultSkillGrantProfileIds.Service,
            AgentSkillCatalog.Service,
            AgentSkillCatalog.UnitsPerPoint);
        yield return new SkillGrantProfileDefinition(
            DefaultSkillGrantProfileIds.Construction,
            new[]
            {
                new SkillGrant(
                    AgentSkillCatalog.Stonework,
                    AgentSkillCatalog.UnitsPerPoint / 2),
                new SkillGrant(
                    AgentSkillCatalog.Woodworking,
                    AgentSkillCatalog.UnitsPerPoint / 2),
                new SkillGrant(
                    AgentSkillCatalog.Logistics,
                    AgentSkillCatalog.UnitsPerPoint / 4),
            });
    }

    private static SkillGrantProfileDefinition Single(
        SkillGrantProfileId id,
        AgentSkillId skillId,
        int units)
    {
        return new SkillGrantProfileDefinition(id, new[] { new SkillGrant(skillId, units) });
    }

    private static SkillGrantProfileDefinition WithLogistics(
        SkillGrantProfileId id,
        AgentSkillId primary)
    {
        return new SkillGrantProfileDefinition(id, new[]
        {
            new SkillGrant(primary, AgentSkillCatalog.UnitsPerPoint),
            new SkillGrant(
                AgentSkillCatalog.Logistics,
                AgentSkillCatalog.UnitsPerPoint / 4),
        });
    }
}

}
