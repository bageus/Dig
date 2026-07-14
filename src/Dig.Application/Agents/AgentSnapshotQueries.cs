using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Application.Messaging;
using Dig.Domain.Agents;

namespace Dig.Application.Agents
{

public sealed class GetAgentSnapshotsQuery : IQuery<IReadOnlyList<AgentSnapshot>>
{
    public GetAgentSnapshotsQuery(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
    }

    public long Tick { get; }
}

public sealed class GetAgentSnapshotsQueryHandler
    : IQueryHandler<GetAgentSnapshotsQuery, IReadOnlyList<AgentSnapshot>>
{
    private readonly IAgentRepository _repository;

    public GetAgentSnapshotsQueryHandler(IAgentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<AgentSnapshot> Handle(GetAgentSnapshotsQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        IReadOnlyList<AgentState> agents = _repository.GetAll();
        AgentSnapshot[] snapshots = new AgentSnapshot[agents.Count];
        for (int index = 0; index < agents.Count; index++)
        {
            snapshots[index] = agents[index].CreateSnapshot(query.Tick);
        }

        return new ReadOnlyCollection<AgentSnapshot>(snapshots);
    }
}
}