using System;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class WorldItemGravityPolicy
{
    public static CellId ResolveLandingCell(
        CellId source,
        int worldHeight,
        Func<CellId, bool> isSolid)
    {
        if (worldHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldHeight));
        }

        if (isSolid == null)
        {
            throw new ArgumentNullException(nameof(isSolid));
        }

        CellId current = source;
        while (current.Y + 1 < worldHeight)
        {
            CellId below = new CellId(current.X, current.Y + 1, current.Z);
            if (isSolid(below))
            {
                break;
            }

            current = below;
        }

        return current;
    }
}

}
