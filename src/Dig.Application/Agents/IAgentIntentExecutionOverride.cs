using Dig.Domain.Agents;

namespace Dig.Application.Agents
{

public interface IAgentIntentExecutionOverride
{
    bool TryExecute(
        AgentState agent,
        AgentDecision decision,
        AgentBehaviorPolicy policy,
        long tick);
}

}
