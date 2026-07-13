using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Agents;

public sealed class SetAgentPlayerOrderCommand : ICommand<Result>
{
    public SetAgentPlayerOrderCommand(
        EntityId agentId,
        PlayerOrder order,
        long tick)
    {
        AgentId = agentId;
        Order = order ?? throw new ArgumentNullException(nameof(order));
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public PlayerOrder Order { get; }

    public long Tick { get; }
}

public sealed class SetAgentPlayerOrderCommandHandler
    : ICommandHandler<SetAgentPlayerOrderCommand, Result>
{
    private readonly IAgentRepository _repository;
    private readonly IEventSink _eventSink;

    public SetAgentPlayerOrderCommandHandler(
        IAgentRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(SetAgentPlayerOrderCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? agent = _repository.Get(command.AgentId);
        if (agent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        Result result = agent.SetPlayerOrder(command.Order, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(agent);
        _eventSink.Append(agent.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class ClearAgentPlayerOrderCommand : ICommand<Result>
{
    public ClearAgentPlayerOrderCommand(EntityId agentId, long tick)
    {
        AgentId = agentId;
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public long Tick { get; }
}

public sealed class ClearAgentPlayerOrderCommandHandler
    : ICommandHandler<ClearAgentPlayerOrderCommand, Result>
{
    private readonly IAgentRepository _repository;
    private readonly IEventSink _eventSink;

    public ClearAgentPlayerOrderCommandHandler(
        IAgentRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(ClearAgentPlayerOrderCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? agent = _repository.Get(command.AgentId);
        if (agent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        Result result = agent.ClearPlayerOrder(command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(agent);
        _eventSink.Append(agent.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class GetAgentSnapshotQuery : IQuery<Result<AgentSnapshot>>
{
    public GetAgentSnapshotQuery(EntityId agentId, long tick)
    {
        AgentId = agentId;
        Tick = tick;
    }

    public EntityId AgentId { get; }

    public long Tick { get; }
}

public sealed class GetAgentSnapshotQueryHandler
    : IQueryHandler<GetAgentSnapshotQuery, Result<AgentSnapshot>>
{
    private readonly IAgentRepository _repository;

    public GetAgentSnapshotQueryHandler(IAgentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<AgentSnapshot> Handle(GetAgentSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        AgentState? agent = _repository.Get(query.AgentId);
        return agent is null
            ? Result<AgentSnapshot>.Failure(AgentApplicationErrors.NotFound)
            : Result<AgentSnapshot>.Success(agent.CreateSnapshot(query.Tick));
    }
}
