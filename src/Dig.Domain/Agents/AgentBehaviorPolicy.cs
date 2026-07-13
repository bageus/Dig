using System.Collections.ObjectModel;

namespace Dig.Domain.Agents;

public sealed class AgentNeedPolicy
{
    public AgentNeedPolicy(
        NeedDelta passiveDelta,
        int criticalThreshold,
        int healthDamagePerCriticalTick,
        int healthRecoveryPerStableTick,
        int moodCriticalPenalty)
    {
        if (criticalThreshold < NeedValue.Minimum
            || criticalThreshold > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(criticalThreshold));
        }

        if (healthDamagePerCriticalTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(healthDamagePerCriticalTick));
        }

        if (healthRecoveryPerStableTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(healthRecoveryPerStableTick));
        }

        if (moodCriticalPenalty < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(moodCriticalPenalty));
        }

        PassiveDelta = passiveDelta;
        CriticalThreshold = criticalThreshold;
        HealthDamagePerCriticalTick = healthDamagePerCriticalTick;
        HealthRecoveryPerStableTick = healthRecoveryPerStableTick;
        MoodCriticalPenalty = moodCriticalPenalty;
    }

    public NeedDelta PassiveDelta { get; }

    public int CriticalThreshold { get; }

    public int HealthDamagePerCriticalTick { get; }

    public int HealthRecoveryPerStableTick { get; }

    public int MoodCriticalPenalty { get; }
}

public sealed class AgentUtilityPolicy
{
    public AgentUtilityPolicy(
        int criticalBonus,
        int hysteresisBonus,
        int decisionCooldownTicks,
        int workScheduleScore,
        int offScheduleWorkScore,
        int sleepScheduleBonus,
        int restScheduleBonus,
        int playerOrderBaseScore,
        int idleScore,
        int criticalThreatThreshold)
    {
        CriticalBonus = RequireNonNegative(criticalBonus, nameof(criticalBonus));
        HysteresisBonus = RequireNonNegative(hysteresisBonus, nameof(hysteresisBonus));
        DecisionCooldownTicks = RequireNonNegative(
            decisionCooldownTicks,
            nameof(decisionCooldownTicks));
        WorkScheduleScore = RequireNonNegative(
            workScheduleScore,
            nameof(workScheduleScore));
        OffScheduleWorkScore = RequireNonNegative(
            offScheduleWorkScore,
            nameof(offScheduleWorkScore));
        SleepScheduleBonus = RequireNonNegative(
            sleepScheduleBonus,
            nameof(sleepScheduleBonus));
        RestScheduleBonus = RequireNonNegative(
            restScheduleBonus,
            nameof(restScheduleBonus));
        PlayerOrderBaseScore = RequireNonNegative(
            playerOrderBaseScore,
            nameof(playerOrderBaseScore));
        IdleScore = RequireNonNegative(idleScore, nameof(idleScore));

        if (criticalThreatThreshold < NeedValue.Minimum
            || criticalThreatThreshold > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(criticalThreatThreshold));
        }

        CriticalThreatThreshold = criticalThreatThreshold;
    }

    public int CriticalBonus { get; }

    public int HysteresisBonus { get; }

    public int DecisionCooldownTicks { get; }

    public int WorkScheduleScore { get; }

    public int OffScheduleWorkScore { get; }

    public int SleepScheduleBonus { get; }

    public int RestScheduleBonus { get; }

    public int PlayerOrderBaseScore { get; }

    public int IdleScore { get; }

    public int CriticalThreatThreshold { get; }

    private static int RequireNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}

public readonly struct AgentActionEffect
{
    public AgentActionEffect(int durationTicks, NeedDelta needDelta)
    {
        if (durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks));
        }

        DurationTicks = durationTicks;
        NeedDelta = needDelta;
    }

    public int DurationTicks { get; }

    public NeedDelta NeedDelta { get; }
}

public sealed class AgentActionPolicy
{
    private readonly IReadOnlyDictionary<AgentIntentKind, AgentActionEffect> _effects;

    public AgentActionPolicy(
        IReadOnlyDictionary<AgentIntentKind, AgentActionEffect> effects)
    {
        if (effects is null)
        {
            throw new ArgumentNullException(nameof(effects));
        }

        Dictionary<AgentIntentKind, AgentActionEffect> copy =
            new Dictionary<AgentIntentKind, AgentActionEffect>(effects);
        foreach (AgentIntentKind kind in Enum.GetValues(typeof(AgentIntentKind)))
        {
            if (!copy.ContainsKey(kind))
            {
                throw new ArgumentException(
                    $"Action policy is missing '{kind}'.",
                    nameof(effects));
            }
        }

        _effects = new ReadOnlyDictionary<AgentIntentKind, AgentActionEffect>(copy);
    }

    public AgentActionEffect Get(AgentIntentKind kind)
    {
        return _effects[kind];
    }

    public static AgentActionPolicy CreateDefault()
    {
        return new AgentActionPolicy(
            new Dictionary<AgentIntentKind, AgentActionEffect>
            {
                [AgentIntentKind.Flee] = new AgentActionEffect(
                    2,
                    new NeedDelta(-200, -250, -50, 0)),
                [AgentIntentKind.Eat] = new AgentActionEffect(
                    2,
                    new NeedDelta(3_500, -50, 50, 0)),
                [AgentIntentKind.Sleep] = new AgentActionEffect(
                    3,
                    new NeedDelta(-100, 2_500, 200, 50)),
                [AgentIntentKind.PlayerOrder] = new AgentActionEffect(
                    2,
                    new NeedDelta(-300, -400, -50, 0)),
                [AgentIntentKind.Work] = new AgentActionEffect(
                    2,
                    new NeedDelta(-300, -500, -100, 0)),
                [AgentIntentKind.Rest] = new AgentActionEffect(
                    2,
                    new NeedDelta(-100, 300, 1_800, 25)),
                [AgentIntentKind.Idle] = new AgentActionEffect(
                    1,
                    new NeedDelta(-50, 50, 100, 25)),
            });
    }
}

public sealed class AgentBehaviorPolicy
{
    public AgentBehaviorPolicy(
        AgentNeedPolicy needs,
        AgentUtilityPolicy utility,
        AgentActionPolicy actions)
    {
        Needs = needs ?? throw new ArgumentNullException(nameof(needs));
        Utility = utility ?? throw new ArgumentNullException(nameof(utility));
        Actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    public AgentNeedPolicy Needs { get; }

    public AgentUtilityPolicy Utility { get; }

    public AgentActionPolicy Actions { get; }

    public static AgentBehaviorPolicy CreateDefault()
    {
        return new AgentBehaviorPolicy(
            new AgentNeedPolicy(
                new NeedDelta(-400, -350, -100, 0),
                criticalThreshold: 2_000,
                healthDamagePerCriticalTick: 500,
                healthRecoveryPerStableTick: 50,
                moodCriticalPenalty: 200),
            new AgentUtilityPolicy(
                criticalBonus: 10_000,
                hysteresisBonus: 700,
                decisionCooldownTicks: 2,
                workScheduleScore: 5_000,
                offScheduleWorkScore: 1_500,
                sleepScheduleBonus: 4_000,
                restScheduleBonus: 3_500,
                playerOrderBaseScore: 5_000,
                idleScore: 100,
                criticalThreatThreshold: 7_000),
            AgentActionPolicy.CreateDefault());
    }
}
