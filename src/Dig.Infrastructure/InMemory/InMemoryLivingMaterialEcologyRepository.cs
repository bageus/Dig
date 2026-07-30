using System;
using Dig.Application.Ecology;
using Dig.Domain.Ecology;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryLivingMaterialEcologyRepository
    : ILivingMaterialEcologyRepository
{
    private LivingMaterialEcologyState _state;

    public InMemoryLivingMaterialEcologyRepository(LivingMaterialEcologyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public LivingMaterialEcologyState Get() => _state;

    public void Save(LivingMaterialEcologyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }
}

}
