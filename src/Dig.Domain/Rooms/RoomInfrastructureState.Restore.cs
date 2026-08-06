using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Rooms
{

public sealed partial class RoomInfrastructureState
{
    public RoomInfrastructureProjectSnapshot? Get(EntityId roomInfrastructureId)
    {
        return Find(roomInfrastructureId)?.CreateSnapshot();
    }

    public RoomInfrastructureProjectSnapshot? GetByTemplateInstance(
        string templateInstanceId)
    {
        if (string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));
        }

        return _roomsByTemplateInstance.TryGetValue(
                templateInstanceId.Trim(),
                out EntityId roomId)
            ? _rooms[roomId].CreateSnapshot()
            : null;
    }

    public RoomInfrastructureSnapshot CaptureSnapshot()
    {
        return new RoomInfrastructureSnapshot(
            Version,
            _rooms.Values.Select(value => value.CreateSnapshot()));
    }

    public static Result<RoomInfrastructureState> Restore(
        RoomInfrastructureSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        RoomInfrastructureState state = new RoomInfrastructureState();
        foreach (RoomInfrastructureProjectSnapshot roomSnapshot in snapshot.Rooms)
        {
            Result<RoomInfrastructureProjectState> restored =
                RoomInfrastructureProjectState.Restore(roomSnapshot);
            if (restored.IsFailure
                || state._rooms.ContainsKey(roomSnapshot.RoomInfrastructureId)
                || state._roomsByTemplateInstance.ContainsKey(roomSnapshot.TemplateInstanceId))
            {
                return Result<RoomInfrastructureState>.Failure(
                    RoomInfrastructureErrors.InvalidSnapshot);
            }

            state._rooms.Add(roomSnapshot.RoomInfrastructureId, restored.Value);
            state._roomsByTemplateInstance.Add(
                roomSnapshot.TemplateInstanceId,
                roomSnapshot.RoomInfrastructureId);
        }

        state.Version = snapshot.Version;
        return Result<RoomInfrastructureState>.Success(state);
    }

    private RoomInfrastructureProjectState? Find(EntityId roomInfrastructureId)
    {
        if (roomInfrastructureId.IsEmpty)
        {
            throw new ArgumentException("Room infrastructure id cannot be empty.", nameof(roomInfrastructureId));
        }

        return _rooms.TryGetValue(
            roomInfrastructureId,
            out RoomInfrastructureProjectState? room)
            ? room
            : null;
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}

}
