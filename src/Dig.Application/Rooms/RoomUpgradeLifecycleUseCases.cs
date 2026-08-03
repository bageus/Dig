using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Rooms;

namespace Dig.Application.Rooms
{

public sealed class OrderRoomUpgradeHandler
    : ICommandHandler<OrderRoomUpgradeCommand, Result>
{
    private readonly RoomInfrastructureMutation _mutation;

    public OrderRoomUpgradeHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _mutation = new RoomInfrastructureMutation(repository, eventSink);
    }

    public Result Handle(OrderRoomUpgradeCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return _mutation.Apply(state => state.OrderUpgrade(
            command.RoomInfrastructureId,
            command.RequestedPurpose,
            command.Tick));
    }
}

public sealed class ChangeRoomRequestedPurposeHandler
    : ICommandHandler<ChangeRoomRequestedPurposeCommand, Result>
{
    private readonly RoomInfrastructureMutation _mutation;

    public ChangeRoomRequestedPurposeHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _mutation = new RoomInfrastructureMutation(repository, eventSink);
    }

    public Result Handle(ChangeRoomRequestedPurposeCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return _mutation.Apply(state => state.ChangeRequestedPurpose(
            command.RoomInfrastructureId,
            command.Purpose,
            command.Tick));
    }
}

public sealed class AttachRoomUpgradeJobHandler
    : ICommandHandler<AttachRoomUpgradeJobCommand, Result>
{
    private readonly RoomInfrastructureMutation _mutation;

    public AttachRoomUpgradeJobHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _mutation = new RoomInfrastructureMutation(repository, eventSink);
    }

    public Result Handle(AttachRoomUpgradeJobCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return _mutation.Apply(state => state.AttachJob(
            command.RoomInfrastructureId,
            command.JobId,
            command.Tick));
    }
}

public sealed class RecordRoomMaterialDeliveryHandler
    : ICommandHandler<RecordRoomMaterialDeliveryCommand, Result>
{
    private readonly RoomInfrastructureMutation _mutation;

    public RecordRoomMaterialDeliveryHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _mutation = new RoomInfrastructureMutation(repository, eventSink);
    }

    public Result Handle(RecordRoomMaterialDeliveryCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return _mutation.Apply(state => state.RecordDelivery(
            command.RoomInfrastructureId,
            command.DeliveryJobId,
            command.ItemId,
            command.Quantity,
            command.Tick));
    }
}

public sealed class StartRoomImprovementWorkHandler
    : ICommandHandler<StartRoomImprovementWorkCommand, Result>
{
    private readonly RoomInfrastructureMutation _mutation;

    public StartRoomImprovementWorkHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _mutation = new RoomInfrastructureMutation(repository, eventSink);
    }

    public Result Handle(StartRoomImprovementWorkCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return _mutation.Apply(state => state.StartImprovementWork(
            command.RoomInfrastructureId,
            command.WorkJobId,
            command.Tick));
    }
}

public sealed class CommitRoomMaterialUnitHandler
    : ICommandHandler<
        CommitRoomMaterialUnitCommand,
        Result<RoomMaterialCommitResult>>
{
    private readonly IRoomInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public CommitRoomMaterialUnitHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<RoomMaterialCommitResult> Handle(
        CommitRoomMaterialUnitCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState state = _repository.Get();
        Result<RoomMaterialCommitResult> result = state.CommitMaterialUnit(
            command.RoomInfrastructureId,
            command.WorkJobId,
            command.UnitId,
            command.Tick);
        if (result.IsSuccess && !result.Value.AlreadyCommitted)
        {
            Save(state);
        }

        return result;
    }

    private void Save(RoomInfrastructureState state)
    {
        _repository.Save(state);
        _eventSink.Append(state.DequeueUncommittedEvents());
    }
}

public sealed class CancelRoomUpgradeBeforeWorkHandler
    : ICommandHandler<
        CancelRoomUpgradeBeforeWorkCommand,
        Result<RoomUpgradeCancellationResult>>
{
    private readonly IRoomInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public CancelRoomUpgradeBeforeWorkHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<RoomUpgradeCancellationResult> Handle(
        CancelRoomUpgradeBeforeWorkCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState state = _repository.Get();
        Result<RoomUpgradeCancellationResult> result =
            state.CancelUpgradeBeforeWork(
                command.RoomInfrastructureId,
                command.Reason,
                command.Tick);
        if (result.IsSuccess)
        {
            _repository.Save(state);
            _eventSink.Append(state.DequeueUncommittedEvents());
        }

        return result;
    }
}

internal sealed class RoomInfrastructureMutation
{
    private readonly IRoomInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public RoomInfrastructureMutation(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Apply(Func<RoomInfrastructureState, Result> mutation)
    {
        RoomInfrastructureState state = _repository.Get();
        Result result = mutation(state);
        if (result.IsSuccess)
        {
            _repository.Save(state);
            _eventSink.Append(state.DequeueUncommittedEvents());
        }

        return result;
    }
}

}
