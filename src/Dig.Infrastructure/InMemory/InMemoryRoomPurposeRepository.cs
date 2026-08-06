using System;
using Dig.Application.Rooms;
using Dig.Domain.World;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryRoomPurposeRepository : IRoomPurposeRepository
{
    private RoomPurposeState _state;

    public InMemoryRoomPurposeRepository(RoomPurposeState? state = null)
    {
        _state = state ?? new RoomPurposeState();
    }

    public RoomPurposeState Get()
    {
        return _state;
    }

    public void Save(RoomPurposeState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }
}

}
