using System;
using Dig.Domain.Agents;

namespace Dig.Domain.Combat
{

public sealed class CombatSkillScalingPolicy
{
    public const int BasisPoints = 10_000;

    public CombatSkillScalingPolicy(
        int accuracyBasisPointsPerSkillPoint,
        int maximumAccuracyBonus,
        int damageBasisPointsPerSkillPoint,
        int maximumDamageBonus,
        int defenseReductionPerSkillPoint,
        int maximumDefenseReduction)
    {
        ValidateCoefficient(
            accuracyBasisPointsPerSkillPoint,
            nameof(accuracyBasisPointsPerSkillPoint));
        ValidateReduction(maximumAccuracyBonus, nameof(maximumAccuracyBonus));
        ValidateCoefficient(
            damageBasisPointsPerSkillPoint,
            nameof(damageBasisPointsPerSkillPoint));
        ValidateReduction(maximumDamageBonus, nameof(maximumDamageBonus));
        ValidateCoefficient(
            defenseReductionPerSkillPoint,
            nameof(defenseReductionPerSkillPoint));
        ValidateReduction(maximumDefenseReduction, nameof(maximumDefenseReduction));

        AccuracyBasisPointsPerSkillPoint = accuracyBasisPointsPerSkillPoint;
        MaximumAccuracyBonus = maximumAccuracyBonus;
        DamageBasisPointsPerSkillPoint = damageBasisPointsPerSkillPoint;
        MaximumDamageBonus = maximumDamageBonus;
        DefenseReductionPerSkillPoint = defenseReductionPerSkillPoint;
        MaximumDefenseReduction = maximumDefenseReduction;
    }

    public int AccuracyBasisPointsPerSkillPoint { get; }
    public int MaximumAccuracyBonus { get; }
    public int DamageBasisPointsPerSkillPoint { get; }
    public int MaximumDamageBonus { get; }
    public int DefenseReductionPerSkillPoint { get; }
    public int MaximumDefenseReduction { get; }

    public static CombatSkillScalingPolicy CreateCaveEncounter()
    {
        return new CombatSkillScalingPolicy(
            accuracyBasisPointsPerSkillPoint: 25,
            maximumAccuracyBonus: 2_500,
            damageBasisPointsPerSkillPoint: 40,
            maximumDamageBonus: 4_000,
            defenseReductionPerSkillPoint: 30,
            maximumDefenseReduction: 3_000);
    }

    public int ResolveAccuracyModifier(int skillUnits)
    {
        ValidateSkillUnits(skillUnits);
        return Math.Min(
            MaximumAccuracyBonus,
            checked(skillUnits * AccuracyBasisPointsPerSkillPoint
                / AgentSkillCatalog.UnitsPerPoint));
    }

    public int ResolveDamageMultiplier(int skillUnits)
    {
        ValidateSkillUnits(skillUnits);
        int bonus = Math.Min(
            MaximumDamageBonus,
            checked(skillUnits * DamageBasisPointsPerSkillPoint
                / AgentSkillCatalog.UnitsPerPoint));
        return checked(BasisPoints + bonus);
    }

    public int ResolveDefenseReduction(int defenseSkillUnits)
    {
        ValidateSkillUnits(defenseSkillUnits);
        return Math.Min(
            MaximumDefenseReduction,
            checked(defenseSkillUnits * DefenseReductionPerSkillPoint
                / AgentSkillCatalog.UnitsPerPoint));
    }

    public static void ValidateDamageMultiplier(int value, string parameterName)
    {
        if (value < 0 || value > BasisPoints * 2)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ValidateReduction(int value, string parameterName)
    {
        if (value < 0 || value > BasisPoints)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateCoefficient(int value, string parameterName)
    {
        if (value < 0 || value > BasisPoints)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateSkillUnits(int value)
    {
        if (value < 0 || value > AgentSkillCatalog.IndividualMaximumUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}

}
