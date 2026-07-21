using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Presentation.Navigation
{

public readonly struct TunnelMovementTarget
{
    public TunnelMovementTarget(SpatialCellId cell, double offsetX)
    {
        if (offsetX < -TunnelMovementTargetResolver.MaximumOffsetX
            || offsetX > TunnelMovementTargetResolver.MaximumOffsetX)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetX));
        }

        Cell = cell;
        OffsetX = offsetX;
    }

    public SpatialCellId Cell { get; }

    public double OffsetX { get; }
}

public sealed class TunnelMovementTargetResolver
{
    public const double MaximumOffsetX = 0.44d;

    public TunnelMovementTarget Resolve(
        IReadOnlyList<SpatialCellId> candidates,
        double logicalX,
        double logicalY)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (candidates.Count == 0)
        {
            throw new ArgumentException(
                "A movement surface requires at least one hidden navigation cell.",
                nameof(candidates));
        }

        SpatialCellId best = candidates[0];
        double bestDistance = DistanceSquared(best, logicalX, logicalY);
        for (int index = 1; index < candidates.Count; index++)
        {
            SpatialCellId candidate = candidates[index];
            double distance = DistanceSquared(candidate, logicalX, logicalY);
            if (distance < bestDistance
                || (Math.Abs(distance - bestDistance) < 0.000001d
                    && candidate.CompareTo(best) < 0))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        double offset = Math.Max(
            -MaximumOffsetX,
            Math.Min(MaximumOffsetX, logicalX - best.X));
        return new TunnelMovementTarget(best, offset);
    }

    private static double DistanceSquared(
        SpatialCellId cell,
        double logicalX,
        double logicalY)
    {
        double deltaX = logicalX - cell.X;
        double deltaY = logicalY - cell.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }
}

}
