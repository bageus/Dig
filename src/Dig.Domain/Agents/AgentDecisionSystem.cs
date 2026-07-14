namespace Dig.Domain.Agents;

public sealed class AgentDecisionSystem
{
    private static readonly AgentSkillId GeneralWorkSkill =
        new AgentSkillId("general.work");

    public AgentDecision Decide(
        AgentSnapshot agent,
        AgentDecisionContext context,
        AgentBehaviorPolicy policy,
        long tick)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (tick < agent.LastActionSwitchTick)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tick),
                "Decision tick cannot precede the active action switch tick.");
        }

        if (!agent.IsAlive)
        {
            return CreateDeadDecision(tick);
        }

        AgentIntentKind? currentIntent = agent.ActiveAction?.IntentKind;
        Candidate[] candidates = CreateCandidates(
            agent,
            context,
            policy,
            tick,
            currentIntent);
        ApplyContinuityRules(agent, policy, tick, currentIntent, candidates);
        Array.Sort(candidates, CandidateIntentComparer.Instance);

        int selectedIndex = SelectCandidate(candidates);
        Candidate selected = candidates[selectedIndex];
        string selectedReason = GetSelectedReason(selected, currentIntent);
        UtilityOptionDiagnostic[] diagnostics = CreateDiagnostics(
            candidates,
            selectedIndex,
            selectedReason);
        string? orderId = selected.IntentKind == AgentIntentKind.PlayerOrder
            ? selected.PlayerOrderId
            : null;

        return AgentDecision.CreateOwned(
            tick,
            selected.IntentKind,
            orderId,
            selected.FinalScore,
            selected.Critical,
            selectedReason,
            diagnostics);
    }

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

    private static int SelectCandidate(Candidate[] candidates)
    {
        int selectedIndex = -1;
        for (int index = 0; index < candidates.Length; index++)
        {
            Candidate candidate = candidates[index];
            if (!candidate.Available || candidate.BlockedByCooldown)
            {
                continue;
            }

            if (selectedIndex < 0
                || candidate.FinalScore > candidates[selectedIndex].FinalScore)
            {
                selectedIndex = index;
            }
        }

        if (selectedIndex < 0)
        {
            throw new InvalidOperationException("Utility AI has no eligible candidate.");
        }

        return selectedIndex;
    }

    private static UtilityOptionDiagnostic[] CreateDiagnostics(
        Candidate[] candidates,
        int selectedIndex,
        string selectedReason)
    {
        UtilityOptionDiagnostic[] diagnostics =
            new UtilityOptionDiagnostic[candidates.Length];
        for (int index = 0; index < candidates.Length; index++)
        {
            diagnostics[index] = CreateDiagnostic(
                candidates[index],
                index == selectedIndex,
                selectedReason);
        }

        return diagnostics;
    }

    private static UtilityOptionDiagnostic CreateDiagnostic(
        Candidate candidate,
        bool selected,
        string selectedReason)
    {
        string reason;
        string detail;
        if (selected)
        {
            reason = selectedReason;
            detail = "Selected as the highest eligible deterministic utility option.";
        }
        else if (!candidate.Available)
        {
            reason = "rejected.unavailable";
            detail = "Required world capability or resource is unavailable.";
        }
        else if (candidate.BlockedByCooldown)
        {
            reason = "rejected.cooldown";
            detail = "A recent action switch blocks non-critical oscillation.";
        }
        else
        {
            reason = "rejected.lower_utility";
            detail = "Another eligible option has higher utility or tie-break priority.";
        }

        return new UtilityOptionDiagnostic(
            candidate.IntentKind,
            candidate.BaseScore,
            candidate.FinalScore,
            candidate.Available,
            candidate.Critical,
            selected,
            reason,
            detail);
    }

    private static string GetSelectedReason(
        Candidate selected,
        AgentIntentKind? currentIntent)
    {
        if (selected.Critical)
        {
            return "selected.critical_survival";
        }

        if (selected.IntentKind == AgentIntentKind.PlayerOrder)
        {
            return "selected.player_override";
        }

        if (selected.ReceivedHysteresis && selected.IntentKind == currentIntent)
        {
            return "selected.hysteresis";
        }

        return "selected.utility";
    }

    private static AgentDecision CreateDeadDecision(long tick)
    {
        UtilityOptionDiagnostic option = new UtilityOptionDiagnostic(
            AgentIntentKind.Idle,
            0,
            0,
            available: true,
            critical: false,
            selected: true,
            "selected.agent_dead",
            "Dead agents do not select executable intentions.");
        return new AgentDecision(
            tick,
            AgentIntentKind.Idle,
            null,
            0,
            critical: false,
            "selected.agent_dead",
            "Agent is dead and remains idle.",
            new[] { option });
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
