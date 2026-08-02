using System;
using Dig.Domain.Agents;

namespace Dig.Domain.Combat
{

public sealed class CombatSkillProfile
{
    public CombatSkillProfile(AgentSkillId skillId, int hitGrantUnits)
    {
        if (skillId != AgentSkillCatalog.OneHandedCombat
            && skillId != AgentSkillCatalog.TwoHandedCombat
            && skillId != AgentSkillCatalog.RangedCombat
            && skillId != AgentSkillCatalog.UnarmedCombat)
        {
            throw new ArgumentException(
                "A weapon profile requires an offensive combat skill.",
                nameof(skillId));
        }

        if (hitGrantUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hitGrantUnits));
        }

        SkillId = skillId;
        HitGrantUnits = hitGrantUnits;
    }

    public AgentSkillId SkillId { get; }
    public int HitGrantUnits { get; }
}

public sealed class CombatDefenseSkillProfile
{
    public CombatDefenseSkillProfile(string profileId, int defenseGrantUnits)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException(
                "Defense skill profile id is required.",
                nameof(profileId));
        }

        if (defenseGrantUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defenseGrantUnits));
        }

        ProfileId = profileId.Trim();
        DefenseGrantUnits = defenseGrantUnits;
    }

    public string ProfileId { get; }
    public int DefenseGrantUnits { get; }
}

public sealed class ShieldSkillProfile
{
    public ShieldSkillProfile(string profileId, int defenseGrantUnits)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException(
                "Shield skill profile id is required.",
                nameof(profileId));
        }

        if (defenseGrantUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defenseGrantUnits));
        }

        ProfileId = profileId.Trim();
        DefenseGrantUnits = defenseGrantUnits;
    }

    public string ProfileId { get; }
    public int DefenseGrantUnits { get; }
}

}
