using System;
using Dig.Application.Rooms;
using Dig.Domain.Rooms;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryRoomInfrastructureRepository
    : IRoomInfrastructureRepository
{
    private RoomInfrastructureState _state;

    public InMemoryRoomInfrastructureRepository(
        RoomInfrastructureState? state = null)
    {
        _state = state ?? new RoomInfrastructureState();
    }

    public RoomInfrastructureState Get()
    {
        return _state;
    }

    public void Save(RoomInfrastructureState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }
}

}
