using System;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

/// <summary>
/// Resolves a stable standing point on a work cell facing an adjacent target.
/// The inset keeps the actor on the supporting floor instead of placing its
/// authoritative pose exactly on a cell boundary.
/// </summary>
public static class WorkSurfacePositioning
{
    public const int EdgeInset = 150;

    public static SurfacePose Resolve(CellId workCell, CellId targetCell)
    {
        int deltaX = targetCell.X - workCell.X;
        int deltaZ = targetCell.Z - workCell.Z;
        int u = SurfacePose.CellCentre;
        int v = SurfacePose.CellCentre;

        if (Math.Abs(deltaX) >= Math.Abs(deltaZ) && deltaX != 0)
        {
            u = deltaX > 0
                ? SurfacePose.UnitsPerCell - EdgeInset
                : EdgeInset;
        }
        else if (deltaZ != 0)
        {
            v = deltaZ > 0
                ? SurfacePose.UnitsPerCell - EdgeInset
                : EdgeInset;
        }

        return new SurfacePose(workCell, SurfaceFace.Floor, u, v);
    }

    public static bool IsAt(SurfacePose actual, SurfacePose required)
    {
        return actual == required;
    }
}

}
