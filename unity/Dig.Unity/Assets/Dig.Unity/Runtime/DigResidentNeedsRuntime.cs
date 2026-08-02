using System;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Unity
{

internal sealed class DigResidentNeedsRuntime :
    IAgentDecisionContextProvider,
    IAgentIntentExecutionOverride,
    IAgentActionExecutionGate
{
    private DigTerrainWorkSession? _terrain;
    private Func<EntityId, long, bool>? _isCombatActiveOrThreatened;
    private Func<EntityId, long, bool>? _hasDirectCommandPriority;

    internal void Bind(
        DigTerrainWorkSession terrain,
        Func<EntityId, long, bool> isCombatActiveOrThreatened,
        Func<EntityId, long, bool> hasDirectCommandPriority)
    {
        _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        _isCombatActiveOrThreatened = isCombatActiveOrThreatened
            ?? throw new ArgumentNullException(nameof(isCombatActiveOrThreatened));
        _hasDirectCommandPriority = hasDirectCommandPriority
            ?? throw new ArgumentNullException(nameof(hasDirectCommandPriority));
    }

    public AgentDecisionContext GetContext(AgentSnapshot agent, long tick)
    {
        return _terrain?.CreateResidentNeedsContext(agent, tick)
            ?? AgentDecisionContext.AllAvailable();
    }

    public bool CanExecuteActions(AgentState agent, long tick)
    {
        if (_terrain == null
            || _isCombatActiveOrThreatened == null
            || _hasDirectCommandPriority == null)
        {
            return true;
        }

        // A direct player command owns the resident before both autonomy and
        // self-defense. Its own movement/job pipeline advances later in the tick.
        if (_hasDirectCommandPriority(agent.Id, tick))
        {
            return false;
        }

        if (!_isCombatActiveOrThreatened(agent.Id, tick))
        {
            return true;
        }

        Result interrupted = _terrain.InterruptResidentForCombat(agent, tick);
        if (interrupted.IsFailure)
        {
            throw new InvalidOperationException(interrupted.Error!.ToString());
        }

        return false;
    }

    public bool TryExecute(
        AgentState agent,
        AgentDecision decision,
        AgentBehaviorPolicy policy,
        long tick)
    {
        return _terrain?.TryExecuteResidentNeedsAction(
            agent,
            decision,
            policy,
            tick) == true;
    }
}

}
