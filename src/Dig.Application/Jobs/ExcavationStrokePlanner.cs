using System;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public enum ExcavationStrokeAxis
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
}

public readonly struct ExcavationStrokeDecision
{
    public ExcavationStrokeDecision(CellId cell, ExcavationStrokeAxis axis)
    {
        if (!Enum.IsDefined(typeof(ExcavationStrokeAxis), axis))
        {
            throw new ArgumentOutOfRangeException(nameof(axis));
        }

        Cell = cell;
        Axis = axis;
    }

    public CellId Cell { get; }

    public ExcavationStrokeAxis Axis { get; }
}

public sealed class ExcavationStrokePlanner
{
    public ExcavationStrokeDecision Resolve(
        CellId anchor,
        CellId pointer,
        ExcavationStrokeAxis lockedAxis = ExcavationStrokeAxis.None)
    {
        if (!Enum.IsDefined(typeof(ExcavationStrokeAxis), lockedAxis))
        {
            throw new ArgumentOutOfRangeException(nameof(lockedAxis));
        }

        if (anchor.Z != pointer.Z)
        {
            throw new ArgumentException(
                "An excavation stroke cannot cross depth layers.",
                nameof(pointer));
        }

        ExcavationStrokeAxis axis = lockedAxis;
        int horizontalDistance = Math.Abs(pointer.X - anchor.X);
        int verticalDistance = Math.Abs(pointer.Y - anchor.Y);
        if (axis == ExcavationStrokeAxis.None
            && (horizontalDistance > 0 || verticalDistance > 0))
        {
            axis = horizontalDistance >= verticalDistance
                ? ExcavationStrokeAxis.Horizontal
                : ExcavationStrokeAxis.Vertical;
        }

        CellId cell = axis switch
        {
            ExcavationStrokeAxis.Horizontal => new CellId(pointer.X, anchor.Y, anchor.Z),
            ExcavationStrokeAxis.Vertical => new CellId(anchor.X, pointer.Y, anchor.Z),
            _ => anchor,
        };
        return new ExcavationStrokeDecision(cell, axis);
    }
}

}
