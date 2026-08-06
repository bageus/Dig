using System;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

/// <summary>
/// Builds the next authoritative pose for a single vertical coarse-route step.
/// Every face change keeps the same world point; the cell boundary is crossed
/// only after the actor has reached the matching edge of the wall faces.
/// </summary>
public static class VerticalSurfaceSteering
{
    public static bool TryAttachToWall(
        SurfacePose current,
        Func<SurfaceFace, bool> canAttach,
        out SurfacePose wall)
    {
        if (canAttach == null)
        {
            throw new ArgumentNullException(nameof(canAttach));
        }

        if (current.IsVertical
            || !TrySelectAttachmentFace(current, canAttach, out SurfaceFace face))
        {
            wall = default;
            return false;
        }

        wall = AttachToWall(current, face);
        return true;
    }

    public static bool TryBuildNextPose(
        SurfacePose current,
        CellId destination,
        out SurfacePose next,
        out bool crossesCellBoundary)
    {
        return TryBuildNextPose(
            current,
            destination,
            face => face != SurfaceFace.NegativeZ || current.Cell.Z != 0,
            out next,
            out crossesCellBoundary);
    }

    public static bool TryBuildNextPose(
        SurfacePose current,
        CellId destination,
        Func<SurfaceFace, bool> canAttach,
        out SurfacePose next,
        out bool crossesCellBoundary)
    {
        if (canAttach == null)
        {
            throw new ArgumentNullException(nameof(canAttach));
        }

        int deltaY = destination.Y - current.Cell.Y;
        if (destination.X != current.Cell.X
            || destination.Z != current.Cell.Z
            || Math.Abs(deltaY) != 1)
        {
            next = default;
            crossesCellBoundary = false;
            return false;
        }

        SurfaceFace face;
        if (current.IsVertical)
        {
            face = current.Face;
            if (!canAttach(face))
            {
                next = default;
                crossesCellBoundary = false;
                return false;
            }
        }
        else if (!TrySelectAttachmentFace(current, canAttach, out face))
        {
            next = default;
            crossesCellBoundary = false;
            return false;
        }
        if (!current.IsVertical)
        {
            next = AttachToWall(current, face);
            crossesCellBoundary = false;
            return true;
        }

        int exitV = deltaY > 0 ? SurfacePose.UnitsPerCell : 0;
        if (current.V != exitV)
        {
            next = new SurfacePose(current.Cell, face, current.U, exitV);
            crossesCellBoundary = false;
            return true;
        }

        next = new SurfacePose(
            destination,
            face,
            current.U,
            deltaY > 0 ? 0 : SurfacePose.UnitsPerCell);
        crossesCellBoundary = true;
        return true;
    }

    public static bool TryDetachToFloor(SurfacePose current, out SurfacePose floor)
    {
        if (!current.IsVertical)
        {
            floor = default;
            return false;
        }

        int floorU = SurfacePose.CellCentre;
        int floorV = SurfacePose.CellCentre;
        switch (current.Face)
        {
            case SurfaceFace.NegativeX:
                floorU = 0;
                floorV = current.U;
                break;
            case SurfaceFace.PositiveX:
                floorU = SurfacePose.UnitsPerCell;
                floorV = current.U;
                break;
            case SurfaceFace.NegativeZ:
                floorU = current.U;
                floorV = 0;
                break;
            case SurfaceFace.PositiveZ:
                floorU = current.U;
                floorV = SurfacePose.UnitsPerCell;
                break;
        }

        floor = new SurfacePose(current.Cell, SurfaceFace.Floor, floorU, floorV);
        return true;
    }

    private static bool TrySelectAttachmentFace(
        SurfacePose floor,
        Func<SurfaceFace, bool> canAttach,
        out SurfaceFace selected)
    {
        selected = default;
        int distance = int.MaxValue;
        SurfaceFace[] faces =
        {
            SurfaceFace.NegativeX,
            SurfaceFace.PositiveX,
            SurfaceFace.NegativeZ,
            SurfaceFace.PositiveZ,
        };
        int[] distances =
        {
            floor.U,
            SurfacePose.UnitsPerCell - floor.U,
            floor.V,
            SurfacePose.UnitsPerCell - floor.V,
        };
        for (int index = 0; index < faces.Length; index++)
        {
            if (canAttach(faces[index]))
            {
                SelectCloser(distances[index], faces[index], ref distance, ref selected);
            }
        }
        return distance != int.MaxValue;
    }

    private static void SelectCloser(
        int candidateDistance,
        SurfaceFace candidate,
        ref int distance,
        ref SurfaceFace selected)
    {
        if (candidateDistance < distance)
        {
            distance = candidateDistance;
            selected = candidate;
        }
    }

    private static SurfacePose AttachToWall(SurfacePose floor, SurfaceFace face)
    {
        int wallU = face == SurfaceFace.NegativeX || face == SurfaceFace.PositiveX
            ? floor.V
            : floor.U;
        return new SurfacePose(
            floor.Cell,
            face,
            wallU,
            SurfacePose.CellCentre);
    }
}

}
