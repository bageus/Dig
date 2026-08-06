using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public sealed class RegisterCompletedRoomHandler
    : ICommandHandler<RegisterCompletedRoomCommand, Result>
{
    private readonly IRoomPurposeRepository _repository;
    private readonly IEventSink _eventSink;

    public RegisterCompletedRoomHandler(
        IRoomPurposeRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(RegisterCompletedRoomCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomPurposeState state = _repository.Get();
        Result result = state.RegisterCompletedRoom(
            command.RoomId,
            command.TemplateId,
            command.VolumeCells,
            command.Tick);
        if (result.IsSuccess)
        {
            Save(state);
        }

        return result;
    }

    private void Save(RoomPurposeState state)
    {
        _repository.Save(state);
        _eventSink.Append(state.DequeueUncommittedEvents());
    }
}

public sealed class ChangeRoomPurposeHandler
    : ICommandHandler<ChangeRoomPurposeCommand, Result>
{
    private readonly IRoomPurposeRepository _repository;
    private readonly IEventSink _eventSink;

    public ChangeRoomPurposeHandler(
        IRoomPurposeRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(ChangeRoomPurposeCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomPurposeState state = _repository.Get();
        Result result = state.ChangeRequestedPurpose(
            command.RoomId,
            command.Purpose,
            command.Tick);
        if (result.IsSuccess)
        {
            _repository.Save(state);
            _eventSink.Append(state.DequeueUncommittedEvents());
        }

        return result;
    }
}

public sealed class GetRoomPurposesHandler
    : IQueryHandler<GetRoomPurposesQuery, IReadOnlyList<RoomPurposeSnapshot>>
{
    private readonly IRoomPurposeRepository _repository;

    public GetRoomPurposesHandler(IRoomPurposeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<RoomPurposeSnapshot> Handle(GetRoomPurposesQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().CaptureSnapshot();
    }
}

}
