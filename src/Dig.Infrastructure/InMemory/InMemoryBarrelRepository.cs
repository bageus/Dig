using System;
using Dig.Application.WorldObjects;
using Dig.Domain.WorldObjects;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryBarrelRepository : IBarrelRepository
{
    private BarrelState _barrels;

    public InMemoryBarrelRepository(BarrelState barrels)
    {
        _barrels = barrels ?? throw new ArgumentNullException(nameof(barrels));
    }

    public BarrelState Get() => _barrels;

    public void Save(BarrelState barrels)
    {
        _barrels = barrels ?? throw new ArgumentNullException(nameof(barrels));
    }
}

}