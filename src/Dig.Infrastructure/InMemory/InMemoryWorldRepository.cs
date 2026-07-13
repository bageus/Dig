using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryWorldRepository : IWorldRepository
{
    private readonly object _gate = new object();
    private WorldState _world;

    public InMemoryWorldRepository(WorldState world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public WorldState Get()
    {
        lock (_gate)
        {
            return _world;
        }
    }

    public void Save(WorldState world)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        lock (_gate)
        {
            _world = world;
        }
    }
}
