using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Domain.Production
{

public static class BuildingSupplyReachability
{
    public static IReadOnlyList<CellId> ResolveConnectedCells(
        NavigationSnapshot navigation,
        CellId destination)
    {
        if (navigation is null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        if (!navigation.TryGetRegion(destination, out int destinationRegion))
        {
            return Array.Empty<CellId>();
        }

        return navigation.Chunks
            .SelectMany(value => value.WalkableCells)
            .Where(value => navigation.TryGetRegion(value, out int region)
                && region == destinationRegion)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }

    public static bool IsConnected(
        NavigationSnapshot navigation,
        CellId left,
        CellId right)
    {
        if (navigation is null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        return navigation.TryGetRegion(left, out int leftRegion)
            && navigation.TryGetRegion(right, out int rightRegion)
            && leftRegion == rightRegion;
    }
}

}
