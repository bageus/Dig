using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public enum RoomPurposeKind
{
    None = 0,
    Bedroom = 1,
    KitchenDining = 2,
    Workshop = 3,
    Farm = 4,
}

public enum RoomImprovementStatus
{
    Unimproved = 0,
    Ordered = 1,
    Improved = 2,
}

public static class RoomPurposeErrors
{
    public static readonly DomainError RoomNotFound = new DomainError(
        "room.purpose.room_not_found",
        "The completed template room was not found.");

    public static readonly DomainError RoomIdentityConflict = new DomainError(
        "room.purpose.identity_conflict",
        "The stable room identity already belongs to different room geometry.");

    public static readonly DomainError InvalidSnapshot = new DomainError(
        "room.purpose.invalid_snapshot",
        "The room-purpose snapshot is invalid.");
}

public sealed class RoomPurposeSnapshot
{
    private readonly CellId[] _volumeCells;

    public RoomPurposeSnapshot(
        EntityId roomId,
        string templateId,
        IEnumerable<CellId> volumeCells,
        RoomPurposeKind requestedPurpose,
        RoomPurposeKind activePurpose,
        RoomImprovementStatus improvementStatus,
        long version)
    {
        if (roomId.IsEmpty)
        {
            throw new ArgumentException("Room id is required.", nameof(roomId));
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("Room template id is required.", nameof(templateId));
        }

        if (volumeCells is null)
        {
            throw new ArgumentNullException(nameof(volumeCells));
        }

        CellId[] cells = volumeCells.Distinct().OrderBy(cell => cell).ToArray();
        if (cells.Length == 0)
        {
            throw new ArgumentException("Room volume cannot be empty.", nameof(volumeCells));
        }

        if (!Enum.IsDefined(typeof(RoomPurposeKind), requestedPurpose)
            || !Enum.IsDefined(typeof(RoomPurposeKind), activePurpose)
            || !Enum.IsDefined(typeof(RoomImprovementStatus), improvementStatus)
            || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        RoomId = roomId;
        TemplateId = templateId.Trim();
        _volumeCells = cells;
        RequestedPurpose = requestedPurpose;
        ActivePurpose = activePurpose;
        ImprovementStatus = improvementStatus;
        Version = version;
    }

    public EntityId RoomId { get; }
    public string TemplateId { get; }
    public IReadOnlyList<CellId> VolumeCells =>
        new ReadOnlyCollection<CellId>(_volumeCells);
    public RoomPurposeKind RequestedPurpose { get; }
    public RoomPurposeKind ActivePurpose { get; }
    public RoomImprovementStatus ImprovementStatus { get; }
    public long Version { get; }
}

public sealed class RoomPurposeState : AggregateRoot
{
    private readonly Dictionary<EntityId, RoomEntry> _rooms =
        new Dictionary<EntityId, RoomEntry>();

    public long Version { get; private set; }

    public Result RegisterCompletedRoom(
        EntityId roomId,
        string templateId,
        IEnumerable<CellId> volumeCells,
        long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("Room template id is required.", nameof(templateId));
        }

        if (volumeCells is null)
        {
            throw new ArgumentNullException(nameof(volumeCells));
        }

        CellId[] cells = volumeCells.Distinct().OrderBy(cell => cell).ToArray();
        if (cells.Length == 0)
        {
            throw new ArgumentException("Room volume cannot be empty.", nameof(volumeCells));
        }

        if (_rooms.TryGetValue(roomId, out RoomEntry? existing))
        {
            return string.Equals(existing.TemplateId, templateId, StringComparison.Ordinal)
                && existing.VolumeCells.SequenceEqual(cells)
                    ? Result.Success()
                    : Result.Failure(RoomPurposeErrors.RoomIdentityConflict);
        }

        _rooms.Add(roomId, new RoomEntry(templateId.Trim(), cells));
        Version = checked(Version + 1);
        Raise(new CompletedRoomRegistered(tick, roomId));
        return Result.Success();
    }

