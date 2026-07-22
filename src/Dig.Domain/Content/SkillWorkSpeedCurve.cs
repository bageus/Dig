using System;
using Dig.Domain.Agents;

namespace Dig.Domain.Content
{

public sealed class SkillWorkSpeedCurve
{
    public SkillWorkSpeedCurve(
        AgentSkillId skillId,
        int minimumEfficiencyBasisPoints,
        int maximumEfficiencyBasisPoints)
    {
        if (!AgentSkillCatalog.Contains(skillId))
        {
            throw new ArgumentException("Work speed skill must use the catalog.", nameof(skillId));
        }

        if (minimumEfficiencyBasisPoints <= 0
            || minimumEfficiencyBasisPoints > 20_000
            || maximumEfficiencyBasisPoints < minimumEfficiencyBasisPoints
            || maximumEfficiencyBasisPoints > 20_000)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumEfficiencyBasisPoints));
        }

        SkillId = skillId;
        MinimumEfficiencyBasisPoints = minimumEfficiencyBasisPoints;
        MaximumEfficiencyBasisPoints = maximumEfficiencyBasisPoints;
    }

    public AgentSkillId SkillId { get; }
    public int MinimumEfficiencyBasisPoints { get; }
    public int MaximumEfficiencyBasisPoints { get; }

    public int Evaluate(int skillUnits)
    {
        if (skillUnits < 0 || skillUnits > AgentSkillCatalog.IndividualMaximumUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(skillUnits));
        }

        int range = MaximumEfficiencyBasisPoints - MinimumEfficiencyBasisPoints;
        return checked(MinimumEfficiencyBasisPoints
            + (int)((long)range * skillUnits
                / AgentSkillCatalog.IndividualMaximumUnits));
    }
}

}
