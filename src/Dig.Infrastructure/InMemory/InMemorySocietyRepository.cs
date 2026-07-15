using System;
using Dig.Application.Society;
using Dig.Domain.Society;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemorySocietyRepository : ISocietyRepository
{
    private SocietyState _society;

    public InMemorySocietyRepository(SocietyState society)
    {
        _society = society ?? throw new ArgumentNullException(nameof(society));
    }

    public SocietyState Get()
    {
        return _society;
    }

    public void Save(SocietyState society)
    {
        _society = society ?? throw new ArgumentNullException(nameof(society));
    }
}
}
