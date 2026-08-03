using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public sealed class RegisterTunnelSegmentHandler
    : ICommandHandler<RegisterTunnelSegmentCommand, Result>
{
    private readonly ITunnelInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public RegisterTunnelSegmentHandler(
        ITunnelInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(RegisterTunnelSegmentCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState state = _repository.Get();
        Result result = state.RegisterSegment(
            command.SegmentId,
            command.OriginKind,
            command.OriginCell,
            command.OrderedHorizontalCells,
            command.Tick);
        if (result.IsSuccess)
        {
            Save(state);
        }

        return result;
    }

    private void Save(TunnelInfrastructureState state)
    {
        _repository.Save(state);
        _eventSink.Append(state.DequeueUncommittedEvents());
    }
}

public sealed class RegisterCompletedTunnelAnchorHandler
    : ICommandHandler<RegisterCompletedTunnelAnchorCommand, Result>
{
    private readonly ITunnelInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public RegisterCompletedTunnelAnchorHandler(
        ITunnelInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(RegisterCompletedTunnelAnchorCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState state = _repository.Get();
        Result result = command.Kind == TunnelStructuralAnchorKind.WoodenSupport
            ? state.RegisterCompletedWoodenSupport(
                command.SegmentId,
                command.Cell,
                command.Tick)
            : state.RegisterCompletedDoor(
                command.SegmentId,
                command.Cell,
                command.Tick);
        if (result.IsSuccess)
        {
            Save(state);
        }

        return result;
    }

    private void Save(TunnelInfrastructureState state)
    {
        _repository.Save(state);
        _eventSink.Append(state.DequeueUncommittedEvents());
    }
}

public sealed class RegisterCompletedJunctionStoneTrimHandler
    : ICommandHandler<RegisterCompletedJunctionStoneTrimCommand, Result>
{
    private readonly ITunnelInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public RegisterCompletedJunctionStoneTrimHandler(
        ITunnelInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(RegisterCompletedJunctionStoneTrimCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState state = _repository.Get();
        Result result = state.RegisterCompletedJunctionStoneTrim(
            command.Cell,
            command.Tick);
        if (result.IsSuccess)
        {
            _repository.Save(state);
            _eventSink.Append(state.DequeueUncommittedEvents());
        }

        return result;
    }
}

public sealed class GetTunnelInfrastructureHandler
    : IQueryHandler<GetTunnelInfrastructureQuery, TunnelInfrastructureSnapshot>
{
    private readonly ITunnelInfrastructureRepository _repository;

    public GetTunnelInfrastructureHandler(ITunnelInfrastructureRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public TunnelInfrastructureSnapshot Handle(GetTunnelInfrastructureQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().CaptureSnapshot();
    }
}
}
