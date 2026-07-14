using System;
using Dig.Application.Messaging;
using Dig.Domain.Colonies;
using Dig.Domain.Core;

namespace Dig.Application.Colonies
{

public interface IColonyRepository
{
    ColonyState? Get(EntityId colonyId);

    void Save(ColonyState colony);
}

public sealed class RenameColonyCommand : ICommand<Result>
{
    public RenameColonyCommand(EntityId colonyId, string newName, long tick)
    {
        ColonyId = colonyId;
        NewName = newName;
        Tick = tick;
    }

    public EntityId ColonyId { get; }

    public string NewName { get; }

    public long Tick { get; }
}

public sealed class RenameColonyCommandHandler
    : ICommandHandler<RenameColonyCommand, Result>
{
    private readonly IColonyRepository _repository;
    private readonly IEventSink _eventSink;

    public RenameColonyCommandHandler(
        IColonyRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(RenameColonyCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ColonyState? colony = _repository.Get(command.ColonyId);
        if (colony is null)
        {
            return Result.Failure(ColonyErrors.NotFound);
        }

        Result renameResult = colony.Rename(command.NewName, command.Tick);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        _repository.Save(colony);
        _eventSink.Append(colony.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class GetColonySummaryQuery : IQuery<Result<ColonySummary>>
{
    public GetColonySummaryQuery(EntityId colonyId)
    {
        ColonyId = colonyId;
    }

    public EntityId ColonyId { get; }
}

public sealed class GetColonySummaryQueryHandler
    : IQueryHandler<GetColonySummaryQuery, Result<ColonySummary>>
{
    private readonly IColonyRepository _repository;

    public GetColonySummaryQueryHandler(IColonyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<ColonySummary> Handle(GetColonySummaryQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        ColonyState? colony = _repository.Get(query.ColonyId);
        if (colony is null)
        {
            return Result<ColonySummary>.Failure(ColonyErrors.NotFound);
        }

        ColonySummary summary = new ColonySummary(
            colony.Id,
            colony.Name,
            colony.Version);

        return Result<ColonySummary>.Success(summary);
    }
}

public sealed class ColonySummary
{
    public ColonySummary(EntityId id, string name, long version)
    {
        Id = id;
        Name = name;
        Version = version;
    }

    public EntityId Id { get; }

    public string Name { get; }

    public long Version { get; }
}
}
