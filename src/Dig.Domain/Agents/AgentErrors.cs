using Dig.Domain.Core;

namespace Dig.Domain.Agents;

public static class AgentErrors
{
    public static readonly DomainError AgentDead = new DomainError(
        "agents.agent.dead",
        "A dead agent cannot perform the requested operation.");

    public static readonly DomainError TickNotIncreasing = new DomainError(
        "agents.tick.not_increasing",
        "Agent needs can be advanced only once per increasing simulation tick.");

    public static readonly DomainError DecisionTickMismatch = new DomainError(
        "agents.decision.tick_mismatch",
        "The decision tick does not match the action application tick.");

    public static readonly DomainError PlayerOrderInactive = new DomainError(
        "agents.player_order.inactive",
        "The player order is not active at the requested tick.");

    public static readonly DomainError TargetedActionRequired = new DomainError(
        "agents.action.target_required",
        "The active action does not have a reserved settlement target.");

    public static readonly DomainError TargetedActionNotReady = new DomainError(
        "agents.action.not_ready",
        "The targeted action has not reached its required duration.");

    public static readonly DomainError TargetedActionAlreadyReady = new DomainError(
        "agents.action.already_ready",
        "The targeted action is already waiting for external completion.");
}
