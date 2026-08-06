using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{
public sealed class RoomPurposeStateTests
{
    [Fact]
    public void Completed_room_registration_and_purpose_selection_are_authoritative()
    {
        EntityId roomId = Id(1);
        InMemoryRoomPurposeRepository repository = new InMemoryRoomPurposeRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        RegisterCompletedRoomHandler register = new RegisterCompletedRoomHandler(
            repository,
            journal);
        ChangeRoomPurposeHandler change = new ChangeRoomPurposeHandler(
            repository,
            journal);

        Require(register.Handle(new RegisterCompletedRoomCommand(
            roomId,
            "Small",
            new[] { new CellId(1, 2, 0), new CellId(2, 2, 0) },
            tick: 1)));
        Require(change.Handle(new ChangeRoomPurposeCommand(
            roomId,
            RoomPurposeKind.Bedroom,
            tick: 2)));

        RoomPurposeSnapshot room = Assert.Single(repository.Get().CaptureSnapshot());
        Assert.Equal(RoomPurposeKind.Bedroom, room.RequestedPurpose);
        Assert.Equal(RoomPurposeKind.None, room.ActivePurpose);
        Assert.Equal(RoomImprovementStatus.Unimproved, room.ImprovementStatus);
        Assert.Contains(journal.Events, value =>
            value is RoomRequestedPurposeChanged changed
            && changed.RoomId == roomId
            && changed.RequestedPurpose == RoomPurposeKind.Bedroom);
    }

    [Fact]
    public void Repeated_registration_preserves_selected_purpose_and_restore()
    {
        EntityId roomId = Id(1);
        CellId[] cells = { new CellId(1, 2, 0), new CellId(2, 2, 0) };
        RoomPurposeState state = new RoomPurposeState();
        Require(state.RegisterCompletedRoom(roomId, "Small", cells, tick: 1));
        Require(state.ChangeRequestedPurpose(roomId, RoomPurposeKind.Workshop, tick: 2));
        Require(state.RegisterCompletedRoom(roomId, "Small", cells.Reverse(), tick: 3));

        var restored = RoomPurposeState.Restore(state.CaptureSnapshot(), state.Version);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        RoomPurposeSnapshot room = restored.Value.Get(roomId)!;
        Assert.Equal(RoomPurposeKind.Workshop, room.RequestedPurpose);
        Assert.Equal(state.Version, restored.Value.Version);
    }

    private static EntityId Id(int value) => EntityId.Parse(value.ToString("x32"));

    private static void Require(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }
}
}
