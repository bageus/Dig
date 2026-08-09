using System;

namespace Dig.Domain.Navigation
{

/// <summary>
/// Advances an authoritative pose by a bounded deterministic distance. Coarse cells
/// select a legal corridor; they are never used as movement-sized waypoints.
/// </summary>
public static class SurfacePoseSteering
{
    public const int DefaultStepUnits = 200;

    public static SurfacePose MoveTowards(
        SurfacePose current,
        SurfacePose target,
        int maximumStepUnits = DefaultStepUnits)
    {
        if (maximumStepUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStepUnits));
        }

        if (current.Cell != target.Cell || current.Face != target.Face)
        {
            throw new ArgumentException(
                "Continuous steering requires poses on the same surface face.",
                nameof(target));
        }

        int deltaU = target.U - current.U;
        int deltaV = target.V - current.V;
        if (deltaU == 0 && deltaV == 0)
        {
            return target;
        }

        double distance = Math.Sqrt(((long)deltaU * deltaU) + ((long)deltaV * deltaV));
        if (distance <= maximumStepUnits)
        {
            return target;
        }

        double scale = maximumStepUnits / distance;
        int stepU = EnsureProgress(deltaU, (int)(deltaU * scale));
        int stepV = EnsureProgress(deltaV, (int)(deltaV * scale));
        return new SurfacePose(
            current.Cell,
            current.Face,
            current.U + stepU,
            current.V + stepV);
    }

    private static int EnsureProgress(int delta, int step)
    {
        if (delta == 0 || step != 0)
        {
            return step;
        }

        return Math.Sign(delta);
    }
}

}
