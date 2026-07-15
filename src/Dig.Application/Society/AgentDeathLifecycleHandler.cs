using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Society;

namespace Dig.Application.Society
{

public sealed class AgentDeathLifecycleHandler
{
    private static readonly ResidentDeathCauseId HealthDepletedCause =
        new ResidentDeathCauseId("health_depleted");

    private readonly IAgentRepository _agents;
    private readonly ISocietyRepository _society;
    private readonly ResidentDeathCleanupHandler _cleanup;
    private readonly IEventSink _eventSink;

    public AgentDeathLifecycleHandler(
        IAgentRepository agents,
        ISocietyRepository society,
        ResidentDeathCleanupHandler cleanup,
        IEventSink eventSink)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _society = society ?? throw new ArgumentNullException(nameof(society));
        _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<AgentDeathLifecycleReport> Handle(AgentDied agentDied)
    {
        if (agentDied is null)
        {
            throw new ArgumentNullException(nameof(agentDied));
        }

        AgentState? agent = _agents.Get(agentDied.AgentId);
        if (agent is null)
        {
            return Result<AgentDeathLifecycleReport>.Failure(
                SocietyApplicationErrors.AgentNotFound);
        }

        AgentSnapshot snapshot = agent.CreateSnapshot(agentDied.Tick);
        if (snapshot.IsAlive)
        {
            return Result<AgentDeathLifecycleReport>.Failure(
                SocietyApplicationErrors.AgentStillAlive);
        }

        SocietyState society = _society.Get();
        Result recorded = society.RecordDeath(
            agentDied.AgentId,
            HealthDepletedCause,
            snapshot.Position,
            agentDied.Tick);
        if (recorded.IsFailure)
        {
            return Result<AgentDeathLifecycleReport>.Failure(recorded.Error!);
        }

        _society.Save(society);
        IReadOnlyList<IDomainEvent> lifecycleEvents = society.DequeueUncommittedEvents();
        ResidentDied? residentDied = lifecycleEvents
            .OfType<ResidentDied>()
            .LastOrDefault(item => item.ResidentId == agentDied.AgentId);
        _eventSink.Append(lifecycleEvents);

        ResidentDeathCleanupReport? cleanup = residentDied is null
            ? null
            : _cleanup.Handle(residentDied);
        return Result<AgentDeathLifecycleReport>.Success(
            new AgentDeathLifecycleReport(
                agentDied.AgentId,
                residentDied is not null,
                cleanup));
    }
}
}
