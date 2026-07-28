using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class CaveRoomShellProtectionPolicy
{
    public IReadOnlyList<CellId> Resolve(CaveRoomPlan plan, WorldSize worldSize)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        HashSet<CellId> protectedCells = new HashSet<CellId>(plan.RoofCells);
        for (int level = 0; level < plan.Preset.Height; level++)
        {
            int y = plan.Entrance.Y - level;
            CaveRoomRowProfile profile = CaveRoomPlanner.ResolveRowProfile(
                plan.Preset,
                plan.Entrance.X,
                level);
            int leftShellX = profile.RequiredQuarters(profile.MinCellX)
                    == ExcavationQuarter.All
                ? profile.MinCellX - 1
                : profile.MinCellX;
            int rightShellX = profile.RequiredQuarters(profile.MaxCellX)
                    == ExcavationQuarter.All
                ? profile.MaxCellX + 1
                : profile.MaxCellX;
            AddIfContained(protectedCells, worldSize, leftShellX, y);
            AddIfContained(protectedCells, worldSize, rightShellX, y);
        }

        int floorY = plan.Entrance.Y + 1;
        CaveRoomRowProfile baseProfile = CaveRoomPlanner.ResolveRowProfile(
            plan.Preset,
            plan.Entrance.X,
            level: 0);
        foreach (int x in baseProfile.RequiredQuartersByX.Keys)
        {
            AddIfContained(protectedCells, worldSize, x, floorY);
        }

        return protectedCells.OrderBy(cell => cell).ToArray();
    }

    private static void AddIfContained(
        ISet<CellId> cells,
        WorldSize size,
        int x,
        int y)
    {
        if (x >= 0 && y >= 0 && x < size.Width && y < size.Height)
        {
            cells.Add(new CellId(x, y, 0));
        }
    }
}

}