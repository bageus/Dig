using System;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{
public sealed partial class TunnelNavigationVolume
{
    public TunnelTraversalKind ClassifyTraversal(CellId from, CellId to)
    {
        if (!IsOpen(from) || !IsOpen(to))
        {
            return TunnelTraversalKind.Invalid;
        }

        int deltaX = Math.Abs(to.X - from.X);
        int deltaY = Math.Abs(to.Y - from.Y);
        int deltaZ = Math.Abs(to.Z - from.Z);
        if (deltaY == 0 && deltaX == 1 && deltaZ == 1)
        {
            CellId acrossX = new CellId(to.X, from.Y, from.Z);
            CellId acrossZ = new CellId(from.X, from.Y, to.Z);
            return ClassifyCardinalTraversal(from, acrossX) != TunnelTraversalKind.Invalid
                && ClassifyCardinalTraversal(from, acrossZ) != TunnelTraversalKind.Invalid
                && ClassifyCardinalTraversal(acrossX, to) != TunnelTraversalKind.Invalid
                && ClassifyCardinalTraversal(acrossZ, to) != TunnelTraversalKind.Invalid
                    ? TunnelTraversalKind.SupportedWalk
                    : TunnelTraversalKind.Invalid;
        }

        return ClassifyCardinalTraversal(from, to);
    }

    public bool CanTraverseStep(CellId from, CellId to)
    {
        return ClassifyTraversal(from, to) != TunnelTraversalKind.Invalid;
    }

    private TunnelTraversalKind ClassifyCardinalTraversal(CellId from, CellId to)
    {
        if (!IsOpen(from) || !IsOpen(to))
        {
            return TunnelTraversalKind.Invalid;
        }

        int deltaX = Math.Abs(to.X - from.X);
        int deltaY = Math.Abs(to.Y - from.Y);
        int deltaZ = Math.Abs(to.Z - from.Z);
        if (deltaX + deltaY + deltaZ != 1)
        {
            return TunnelTraversalKind.Invalid;
        }

        if (deltaY != 0)
        {
            return deltaX == 0
                && deltaZ == 0
                && (IsVerticalTunnel(from) || IsVerticalTunnel(to))
                    ? TunnelTraversalKind.VerticalClimb
                    : TunnelTraversalKind.Invalid;
        }

        if (deltaZ != 0)
        {
            return TunnelTraversalKind.DepthTraverse;
        }

        bool crossesShaftGap = IsShaftGapCell(from) || IsShaftGapCell(to);
        return crossesShaftGap
            ? TunnelTraversalKind.ShaftGapTraverse
            : TunnelTraversalKind.SupportedWalk;
    }

    private bool IsVerticalTopologyCell(CellId cell)
    {
        if (_verticalCells.Contains(cell))
        {
            return true;
        }

        CellId above = new CellId(cell.X, cell.Y - 1, cell.Z);
        CellId below = new CellId(cell.X, cell.Y + 1, cell.Z);
        return (Contains(above) && _verticalCells.Contains(above))
            || (Contains(below) && _verticalCells.Contains(below));
    }
}
}
