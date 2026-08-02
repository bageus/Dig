using System;
using Dig.Application.Tunnels;
using Dig.Domain.World;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryTunnelInfrastructureRepository
    : ITunnelInfrastructureRepository
{
    private TunnelInfrastructureState _state;

    public InMemoryTunnelInfrastructureRepository(
        TunnelInfrastructureState? state = null)
    {
        _state = state ?? new TunnelInfrastructureState();
    }

    public TunnelInfrastructureState Get()
    {
        return _state;
    }

    public void Save(TunnelInfrastructureState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }
}
}
