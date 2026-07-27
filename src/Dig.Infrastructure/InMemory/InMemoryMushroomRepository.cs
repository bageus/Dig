using System;
using Dig.Application.Ecology;
using Dig.Domain.Ecology;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryMushroomRepository : IMushroomRepository
{
    private MushroomState _mushrooms;

    public InMemoryMushroomRepository(MushroomState mushrooms)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
    }

    public MushroomState Get() => _mushrooms;

    public void Save(MushroomState mushrooms)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
    }
}

}
