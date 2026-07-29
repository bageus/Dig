using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public static class CombatSpatialMath
{
    public static int Distance3D(CellId first, CellId second)
    {
        return checked(
            Math.Abs(first.X - second.X)
            + Math.Abs(first.Y - second.Y)
            + Math.Abs(first.Z - second.Z));
    }
}

public static class CombatLineOfSightResolver
{
    public static bool HasLineOfSight(
        CellId source,
        CellId target,
        Func<CellId, bool> isSolid)
    {
        if (isSolid is null)
        {
            throw new ArgumentNullException(nameof(isSolid));
        }

        IReadOnlyList<CellId> traced = Trace(source, target);
        for (int index = 0; index < traced.Count - 1; index++)
        {
            if (isSolid(traced[index]))
            {
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyList<CellId> Trace(CellId source, CellId target)
    {
        if (source == target)
        {
            return Array.Empty<CellId>();
        }

        List<CellId> cells = new List<CellId>();
        int x = source.X;
        int y = source.Y;
        int z = source.Z;
        int deltaX = Math.Abs(target.X - x);
        int deltaY = Math.Abs(target.Y - y);
        int deltaZ = Math.Abs(target.Z - z);
        int stepX = Math.Sign(target.X - x);
        int stepY = Math.Sign(target.Y - y);
        int stepZ = Math.Sign(target.Z - z);

        if (deltaX >= deltaY && deltaX >= deltaZ)
        {
            TraceAlongX(cells, target, ref x, ref y, ref z,
                deltaX, deltaY, deltaZ, stepX, stepY, stepZ);
        }
        else if (deltaY >= deltaX && deltaY >= deltaZ)
        {
            TraceAlongY(cells, target, ref x, ref y, ref z,
                deltaX, deltaY, deltaZ, stepX, stepY, stepZ);
        }
        else
        {
            TraceAlongZ(cells, target, ref x, ref y, ref z,
                deltaX, deltaY, deltaZ, stepX, stepY, stepZ);
        }

        return new ReadOnlyCollection<CellId>(cells);
    }

    private static void TraceAlongX(
        List<CellId> cells,
        CellId target,
        ref int x,
        ref int y,
        ref int z,
        int deltaX,
        int deltaY,
        int deltaZ,
        int stepX,
        int stepY,
        int stepZ)
    {
        int errorY = 2 * deltaY - deltaX;
        int errorZ = 2 * deltaZ - deltaX;
        while (x != target.X)
        {
            x += stepX;
            if (errorY >= 0)
            {
                y += stepY;
                errorY -= 2 * deltaX;
            }

            if (errorZ >= 0)
            {
                z += stepZ;
                errorZ -= 2 * deltaX;
            }

            errorY += 2 * deltaY;
            errorZ += 2 * deltaZ;
            cells.Add(new CellId(x, y, z));
        }
    }

    private static void TraceAlongY(
        List<CellId> cells,
        CellId target,
        ref int x,
        ref int y,
        ref int z,
        int deltaX,
        int deltaY,
        int deltaZ,
        int stepX,
        int stepY,
        int stepZ)
    {
        int errorX = 2 * deltaX - deltaY;
        int errorZ = 2 * deltaZ - deltaY;
        while (y != target.Y)
        {
            y += stepY;
            if (errorX >= 0)
            {
                x += stepX;
                errorX -= 2 * deltaY;
            }

            if (errorZ >= 0)
            {
                z += stepZ;
                errorZ -= 2 * deltaY;
            }

            errorX += 2 * deltaX;
            errorZ += 2 * deltaZ;
            cells.Add(new CellId(x, y, z));
        }
    }

    private static void TraceAlongZ(
        List<CellId> cells,
        CellId target,
        ref int x,
        ref int y,
        ref int z,
        int deltaX,
        int deltaY,
        int deltaZ,
        int stepX,
        int stepY,
        int stepZ)
    {
        int errorX = 2 * deltaX - deltaZ;
        int errorY = 2 * deltaY - deltaZ;
        while (z != target.Z)
        {
            z += stepZ;
            if (errorX >= 0)
            {
                x += stepX;
                errorX -= 2 * deltaZ;
            }

            if (errorY >= 0)
            {
                y += stepY;
                errorY -= 2 * deltaZ;
            }

            errorX += 2 * deltaX;
            errorY += 2 * deltaY;
            cells.Add(new CellId(x, y, z));
        }
    }
}

public static class CombatEngagementResolver
{
    public static CombatEngagementCandidate? Select(
        WeaponProfile weapon,
        IEnumerable<CombatEngagementCandidate> candidates)
    {
        if (weapon is null)
        {
            throw new ArgumentNullException(nameof(weapon));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return candidates
            .Where(candidate => candidate.Reachable && candidate.Supported)
            .Where(candidate => candidate.DistanceToTarget >= weapon.MinimumRange
                && candidate.DistanceToTarget <= weapon.MaximumRange)
            .Where(candidate => weapon.SpatialMode == CombatAttackSpatialMode.Melee
                ? candidate.HasImmediateTraversalEdge
                : candidate.HasLineOfSight)
            .OrderBy(candidate => candidate.SoftClaimCount)
            .ThenBy(candidate => candidate.RouteCost)
            .ThenBy(candidate => candidate.DistanceToTarget)
            .ThenBy(candidate => candidate.Cell)
            .Select(candidate => (CombatEngagementCandidate?)candidate)
            .FirstOrDefault();
    }
}

public static class CombatRetreatResolver
{
    public static CombatRetreatCandidate? Select(
        int currentMinimumThreatDistance,
        IEnumerable<CombatRetreatCandidate> candidates)
    {
        if (currentMinimumThreatDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentMinimumThreatDistance));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return candidates
            .Where(candidate => candidate.Reachable
                && candidate.Supported
                && candidate.MinimumThreatDistance > currentMinimumThreatDistance)
            .OrderByDescending(candidate => candidate.MinimumThreatDistance)
            .ThenByDescending(candidate => candidate.OwnTerritory)
            .ThenBy(candidate => candidate.RouteCost)
            .ThenBy(candidate => candidate.Cell)
            .Select(candidate => (CombatRetreatCandidate?)candidate)
            .FirstOrDefault();
    }
}
}
