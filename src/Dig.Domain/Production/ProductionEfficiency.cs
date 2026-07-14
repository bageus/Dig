namespace Dig.Domain.Production;

public readonly struct ProductionWorkContext
{
    public ProductionWorkContext(
        int skillEfficiencyBasisPoints,
        int conditionEfficiencyBasisPoints)
    {
        if (skillEfficiencyBasisPoints <= 0 || skillEfficiencyBasisPoints > 20_000)
        {
            throw new ArgumentOutOfRangeException(nameof(skillEfficiencyBasisPoints));
        }

        if (conditionEfficiencyBasisPoints <= 0 || conditionEfficiencyBasisPoints > 20_000)
        {
            throw new ArgumentOutOfRangeException(nameof(conditionEfficiencyBasisPoints));
        }

        SkillEfficiencyBasisPoints = skillEfficiencyBasisPoints;
        ConditionEfficiencyBasisPoints = conditionEfficiencyBasisPoints;
    }

    public int SkillEfficiencyBasisPoints { get; }

    public int ConditionEfficiencyBasisPoints { get; }

    public static ProductionWorkContext Normal =>
        new ProductionWorkContext(10_000, 10_000);
}

public static class ProductionEfficiency
{
    public static int CalculateEffectiveWork(
        int baseWork,
        ProductionWorkContext context)
    {
        if (baseWork <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseWork));
        }

        long skillAdjusted = checked(
            (long)baseWork * context.SkillEfficiencyBasisPoints / 10_000L);
        long conditionAdjusted = checked(
            skillAdjusted * context.ConditionEfficiencyBasisPoints / 10_000L);
        return Math.Max(1, checked((int)conditionAdjusted));
    }
}
