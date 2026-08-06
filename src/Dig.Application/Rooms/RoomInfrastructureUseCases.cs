using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Rooms;

namespace Dig.Application.Rooms
{

public sealed class SynchronizeCompletedRoomInfrastructureHandler
    : ICommandHandler<
        SynchronizeCompletedRoomInfrastructureCommand,
        Result<RoomInfrastructureSynchronizationResult>>
{
    private readonly IRoomInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;

    public SynchronizeCompletedRoomInfrastructureHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<RoomInfrastructureSynchronizationResult> Handle(
        SynchronizeCompletedRoomInfrastructureCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState state = _repository.Get();
        RoomInfrastructureSnapshot before = state.CaptureSnapshot();
        Dictionary<string, RoomInfrastructureProjectSnapshot> byTemplate = before.Rooms
            .ToDictionary(value => value.TemplateInstanceId, StringComparer.Ordinal);
        Dictionary<EntityId, RoomInfrastructureProjectSnapshot> byId = before.Rooms
            .ToDictionary(value => value.RoomInfrastructureId);
        if (!Preflight(command.Rooms, byTemplate, byId))
        {
            return Result<RoomInfrastructureSynchronizationResult>.Failure(
                RoomInfrastructureApplicationErrors.ProvenanceIdentityConflict);
        }

        int added = 0;
        int retained = 0;
        foreach (CompletedRoomInfrastructureProvenance room in command.Rooms)
        {
            if (byTemplate.ContainsKey(room.TemplateInstanceId))
            {
                retained++;
                continue;
            }

            Result registered = state.RegisterCompletedTemplateRoom(
                room.RoomInfrastructureId,
                room.TemplateInstanceId,
                room.TemplateKind,
                command.Tick);
            if (registered.IsFailure)
            {
                return Result<RoomInfrastructureSynchronizationResult>.Failure(
                    registered.Error!);
            }

            added++;
        }

        if (added > 0)
        {
            Save(state);
        }

        return Result<RoomInfrastructureSynchronizationResult>.Success(
            new RoomInfrastructureSynchronizationResult(added, retained));
    }

    private static bool Preflight(
        IReadOnlyList<CompletedRoomInfrastructureProvenance> rooms,
        IReadOnlyDictionary<string, RoomInfrastructureProjectSnapshot> byTemplate,
        IReadOnlyDictionary<EntityId, RoomInfrastructureProjectSnapshot> byId)
    {
        if (rooms.Select(value => value.TemplateInstanceId)
                .Distinct(StringComparer.Ordinal).Count() != rooms.Count
            || rooms.Select(value => value.RoomInfrastructureId)
                .Distinct().Count() != rooms.Count)
        {
            return false;
        }

        foreach (CompletedRoomInfrastructureProvenance room in rooms)
        {
            if (byTemplate.TryGetValue(
                    room.TemplateInstanceId,
                    out RoomInfrastructureProjectSnapshot? existing)
                && (existing.RoomInfrastructureId != room.RoomInfrastructureId
                    || existing.TemplateKind != room.TemplateKind))
            {
                return false;
            }

            if (byId.TryGetValue(
                    room.RoomInfrastructureId,
                    out RoomInfrastructureProjectSnapshot? owner)
                && !string.Equals(
                    owner.TemplateInstanceId,
                    room.TemplateInstanceId,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private void Save(RoomInfrastructureState state)
    {
        _repository.Save(state);
        _eventSink.Append(state.DequeueUncommittedEvents());
    }
}

public sealed class SynchronizeRoomTemporaryStockCellHandler
    : ICommandHandler<
        SynchronizeRoomTemporaryStockCellCommand,
        Result<RoomTemporaryStockCellPlan>>
{
    private readonly IRoomInfrastructureRepository _repository;
    private readonly IEventSink _eventSink;
    private readonly RoomTemporaryStockCellPlanner _planner;

    public SynchronizeRoomTemporaryStockCellHandler(
        IRoomInfrastructureRepository repository,
        IEventSink eventSink,
        RoomTemporaryStockCellPlanner? planner = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
        _planner = planner ?? new RoomTemporaryStockCellPlanner();
    }

    public Result<RoomTemporaryStockCellPlan> Handle(
        SynchronizeRoomTemporaryStockCellCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState state = _repository.Get();
        RoomInfrastructureProjectSnapshot? snapshot = state.Get(
            command.Room.RoomInfrastructureId);
        if (snapshot == null)
        {
            return Result<RoomTemporaryStockCellPlan>.Failure(
                RoomInfrastructureErrors.RoomNotFound);
        }

        if (!string.Equals(
                snapshot.TemplateInstanceId,
                command.Room.TemplateInstanceId,
                StringComparison.Ordinal)
            || snapshot.TemplateKind != command.Room.TemplateKind)
        {
            return Result<RoomTemporaryStockCellPlan>.Failure(
                RoomInfrastructureApplicationErrors.ProvenanceIdentityConflict);
        }

        RoomTemporaryStockCellPlan plan = _planner.Plan(
            command.Room,
            command.World,
            command.ReachableCells,
            command.OccupiedCells,
            snapshot.TemporaryStockCell);
        if (plan.Status == RoomTemporaryStockCellPlanStatus.BlockedNoFreeReachableCell
            || plan.Status == RoomTemporaryStockCellPlanStatus.Retained)
        {
            return Result<RoomTemporaryStockCellPlan>.Success(plan);
        }

        Result assigned = state.AssignTemporaryStockCell(
            command.Room.RoomInfrastructureId,
            plan.Cell!.Value,
            command.Tick);
        if (assigned.IsFailure)
        {
            return Result<RoomTemporaryStockCellPlan>.Failure(assigned.Error!);
        }

        Save(state);
        return Result<RoomTemporaryStockCellPlan>.Success(plan);
    }

    private void Save(RoomInfrastructureState state)
    {
        _repository.Save(state);
        _eventSink.Append(state.DequeueUncommittedEvents());
    }
}

public sealed class GetRoomInfrastructureHandler
    : IQueryHandler<GetRoomInfrastructureQuery, RoomInfrastructureSnapshot>
{
    private readonly IRoomInfrastructureRepository _repository;

    public GetRoomInfrastructureHandler(IRoomInfrastructureRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public RoomInfrastructureSnapshot Handle(GetRoomInfrastructureQuery query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().CaptureSnapshot();
    }
}

}