    public Result ChangeRequestedPurpose(
        EntityId roomId,
        RoomPurposeKind purpose,
        long tick)
    {
        ValidateTick(tick);
        if (!Enum.IsDefined(typeof(RoomPurposeKind), purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }

        if (!_rooms.TryGetValue(roomId, out RoomEntry? room))
        {
            return Result.Failure(RoomPurposeErrors.RoomNotFound);
        }

        if (room.RequestedPurpose == purpose)
        {
            return Result.Success();
        }

        RoomPurposeKind previous = room.RequestedPurpose;
        room.RequestedPurpose = purpose;
        if (room.ImprovementStatus == RoomImprovementStatus.Improved)
        {
            room.ActivePurpose = purpose;
        }

        room.Version = checked(room.Version + 1);
        Version = checked(Version + 1);
        Raise(new RoomRequestedPurposeChanged(tick, roomId, previous, purpose));
        return Result.Success();
    }

    public RoomPurposeSnapshot? Get(EntityId roomId)
    {
        return _rooms.TryGetValue(roomId, out RoomEntry? room)
            ? room.Capture(roomId)
            : null;
    }

    public IReadOnlyList<RoomPurposeSnapshot> CaptureSnapshot()
    {
        return new ReadOnlyCollection<RoomPurposeSnapshot>(_rooms
            .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
            .Select(pair => pair.Value.Capture(pair.Key))
            .ToArray());
    }

    public static Result<RoomPurposeState> Restore(
        IEnumerable<RoomPurposeSnapshot> snapshots,
        long version)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        if (version < 0)
        {
            return Result<RoomPurposeState>.Failure(RoomPurposeErrors.InvalidSnapshot);
        }

        RoomPurposeState state = new RoomPurposeState();
        foreach (RoomPurposeSnapshot snapshot in snapshots)
        {
            if (snapshot == null || state._rooms.ContainsKey(snapshot.RoomId))
            {
                return Result<RoomPurposeState>.Failure(RoomPurposeErrors.InvalidSnapshot);
            }

            state._rooms.Add(snapshot.RoomId, new RoomEntry(
                snapshot.TemplateId,
                snapshot.VolumeCells.ToArray(),
                snapshot.RequestedPurpose,
                snapshot.ActivePurpose,
                snapshot.ImprovementStatus,
                snapshot.Version));
        }

        state.Version = version;
        return Result<RoomPurposeState>.Success(state);
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private sealed class RoomEntry
    {
        internal RoomEntry(
            string templateId,
            CellId[] volumeCells,
            RoomPurposeKind requestedPurpose = RoomPurposeKind.None,
            RoomPurposeKind activePurpose = RoomPurposeKind.None,
            RoomImprovementStatus improvementStatus = RoomImprovementStatus.Unimproved,
            long version = 0)
        {
            TemplateId = templateId;
            VolumeCells = volumeCells;
            RequestedPurpose = requestedPurpose;
            ActivePurpose = activePurpose;
            ImprovementStatus = improvementStatus;
            Version = version;
        }

        internal string TemplateId { get; }
        internal CellId[] VolumeCells { get; }
        internal RoomPurposeKind RequestedPurpose { get; set; }
        internal RoomPurposeKind ActivePurpose { get; set; }
        internal RoomImprovementStatus ImprovementStatus { get; set; }
        internal long Version { get; set; }

        internal RoomPurposeSnapshot Capture(EntityId roomId)
        {
            return new RoomPurposeSnapshot(
                roomId,
                TemplateId,
                VolumeCells,
                RequestedPurpose,
                ActivePurpose,
                ImprovementStatus,
                Version);
        }
    }
}

public sealed class CompletedRoomRegistered : IDomainEvent
{
    public CompletedRoomRegistered(long tick, EntityId roomId)
    {
        Tick = tick;
        RoomId = roomId;
    }

    public long Tick { get; }
    public EntityId RoomId { get; }
}

public sealed class RoomRequestedPurposeChanged : IDomainEvent
{
    public RoomRequestedPurposeChanged(
        long tick,
        EntityId roomId,
        RoomPurposeKind previousPurpose,
        RoomPurposeKind requestedPurpose)
    {
        Tick = tick;
        RoomId = roomId;
        PreviousPurpose = previousPurpose;
        RequestedPurpose = requestedPurpose;
    }

    public long Tick { get; }
    public EntityId RoomId { get; }
    public RoomPurposeKind PreviousPurpose { get; }
    public RoomPurposeKind RequestedPurpose { get; }
}

}
