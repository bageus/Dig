using System;
using Dig.Application.Combat;
using Dig.Domain.Combat;
using Dig.Domain.Factions;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryCombatRepository : ICombatRepository
{
    private CombatState _combat;

    public InMemoryCombatRepository(CombatState combat)
    {
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
    }

    public CombatState Get()
    {
        return _combat;
    }

    public void Save(CombatState combat)
    {
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
    }
}

public sealed class InMemoryFactionRepository : IFactionRepository
{
    private FactionState _factions;

    public InMemoryFactionRepository(FactionState factions)
    {
        _factions = factions ?? throw new ArgumentNullException(nameof(factions));
    }

    public FactionState Get()
    {
        return _factions;
    }

    public void Save(FactionState factions)
    {
        _factions = factions ?? throw new ArgumentNullException(nameof(factions));
    }
}
}
