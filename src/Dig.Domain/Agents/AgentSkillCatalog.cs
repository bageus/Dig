using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.Agents
{

public enum AgentSkillCategory
{
    Work = 0,
    Combat = 1,
}

public sealed class AgentSkillDefinition
{
    public AgentSkillDefinition(
        AgentSkillId id,
        string localizationKey,
        AgentSkillCategory category)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Skill id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(localizationKey))
        {
            throw new ArgumentException(
                "Skill localization key is required.",
                nameof(localizationKey));
        }

        Id = id;
        LocalizationKey = localizationKey.Trim();
        Category = category;
    }

    public AgentSkillId Id { get; }
    public string LocalizationKey { get; }
    public AgentSkillCategory Category { get; }
}

public static class AgentSkillCatalog
{
    public const int SchemaVersion = 1;
    public const int PrecisionVersion = 1;
    public const int UnitsPerPoint = 100;
    public const int IndividualMaximumUnits = 100 * UnitsPerPoint;
    public const int BaseCapacityUnits = 100 * UnitsPerPoint;
    public const int UniversityCapacityUnits = 200 * UnitsPerPoint;

    public static readonly AgentSkillId Stonework = new AgentSkillId("skill.stonework");
    public static readonly AgentSkillId Woodworking = new AgentSkillId("skill.woodworking");
    public static readonly AgentSkillId Cooking = new AgentSkillId("skill.cooking");
    public static readonly AgentSkillId Logistics = new AgentSkillId("skill.logistics");
    public static readonly AgentSkillId Metallurgy = new AgentSkillId("skill.metallurgy");
    public static readonly AgentSkillId Alchemy = new AgentSkillId("skill.alchemy");
    public static readonly AgentSkillId Service = new AgentSkillId("skill.service");
    public static readonly AgentSkillId Defense = new AgentSkillId("skill.defense");
    public static readonly AgentSkillId RangedCombat =
        new AgentSkillId("skill.ranged_combat");
    public static readonly AgentSkillId UnarmedCombat =
        new AgentSkillId("skill.unarmed_combat");
    public static readonly AgentSkillId TwoHandedCombat =
        new AgentSkillId("skill.two_handed_combat");
    public static readonly AgentSkillId OneHandedCombat =
        new AgentSkillId("skill.one_handed_combat");

    private static readonly IReadOnlyList<AgentSkillDefinition> Catalog =
        new ReadOnlyCollection<AgentSkillDefinition>(new[]
        {
            Work(Stonework, "resident.skill.stonework"),
            Work(Woodworking, "resident.skill.woodworking"),
            Work(Cooking, "resident.skill.cooking"),
            Work(Logistics, "resident.skill.logistics"),
            Work(Metallurgy, "resident.skill.metallurgy"),
            Work(Alchemy, "resident.skill.alchemy"),
            Work(Service, "resident.skill.service"),
            Combat(Defense, "resident.skill.defense"),
            Combat(RangedCombat, "resident.skill.ranged_combat"),
            Combat(UnarmedCombat, "resident.skill.unarmed_combat"),
            Combat(TwoHandedCombat, "resident.skill.two_handed_combat"),
            Combat(OneHandedCombat, "resident.skill.one_handed_combat"),
        }.OrderBy(value => value.Id).ToArray());

    private static readonly HashSet<AgentSkillId> Ids =
        new HashSet<AgentSkillId>(Catalog.Select(value => value.Id));

    static AgentSkillCatalog()
    {
        if (Catalog.Count != 12 || Ids.Count != Catalog.Count)
        {
            throw new InvalidOperationException(
                "The authoritative resident skill catalog must contain 12 unique ids.");
        }
    }

    public static IReadOnlyList<AgentSkillDefinition> All => Catalog;

    public static bool Contains(AgentSkillId id)
    {
        return Ids.Contains(id);
    }

    public static int StoneworkThresholdUnits(int tier)
    {
        return tier switch
        {
            1 => 20 * UnitsPerPoint,
            2 => 40 * UnitsPerPoint,
            3 => 60 * UnitsPerPoint,
            _ => throw new ArgumentOutOfRangeException(nameof(tier)),
        };
    }

    private static AgentSkillDefinition Work(AgentSkillId id, string key)
    {
        return new AgentSkillDefinition(id, key, AgentSkillCategory.Work);
    }

    private static AgentSkillDefinition Combat(AgentSkillId id, string key)
    {
        return new AgentSkillDefinition(id, key, AgentSkillCategory.Combat);
    }
}

}
