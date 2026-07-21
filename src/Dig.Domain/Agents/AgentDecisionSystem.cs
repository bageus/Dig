using System;
namespace Dig.Domain.Agents
{

public sealed partial class AgentDecisionSystem
{
    private const int CandidateCount = 7;
    private static readonly AgentSkillId GeneralWorkSkill =
        AgentSkillCatalog.Logistics;

    public AgentDecision Decide(
        AgentSnapshot agent,
        AgentDecisionContext context,
        AgentBehaviorPolicy policy,
        long tick)
    {
        ValidateInputs(agent, context, policy, tick);
        if (!agent.IsAlive)
        {
            return CreateDeadDecision(tick);
        }

        AgentIntentKind? currentIntent = agent.ActiveAction?.IntentKind;
        Span<Candidate> candidates = stackalloc Candidate[CandidateCount];
        string? activeOrderId = PopulateCandidates(
            agent,
            context,
            policy,
            tick,
            currentIntent,
            candidates);
        ApplyContinuityRules(agent, policy, tick, currentIntent, candidates);

        int selectedIndex = SelectCandidate(candidates);
        Candidate selected = candidates[selectedIndex];
        string selectedReason = GetSelectedReason(selected, currentIntent);
        UtilityOptionDiagnostic[] diagnostics = CreateDiagnostics(
            candidates,
            selectedIndex,
            selectedReason);
        string? orderId = selected.IntentKind == AgentIntentKind.PlayerOrder
            ? activeOrderId
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

    private static void ValidateInputs(
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
    }

    private static int SelectCandidate(ReadOnlySpan<Candidate> candidates)
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
        ReadOnlySpan<Candidate> candidates,
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
}
}
