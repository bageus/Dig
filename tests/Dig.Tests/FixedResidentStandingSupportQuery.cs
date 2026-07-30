using Dig.Application.Agents;
using Dig.Domain.World;

namespace Dig.Tests
{
internal sealed class FixedResidentStandingSupportQuery
    : IResidentStandingSupportQuery
{
    private readonly bool _supported;

    internal FixedResidentStandingSupportQuery(bool supported)
    {
        _supported = supported;
    }

    public bool HasFullStandingSupport(CellId cell) => _supported;
}
}
