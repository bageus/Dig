using Dig.Domain.Agents;

namespace Dig.Application.Agents
{

public interface IAgentActionExecutionGate
{
    bool CanExecuteActions(AgentState agent, long tick);
}

}