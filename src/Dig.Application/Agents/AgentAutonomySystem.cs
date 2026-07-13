using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents;

public sealed class AgentAutonomySystem : ISimulationSystem
{
    private readonly IAgentRepository _repository;
    private readonly IAgentDecisionContextProvider _contextProvider;
    private readonly AgentDecisionSystem _decisionSystem;
    private readonly AgentBehaviorPolicy _policy;

    public AgentAutonomySystem(
        IAgentRepository repository,
        IAgentDecisionContextProvider contextProvider,
        AgentDecisionSystem decisionSystem,
        AgentBehaviorPolicy policy,
        int order = 300,
        int intervalTicks = 1)
    {
        if (intervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalTicks));
        }

        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contextProvider = contextProvider
            ?? throw new ArgumentNullException(nameof(contextProvider));
        _decisionSystem = decisionSystem
            ?? throw new ArgumentNullException(nameof(decisionSystem));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Order = order;
        IntervalTicks = intervalTicks;
    }

    public string Name => "agents.autonomy";

    public int Order { get; }

    public int IntervalTicks { get; }

    public AgentTickReport? LastReport { get; private set; }

    public void Execute(SimulationContext context)
    {
        List<AgentTickDecision> decisions = new List<AgentTickDecision>();
        foreach (AgentState agent in _repository.GetAll())
        {
            if (!agent.IsAlive)
            {
                continue;
            }

            Require(agent.AdvanceNeeds(_policy, context.Tick));
            if (!agent.IsAlive)
            {
                _repository.Save(agent);
                continue;
            }

            AgentSnapshot snapshot = agent.CreateSnapshot(context.Tick);
            AgentDecisionContext decisionContext = _contextProvider.GetContext(
                snapshot,
                context.Tick);
            AgentDecision decision = _decisionSystem.Decide(
                snapshot,
                decisionContext,
                _policy,
                context.Tick);
            Require(agent.ApplyDecision(decision, _policy, context.Tick));
            Require(agent.AdvanceAction(_policy, context.Tick));
            _repository.Save(agent);
            decisions.Add(new AgentTickDecision(agent.Id, decision));
        }

        LastReport = new AgentTickReport(context.Tick, decisions);
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
