namespace Dig.Domain.Agents;

public sealed partial class AgentDecisionSystem
{
    private static Candidate[] CreateCandidates(
        AgentSnapshot agent,
        AgentDecisionContext context,
        AgentBehaviorPolicy policy,
        long tick,
        AgentIntentKind? currentIntent)
    {
        AgentUtilityPolicy utility = policy.Utility;
        bool currentFlee = currentIntent == AgentIntentKind.Flee;
        bool fleeCritical = context.ThreatLevel >= utility.CriticalThreatThreshold;
        bool eatCritical = agent.Needs.Nutrition.IsAtOrBelow(
            policy.Needs.CriticalThreshold);
        bool sleepCritical = agent.Needs.Alertness.IsAtOrBelow(
            policy.Needs.CriticalThreshold);
        int sleepScore = agent.Needs.Alertness.Deficit;
        if (agent.ScheduledActivity == ScheduleActivity.Sleep)
        {
            sleepScore = checked(sleepScore + utility.SleepScheduleBonus);
        }

        PlayerOrder? order = agent.PlayerOrder is not null
            && agent.PlayerOrder.IsActiveAt(tick)
            ? agent.PlayerOrder
            : null;
        string? activeOrderId = order?.Id
            ?? (currentIntent == AgentIntentKind.PlayerOrder
                ? agent.ActiveAction?.PlayerOrderId
                : null);
        int orderPriority = order?.Priority ?? 0;

        int workScore = agent.ScheduledActivity == ScheduleActivity.Work
            ? utility.WorkScheduleScore
            : utility.OffScheduleWorkScore;
        workScore = checked(
            workScore
            + (agent.GetSkillLevel(GeneralWorkSkill) / 20)
            + (agent.Needs.Mood.Points / 50));

        int restScore = agent.Needs.Mood.Deficit;
        if (agent.ScheduledActivity == ScheduleActivity.Rest)
        {
            restScore = checked(restScore + utility.RestScheduleBonus);
        }

        Candidate[] candidates =
        {
            new Candidate(
                AgentIntentKind.Flee,
                context.ThreatLevel,
                context.EscapeRouteAvailable && (context.ThreatLevel > 0 || currentFlee),
                fleeCritical,
                null),
            new Candidate(
                AgentIntentKind.Eat,
                agent.Needs.Nutrition.Deficit,
                context.FoodAvailable || currentIntent == AgentIntentKind.Eat,
                eatCritical,
                null),
            new Candidate(
                AgentIntentKind.Sleep,
                sleepScore,
                context.BedAvailable || currentIntent == AgentIntentKind.Sleep,
                sleepCritical,
                null),
            new Candidate(
                AgentIntentKind.PlayerOrder,
                checked(utility.PlayerOrderBaseScore + orderPriority),
                order is not null || currentIntent == AgentIntentKind.PlayerOrder,
                critical: false,
                activeOrderId),
            new Candidate(
                AgentIntentKind.Work,
                workScore,
                context.WorkAvailable || currentIntent == AgentIntentKind.Work,
                critical: false,
                null),
            new Candidate(
                AgentIntentKind.Rest,
                restScore,
                context.RestAvailable || currentIntent == AgentIntentKind.Rest,
                critical: false,
                null),
            new Candidate(
                AgentIntentKind.Idle,
                utility.IdleScore,
                available: true,
                critical: false,
                null),
        };

        for (int index = 0; index < candidates.Length; index++)
        {
            if (candidates[index].Critical)
            {
                candidates[index].FinalScore = checked(
                    candidates[index].FinalScore + utility.CriticalBonus);
            }
        }

        return candidates;
    }

    private static void ApplyContinuityRules(
        AgentSnapshot agent,
        AgentBehaviorPolicy policy,
        long tick,
        AgentIntentKind? currentIntent,
        Candidate[] candidates)
    {
        bool cooldownActive = currentIntent.HasValue
            && agent.LastActionSwitchTick >= 0
            && tick - agent.LastActionSwitchTick < policy.Utility.DecisionCooldownTicks;

        for (int index = 0; index < candidates.Length; index++)
        {
            if (currentIntent == candidates[index].IntentKind)
            {
                candidates[index].FinalScore = checked(
                    candidates[index].FinalScore + policy.Utility.HysteresisBonus);
                candidates[index].ReceivedHysteresis = true;
            }

            bool bypassCooldown = candidates[index].Critical
                || candidates[index].IntentKind == AgentIntentKind.PlayerOrder
                || candidates[index].IntentKind == currentIntent;
            candidates[index].BlockedByCooldown = cooldownActive && !bypassCooldown;
        }
    }

    private static int GetTieBreakRank(AgentIntentKind intentKind)
    {
        return (int)intentKind;
    }

    private struct Candidate
    {
        public Candidate(
            AgentIntentKind intentKind,
            int baseScore,
            bool available,
            bool critical,
            string? playerOrderId)
        {
            IntentKind = intentKind;
            BaseScore = baseScore;
            FinalScore = baseScore;
            Available = available;
            Critical = critical;
            PlayerOrderId = playerOrderId;
            ReceivedHysteresis = false;
            BlockedByCooldown = false;
        }

        public AgentIntentKind IntentKind;
        public int BaseScore;
        public int FinalScore;
        public bool Available;
        public bool Critical;
        public string? PlayerOrderId;
        public bool ReceivedHysteresis;
        public bool BlockedByCooldown;
    }

    private sealed class CandidateIntentComparer : IComparer<Candidate>
    {
        public static readonly CandidateIntentComparer Instance =
            new CandidateIntentComparer();

        private CandidateIntentComparer()
        {
        }

        public int Compare(Candidate left, Candidate right)
        {
            return GetTieBreakRank(left.IntentKind).CompareTo(
                GetTieBreakRank(right.IntentKind));
        }
    }
}
