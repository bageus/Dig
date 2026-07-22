using System;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents
{

public interface IAgentSkillGrantService
{
    Result Validate(SkillGrantBundle bundle);

    Result<SkillRedistributionReport> ApplyConfirmed(
        SkillGrantBundle bundle);
}

public sealed class AgentSkillGrantService : IAgentSkillGrantService
{
    private readonly IAgentRepository _agents;
    private readonly IEventSink _events;

    public AgentSkillGrantService(IAgentRepository agents, IEventSink events)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Validate(SkillGrantBundle bundle)
    {
        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }

        AgentState? agent = _agents.Get(bundle.AgentId);
        if (agent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        return agent.IsAlive
            ? Result.Success()
            : Result.Failure(AgentErrors.AgentDead);
    }

    public Result<SkillRedistributionReport> ApplyConfirmed(
        SkillGrantBundle bundle)
    {
        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }

        Result validation = Validate(bundle);
        if (validation.IsFailure)
        {
            return Result<SkillRedistributionReport>.Failure(validation.Error!);
        }

        AgentState agent = _agents.Get(bundle.AgentId)!;
        Result<SkillRedistributionReport> applied = agent.ApplySkillGrant(bundle);
        if (applied.IsFailure)
        {
            return applied;
        }

        _agents.Save(agent);
        if (!applied.Value.WasAlreadyApplied)
        {
            _events.Append(new IDomainEvent[]
            {
                new SkillProgressionResultConfirmed(bundle),
            });
            _events.Append(agent.DequeueUncommittedEvents());
        }

        return applied;
    }
}

}
