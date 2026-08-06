using System;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

/// <summary>
/// Builds deterministic boundary poses for one horizontal cell-corridor step.
/// The exit and entry poses resolve to the same world point, so renderers can
/// interpolate without a centre-to-centre teleport while cells remain the
/// coarse navigation authority.
/// </summary>
public static class SurfaceCorridorSteering
{
    public static bool TryBuildBoundaryPoses(
        CellId from,
        CellId to,
        out SurfacePose exit,
        out SurfacePose entry)
    {
        int deltaX = to.X - from.X;
        int deltaY = to.Y - from.Y;
        int deltaZ = to.Z - from.Z;
        if (deltaY != 0 || Math.Abs(deltaX) + Math.Abs(deltaZ) != 1)
        {
            exit = default;
            entry = default;
            return false;
        }

        int exitU = SurfacePose.CellCentre;
        int exitV = SurfacePose.CellCentre;
        int entryU = SurfacePose.CellCentre;
        int entryV = SurfacePose.CellCentre;
        if (deltaX != 0)
        {
            exitU = deltaX > 0 ? SurfacePose.UnitsPerCell : 0;
            entryU = deltaX > 0 ? 0 : SurfacePose.UnitsPerCell;
        }
        else
        {
            exitV = deltaZ > 0 ? SurfacePose.UnitsPerCell : 0;
            entryV = deltaZ > 0 ? 0 : SurfacePose.UnitsPerCell;
        }

        exit = new SurfacePose(from, SurfaceFace.Floor, exitU, exitV);
        entry = new SurfacePose(to, SurfaceFace.Floor, entryU, entryV);
        return true;
    }
}

}
