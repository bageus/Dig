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

    internal void Bind(
        DigTerrainWorkSession terrain,
        Func<EntityId, long, bool> isCombatActiveOrThreatened)
    {
        _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        _isCombatActiveOrThreatened = isCombatActiveOrThreatened
            ?? throw new ArgumentNullException(nameof(isCombatActiveOrThreatened));
    }

    public AgentDecisionContext GetContext(AgentSnapshot agent, long tick)
    {
        return _terrain?.CreateResidentNeedsContext(agent, tick)
            ?? AgentDecisionContext.AllAvailable();
    }

    public bool CanExecuteActions(AgentState agent, long tick)
    {
        if (_terrain == null || _isCombatActiveOrThreatened == null)
        {
            return true;
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