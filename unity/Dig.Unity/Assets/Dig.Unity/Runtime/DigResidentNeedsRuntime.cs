using System;
using Dig.Application.Agents;
using Dig.Domain.Agents;

namespace Dig.Unity
{

internal sealed class DigResidentNeedsRuntime :
    IAgentDecisionContextProvider,
    IAgentIntentExecutionOverride
{
    private DigTerrainWorkSession? _terrain;

    internal void Bind(DigTerrainWorkSession terrain)
    {
        _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
    }

    public AgentDecisionContext GetContext(AgentSnapshot agent, long tick)
    {
        return _terrain?.CreateResidentNeedsContext(agent, tick)
            ?? AgentDecisionContext.AllAvailable();
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
