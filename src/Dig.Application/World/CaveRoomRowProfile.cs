using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class CaveRoomRowProfile
{
    private readonly IReadOnlyDictionary<int, ExcavationQuarter> _requiredByX;

    internal CaveRoomRowProfile(
        int level,
        int width,
        int leftBoundary2,
        int rightBoundary2,
        IReadOnlyDictionary<int, ExcavationQuarter> requiredByX)
    {
        if (level < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (width <= 0 || rightBoundary2 - leftBoundary2 != width * 2)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (requiredByX == null || requiredByX.Count == 0)
        {
            throw new ArgumentException("A room row requires terrain cells.", nameof(requiredByX));
        }

        Level = level;
        Width = width;
        LeftBoundary2 = leftBoundary2;
        RightBoundary2 = rightBoundary2;
        Dictionary<int, ExcavationQuarter> copy =
            new Dictionary<int, ExcavationQuarter>(requiredByX);
        _requiredByX = new ReadOnlyDictionary<int, ExcavationQuarter>(copy);
        int min = int.MaxValue;
        int max = int.MinValue;
        foreach (int x in copy.Keys)
        {
            min = Math.Min(min, x);
            max = Math.Max(max, x);
        }
        MinCellX = min;
        MaxCellX = max;
    }

    public int Level { get; }
    public int Width { get; }
    public int LeftBoundary2 { get; }
    public int RightBoundary2 { get; }
    public float LeftBoundary => LeftBoundary2 * 0.5f;
    public float RightBoundary => RightBoundary2 * 0.5f;
    public int MinCellX { get; }
    public int MaxCellX { get; }
    public IReadOnlyDictionary<int, ExcavationQuarter> RequiredQuartersByX => _requiredByX;

    public ExcavationQuarter RequiredQuarters(int x)
    {
        return _requiredByX.TryGetValue(x, out ExcavationQuarter value)
            ? value
            : ExcavationQuarter.None;
    }
}

}
