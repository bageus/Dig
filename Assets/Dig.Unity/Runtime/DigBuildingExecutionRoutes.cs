using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed class BuildingPackingRoutePlan
{
    public BuildingPackingRoutePlan(CellId target, PathResult path)
    {
        Target = target;
        Path = path;
    }

    public CellId Target { get; }
    public PathResult Path { get; }
}

internal sealed class BuildingBoxAssemblyRoutePlan
{
    public BuildingBoxAssemblyRoutePlan(CellId target, PathResult path)
    {
        Target = target;
        Path = path;
    }

    public CellId Target { get; }
    public PathResult Path { get; }
}

}
