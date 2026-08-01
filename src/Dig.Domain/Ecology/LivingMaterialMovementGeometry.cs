using System;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public static class LivingMaterialMovementGeometry
{
    public static int ChebyshevDistanceXZ(CellId left, CellId right)
    {
        return Math.Max(
            Math.Abs(left.X - right.X),
            Math.Abs(left.Z - right.Z));
    }

    public static bool IsWithinWanderRadius(
        CellId anchor,
        CellId target,
        int radius)
    {
        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        return target.Y == anchor.Y
            && ChebyshevDistanceXZ(anchor, target) <= radius;
    }

    public static bool IsSingleStepXZ(CellId from, CellId target)
    {
        int deltaX = Math.Abs(target.X - from.X);
        int deltaZ = Math.Abs(target.Z - from.Z);
        return target.Y == from.Y
            && deltaX <= 1
            && deltaZ <= 1
            && deltaX + deltaZ > 0;
    }
}

}
