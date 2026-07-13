using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.World;

public interface IWorldRepository
{
    WorldState Get();

    void Save(WorldState world);
}

public sealed class DesignateDiggingCommand : ICommand<Result>
{
    public DesignateDiggingCommand(CellId cellId, bool designated, long tick)
    {
        CellId = cellId;
        Designated = designated;
        Tick = tick;
    }

    public CellId CellId { get; }

    public bool Designated { get; }

    public long Tick { get; }
}

public sealed class DesignateDiggingCommandHandler
    : ICommandHandler<DesignateDiggingCommand, Result>
{
    private readonly IWorldRepository _repository;
    private readonly IEventSink _eventSink;

    public DesignateDiggingCommandHandler(
        IWorldRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(DesignateDiggingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        WorldState world = _repository.Get();
        Result<WorldMutationResult> mutation = world.SetDigDesignation(
            command.CellId,
            command.Designated,
            command.Tick);
        if (mutation.IsFailure)
        {
            return Result.Failure(mutation.Error!);
        }

        _repository.Save(world);
        _eventSink.Append(world.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class ExcavateCellCommand : ICommand<Result>
{
    public ExcavateCellCommand(
        CellId cellId,
        MaterialId emptyMaterialId,
        long tick)
    {
        CellId = cellId;
        EmptyMaterialId = emptyMaterialId;
        Tick = tick;
    }

    public CellId CellId { get; }

    public MaterialId EmptyMaterialId { get; }

    public long Tick { get; }
}

public sealed class ExcavateCellCommandHandler
    : ICommandHandler<ExcavateCellCommand, Result>
{
    private readonly IWorldRepository _repository;
    private readonly IEventSink _eventSink;

    public ExcavateCellCommandHandler(
        IWorldRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(ExcavateCellCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        WorldState world = _repository.Get();
        Result<WorldMutationResult> mutation = world.Excavate(
            command.CellId,
            command.EmptyMaterialId,
            command.Tick);
        if (mutation.IsFailure)
        {
            return Result.Failure(mutation.Error!);
        }

        _repository.Save(world);
        _eventSink.Append(world.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class GetCellQuery : IQuery<Result<CellSnapshot>>
{
    public GetCellQuery(CellId cellId)
    {
        CellId = cellId;
    }

    public CellId CellId { get; }
}

public sealed class GetCellQueryHandler
    : IQueryHandler<GetCellQuery, Result<CellSnapshot>>
{
    private readonly IWorldRepository _repository;

    public GetCellQueryHandler(IWorldRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<CellSnapshot> Handle(GetCellQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().GetCell(query.CellId);
    }
}

public sealed class GetChunkSnapshotQuery : IQuery<Result<ChunkSnapshot>>
{
    public GetChunkSnapshotQuery(ChunkId chunkId)
    {
        ChunkId = chunkId;
    }

    public ChunkId ChunkId { get; }
}

public sealed class GetChunkSnapshotQueryHandler
    : IQueryHandler<GetChunkSnapshotQuery, Result<ChunkSnapshot>>
{
    private readonly IWorldRepository _repository;

    public GetChunkSnapshotQueryHandler(IWorldRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<ChunkSnapshot> Handle(GetChunkSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().CreateChunkSnapshot(query.ChunkId);
    }
}
