using System;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public enum SurfaceFace
{
    Floor = 0,
    NegativeX = 1,
    PositiveX = 2,
    NegativeZ = 3,
    PositiveZ = 4,
}

public enum SurfaceMoverKind
{
    Resident = 0,
    CaveMonster = 1,
    Spider = 2,
    GroundEnemy = 3,
    Hamster = 4,
    Worm = 5,
}

/// <summary>
/// A deterministic position on one exposed voxel face. Coordinates use thousandths
/// of a cell so simulation state never depends on floating point rounding.
/// </summary>
public readonly struct SurfacePose : IEquatable<SurfacePose>
{
    public const int UnitsPerCell = 1_000;
    public const int CellCentre = UnitsPerCell / 2;

    public SurfacePose(CellId cell, SurfaceFace face, int u, int v)
    {
        if (!Enum.IsDefined(typeof(SurfaceFace), face))
        {
            throw new ArgumentOutOfRangeException(nameof(face));
        }

        if (u < 0 || u > UnitsPerCell || v < 0 || v > UnitsPerCell)
        {
            throw new ArgumentOutOfRangeException(
                nameof(u),
                "Surface coordinates must stay inside the selected voxel face.");
        }

        Cell = cell;
        Face = face;
        U = u;
        V = v;
    }

    public CellId Cell { get; }
    public SurfaceFace Face { get; }
    public int U { get; }
    public int V { get; }
    public bool IsVertical => Face != SurfaceFace.Floor;

    public static SurfacePose FloorCentre(CellId cell)
    {
        return new SurfacePose(cell, SurfaceFace.Floor, CellCentre, CellCentre);
    }

    public static SurfacePose FloorPoint(CellId cell, double offsetX, double offsetZ = 0d)
    {
        return new SurfacePose(
            cell,
            SurfaceFace.Floor,
            ToSurfaceCoordinate(offsetX),
            ToSurfaceCoordinate(offsetZ));
    }

    private static int ToSurfaceCoordinate(double centreOffset)
    {
        if (double.IsNaN(centreOffset) || double.IsInfinity(centreOffset)
            || centreOffset < -0.5d || centreOffset > 0.5d)
        {
            throw new ArgumentOutOfRangeException(nameof(centreOffset));
        }

        return (int)Math.Round(
            CellCentre + (centreOffset * UnitsPerCell),
            MidpointRounding.AwayFromZero);
    }

    public bool Equals(SurfacePose other)
    {
        return Cell == other.Cell
            && Face == other.Face
            && U == other.U
            && V == other.V;
    }

    public override bool Equals(object? obj)
    {
        return obj is SurfacePose other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Cell.GetHashCode();
            hash = (hash * 397) ^ (int)Face;
            hash = (hash * 397) ^ U;
            return (hash * 397) ^ V;
        }
    }

    public static bool operator ==(SurfacePose left, SurfacePose right) => left.Equals(right);
    public static bool operator !=(SurfacePose left, SurfacePose right) => !left.Equals(right);
}

public static class SurfaceTraversalPolicy
{
    public static bool CanUse(SurfaceMoverKind mover, SurfacePose pose)
    {
        if (pose.Face == SurfaceFace.Floor)
        {
            return true;
        }

        if (!CanClimb(mover))
        {
            return false;
        }

        // Z0 is the open front boundary of the playable volume, not a climbable wall.
        return pose.Face != SurfaceFace.NegativeZ || pose.Cell.Z != 0;
    }

    public static bool CanClimb(SurfaceMoverKind mover)
    {
        return mover == SurfaceMoverKind.Resident
            || mover == SurfaceMoverKind.CaveMonster
            || mover == SurfaceMoverKind.Spider;
    }
}

public static class SurfaceSpatialMath
{
    public static int DefaultClearanceUnits => 300;

    public static long DistanceSquared(SurfacePose left, SurfacePose right)
    {
        ResolveWorldPosition(left, out int leftX, out int leftY, out int leftZ);
        ResolveWorldPosition(right, out int rightX, out int rightY, out int rightZ);
        long deltaX = leftX - (long)rightX;
        long deltaY = leftY - (long)rightY;
        long deltaZ = leftZ - (long)rightZ;
        return (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
    }

    public static bool HasClearance(
        SurfacePose left,
        SurfacePose right,
        int clearanceUnits)
    {
        if (clearanceUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clearanceUnits));
        }

        long required = clearanceUnits;
        return DistanceSquared(left, right) >= required * required;
    }

    private static void ResolveWorldPosition(
        SurfacePose pose,
        out int x,
        out int y,
        out int z)
    {
        x = checked((pose.Cell.X * SurfacePose.UnitsPerCell) + SurfacePose.CellCentre);
        y = checked((pose.Cell.Y * SurfacePose.UnitsPerCell) + SurfacePose.CellCentre);
        z = checked((pose.Cell.Z * SurfacePose.UnitsPerCell) + SurfacePose.CellCentre);
        switch (pose.Face)
        {
            case SurfaceFace.Floor:
                x += pose.U - SurfacePose.CellCentre;
                z += pose.V - SurfacePose.CellCentre;
                break;
            case SurfaceFace.NegativeX:
                x -= SurfacePose.CellCentre;
                z += pose.U - SurfacePose.CellCentre;
                y += pose.V - SurfacePose.CellCentre;
                break;
            case SurfaceFace.PositiveX:
                x += SurfacePose.CellCentre;
                z += pose.U - SurfacePose.CellCentre;
                y += pose.V - SurfacePose.CellCentre;
                break;
            case SurfaceFace.NegativeZ:
                z -= SurfacePose.CellCentre;
                x += pose.U - SurfacePose.CellCentre;
                y += pose.V - SurfacePose.CellCentre;
                break;
            case SurfaceFace.PositiveZ:
                z += SurfacePose.CellCentre;
                x += pose.U - SurfacePose.CellCentre;
                y += pose.V - SurfacePose.CellCentre;
                break;
        }
    }
}

}
