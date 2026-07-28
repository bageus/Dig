using System;
using Dig.Domain.World;

namespace Dig.Application.World
{

public readonly struct CaveRoomExcavationTarget : IEquatable<CaveRoomExcavationTarget>
{
    public CaveRoomExcavationTarget(CellId cell, ExcavationQuarter requiredQuarters)
    {
        int value = (int)requiredQuarters;
        if (requiredQuarters == ExcavationQuarter.None
            || (value & ~(int)ExcavationQuarter.All) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredQuarters));
        }

        Cell = cell;
        RequiredQuarters = requiredQuarters;
    }

    public CellId Cell { get; }
    public ExcavationQuarter RequiredQuarters { get; }
    public bool IsFullCell => RequiredQuarters == ExcavationQuarter.All;

    public bool Equals(CaveRoomExcavationTarget other)
    {
        return Cell == other.Cell && RequiredQuarters == other.RequiredQuarters;
    }

    public override bool Equals(object? obj)
    {
        return obj is CaveRoomExcavationTarget other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Cell, RequiredQuarters);
    }
}

}
