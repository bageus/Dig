using Dig.Domain.Core;

namespace Dig.Application.Messaging;

public readonly struct CommandJournalEntry
{
    public CommandJournalEntry(
        long tick,
        string commandName,
        bool succeeded,
        string? errorCode)
    {
        Tick = tick;
        CommandName = commandName;
        Succeeded = succeeded;
        ErrorCode = errorCode;
    }

    public long Tick { get; }

    public string CommandName { get; }

    public bool Succeeded { get; }

    public string? ErrorCode { get; }
}

public interface IExecutionJournal : IEventSink
{
    void RecordCommand(CommandJournalEntry entry);
}

public sealed class CommandPipeline
{
    private readonly IExecutionJournal _journal;

    public CommandPipeline(IExecutionJournal journal)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
    }

    public Result Execute<TCommand>(
        TCommand command,
        ICommandHandler<TCommand, Result> handler,
        long tick)
        where TCommand : ICommand<Result>
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Result result = handler.Handle(command);
        _journal.RecordCommand(new CommandJournalEntry(
            tick,
            typeof(TCommand).Name,
            result.IsSuccess,
            result.Error?.Code));
        return result;
    }
}

public sealed class QueryPipeline
{
    public TResult Execute<TQuery, TResult>(
        TQuery query,
        IQueryHandler<TQuery, TResult> handler)
        where TQuery : IQuery<TResult>
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return handler.Handle(query);
    }
}
