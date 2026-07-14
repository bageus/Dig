using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Application.Messaging
{

public interface ICommand<TResult>
{
}

public interface IQuery<TResult>
{
}

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    TResult Handle(TCommand command);
}

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    TResult Handle(TQuery query);
}

public interface IEventSink
{
    void Append(IReadOnlyCollection<IDomainEvent> events);
}
}
