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
        List<Candidate> candidates = CreateCandidates(
            agent,
            context,
            policy,
            tick,
            currentIntent);
        ApplyContinuityRules(agent, policy, tick, currentIntent, candidates);

        Candidate selected = candidates
            .Where(candidate => candidate.Available && !candidate.BlockedByCooldown)
            .OrderByDescending(candidate => candidate.FinalScore)
            .ThenBy(candidate => GetTieBreakRank(candidate.IntentKind))
            .First();

        string selectedReason = GetSelectedReason(selected, currentIntent);
        UtilityOptionDiagnostic[] diagnostics = candidates
            .OrderBy(candidate => GetTieBreakRank(candidate.IntentKind))
            .Select(candidate => CreateDiagnostic(candidate, selected, selectedReason))
            .ToArray();
        string? orderId = selected.IntentKind == AgentIntentKind.PlayerOrder
            ? selected.PlayerOrderId
            : null;

        return new AgentDecision(
            tick,
            selected.IntentKind,
            orderId,
            selected.FinalScore,
            selected.Critical,
            selectedReason,
            BuildExplanation(selected, selectedReason),
            diagnostics);
    }

    private static List<Candidate> CreateCandidates(
        AgentSnapshot agent,
        AgentDecisionContext context,
        AgentBehaviorPolicy policy,
        long tick,
        AgentIntentKind? currentIntent)
    {
        AgentUtilityPolicy utility = policy.Utility;
        bool currentFlee = currentIntent == AgentIntentKind.Flee;
        bool fleeCritical = context.ThreatLevel >= utility.CriticalThreatThreshold;
        Candidate flee = new Candidate(
            AgentIntentKind.Flee,
            context.ThreatLevel,
            context.EscapeRouteAvailable && (context.ThreatLevel > 0 || currentFlee),
            fleeCritical,
            null);

        bool eatCritical = agent.Needs.Nutrition.IsAtOrBelow(
            policy.Needs.CriticalThreshold);
        Candidate eat = new Candidate(
            AgentIntentKind.Eat,
            agent.Needs.Nutrition.Deficit,
            context.FoodAvailable || currentIntent == AgentIntentKind.Eat,
            eatCritical,
            null);

        bool sleepCritical = agent.Needs.Alertness.IsAtOrBelow(
            policy.Needs.CriticalThreshold);
        int sleepScore = agent.Needs.Alertness.Deficit;
        if (agent.ScheduledActivity == ScheduleActivity.Sleep)
        {
            sleepScore = checked(sleepScore + utility.SleepScheduleBonus);
        }

        Candidate sleep = new Candidate(
            AgentIntentKind.Sleep,
            sleepScore,
            context.BedAvailable || currentIntent == AgentIntentKind.Sleep,
            sleepCritical,
            null);

        PlayerOrder? order = agent.PlayerOrder is not null
            && agent.PlayerOrder.IsActiveAt(tick)
            ? agent.PlayerOrder
            : null;
        string? activeOrderId = order?.Id
            ?? (currentIntent == AgentIntentKind.PlayerOrder
                ? agent.ActiveAction?.PlayerOrderId
                : null);
        int orderPriority = order?.Priority ?? 0;
        Candidate playerOrder = new Candidate(
            AgentIntentKind.PlayerOrder,
            checked(utility.PlayerOrderBaseScore + orderPriority),
            order is not null || currentIntent == AgentIntentKind.PlayerOrder,
            critical: false,
            activeOrderId);

        int workScore = agent.ScheduledActivity == ScheduleActivity.Work
            ? utility.WorkScheduleScore
            : utility.OffScheduleWorkScore;
        workScore = checked(
            workScore
            + (agent.GetSkillLevel(GeneralWorkSkill) / 20)
            + (agent.Needs.Mood.Points / 50));
        Candidate work = new Candidate(
            AgentIntentKind.Work,
            workScore,
            context.WorkAvailable || currentIntent == AgentIntentKind.Work,
            critical: false,
            null);

        int restScore = agent.Needs.Mood.Deficit;
        if (agent.ScheduledActivity == ScheduleActivity.Rest)
        {
            restScore = checked(restScore + utility.RestScheduleBonus);
        }

        Candidate rest = new Candidate(
            AgentIntentKind.Rest,
            restScore,
            context.RestAvailable || currentIntent == AgentIntentKind.Rest,
            critical: false,
            null);
        Candidate idle = new Candidate(
            AgentIntentKind.Idle,
            utility.IdleScore,
            available: true,
            critical: false,
            null);

        Candidate[] candidates = { flee, eat, sleep, playerOrder, work, rest, idle };
        foreach (Candidate candidate in candidates)
        {
            if (candidate.Critical)
            {
                candidate.FinalScore = checked(
                    candidate.FinalScore + utility.CriticalBonus);
            }
        }

        return candidates.ToList();
    }

    private static void ApplyContinuityRules(
        AgentSnapshot agent,
        AgentBehaviorPolicy policy,
        long tick,
        AgentIntentKind? currentIntent,
        IEnumerable<Candidate> candidates)
    {
        bool cooldownActive = currentIntent.HasValue
            && agent.LastActionSwitchTick >= 0
            && tick - agent.LastActionSwitchTick < policy.Utility.DecisionCooldownTicks;

        foreach (Candidate candidate in candidates)
        {
            if (currentIntent == candidate.IntentKind)
            {
                candidate.FinalScore = checked(
                    candidate.FinalScore + policy.Utility.HysteresisBonus);
                candidate.ReceivedHysteresis = true;
            }

            bool bypassCooldown = candidate.Critical
                || candidate.IntentKind == AgentIntentKind.PlayerOrder
                || candidate.IntentKind == currentIntent;
            candidate.BlockedByCooldown = cooldownActive && !bypassCooldown;
        }
    }

    private static UtilityOptionDiagnostic CreateDiagnostic(
        Candidate candidate,
        Candidate selected,
        string selectedReason)
    {
        bool isSelected = ReferenceEquals(candidate, selected);
        string reason;
        string detail;
        if (isSelected)
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
            isSelected,
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

    private static string BuildExplanation(Candidate selected, string reasonCode)
    {
        return $"{selected.IntentKind} selected with score {selected.FinalScore} ({reasonCode}).";
    }

    private static int GetTieBreakRank(AgentIntentKind intentKind)
    {
        return (int)intentKind;
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

    private sealed class Candidate
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
        }

        public AgentIntentKind IntentKind { get; }

        public int BaseScore { get; }

        public int FinalScore { get; set; }

        public bool Available { get; }

        public bool Critical { get; }

        public string? PlayerOrderId { get; }

        public bool ReceivedHysteresis { get; set; }

        public bool BlockedByCooldown { get; set; }
    }
}
