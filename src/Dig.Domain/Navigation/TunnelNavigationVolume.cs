using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public enum TunnelPathFailureReason
{
    None = 0,
    InvalidStart = 1,
    InvalidGoal = 2,
    BlockedStart = 3,
    BlockedGoal = 4,
    Unreachable = 5,
}

public sealed class TunnelPath
{
    public TunnelPath(IReadOnlyCollection<SpatialCellId> cells)
    {
        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (cells.Count == 0)
        {
            throw new ArgumentException("A tunnel path requires at least one cell.", nameof(cells));
        }

        Cells = new ReadOnlyCollection<SpatialCellId>(cells.ToArray());
    }

    public IReadOnlyList<SpatialCellId> Cells { get; }
}

public sealed class TunnelPathResult
{
    private TunnelPathResult(
        TunnelPath? path,
        TunnelPathFailureReason failureReason,
        string detail)
    {
        Path = path;
        FailureReason = failureReason;
        Detail = detail;
    }

    public bool Succeeded => Path != null;

    public TunnelPath? Path { get; }

    public TunnelPathFailureReason FailureReason { get; }

    public string Detail { get; }

    public static TunnelPathResult Success(TunnelPath path)
    {
        return new TunnelPathResult(
            path ?? throw new ArgumentNullException(nameof(path)),
            TunnelPathFailureReason.None,
            "A deterministic tunnel route was found.");
    }

    public static TunnelPathResult Failure(
        TunnelPathFailureReason reason,
        string detail)
    {
        if (reason == TunnelPathFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new TunnelPathResult(null, reason, detail);
    }
}

public sealed partial class TunnelNavigationVolume
{
    private readonly HashSet<SpatialCellId> _openCells;
    private readonly HashSet<SpatialCellId> _verticalCells;

    public TunnelNavigationVolume(
        int width,
        int height,
        int depth,
        IReadOnlyCollection<SpatialCellId> openCells,
        IReadOnlyCollection<SpatialCellId> verticalCells,
        TunnelDemoLayout? demoLayout = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (openCells is null)
        {
            throw new ArgumentNullException(nameof(openCells));
        }

        if (verticalCells is null)
        {
            throw new ArgumentNullException(nameof(verticalCells));
        }

        Width = width;
        Height = height;
        Depth = depth;
        DemoLayout = demoLayout;
        _openCells = new HashSet<SpatialCellId>(openCells);
        _verticalCells = new HashSet<SpatialCellId>(verticalCells);
        ValidateCells(_openCells, nameof(openCells));
        ValidateCells(_verticalCells, nameof(verticalCells));
        if (!_verticalCells.IsSubsetOf(_openCells))
        {
            throw new ArgumentException(
                "Vertical tunnel cells must also be open.",
                nameof(verticalCells));
        }

        Cells = new ReadOnlyCollection<SpatialCellId>(
            _openCells.OrderBy(cell => cell).ToArray());
        VerticalCells = new ReadOnlyCollection<SpatialCellId>(
            _verticalCells.OrderBy(cell => cell).ToArray());
    }

    public int Width { get; }

    public int Height { get; }

    public int Depth { get; }

    public TunnelDemoLayout? DemoLayout { get; }

    public IReadOnlyList<SpatialCellId> Cells { get; }

    public IReadOnlyList<SpatialCellId> VerticalCells { get; }

    public bool Contains(SpatialCellId cell)
    {
        return cell.X >= 0
            && cell.Y >= 0
            && cell.Z >= 0
            && cell.X < Width
            && cell.Y < Height
            && cell.Z < Depth;
    }

    public bool IsOpen(SpatialCellId cell)
    {
        return _openCells.Contains(cell);
    }

    public bool IsVerticalTunnel(SpatialCellId cell)
    {
        return _verticalCells.Contains(cell);
    }

    public bool CanTraverseStep(SpatialCellId from, SpatialCellId to)
    {
        if (!IsOpen(from) || !IsOpen(to))
        {
            return false;
        }

        int deltaX = Math.Abs(to.X - from.X);
        int deltaY = Math.Abs(to.Y - from.Y);
        int deltaZ = Math.Abs(to.Z - from.Z);
        if (deltaX + deltaY + deltaZ != 1)
        {
            return false;
        }

        if (deltaY == 0)
        {
            return true;
        }

        return deltaX == 0
            && deltaZ == 0
            && IsVerticalTunnel(from)
            && IsVerticalTunnel(to);
    }

    public TunnelPathResult FindPath(SpatialCellId start, SpatialCellId goal)
    {
        if (!Contains(start))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.InvalidStart,
                "The resident start cell is outside the tunnel volume.");
        }

        if (!Contains(goal))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.InvalidGoal,
                "The requested destination is outside the tunnel volume.");
        }

        if (!IsOpen(start))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.BlockedStart,
                "The resident is not standing in an open tunnel cell.");
        }

        if (!IsOpen(goal))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.BlockedGoal,
                "The requested destination is not an open tunnel cell.");
        }

        if (start == goal)
        {
            return TunnelPathResult.Success(new TunnelPath(new[] { start }));
        }

        Queue<SpatialCellId> frontier = new Queue<SpatialCellId>();
        Dictionary<SpatialCellId, SpatialCellId> previous =
            new Dictionary<SpatialCellId, SpatialCellId>();
        HashSet<SpatialCellId> visited = new HashSet<SpatialCellId> { start };
        frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            SpatialCellId current = frontier.Dequeue();
            foreach (SpatialCellId neighbor in GetNeighbors(current))
            {
                if (!visited.Add(neighbor))
                {
                    continue;
                }

                previous.Add(neighbor, current);
                if (neighbor == goal)
                {
                    return TunnelPathResult.Success(
                        new TunnelPath(Reconstruct(previous, start, goal)));
                }

                frontier.Enqueue(neighbor);
            }
        }

        return TunnelPathResult.Failure(
            TunnelPathFailureReason.Unreachable,
            "No route connects the resident to the requested tunnel cell.");
    }

    private IEnumerable<SpatialCellId> GetNeighbors(SpatialCellId cell)
    {
        SpatialCellId[] candidates =
        {
            new SpatialCellId(cell.X - 1, cell.Y, cell.Z),
            new SpatialCellId(cell.X + 1, cell.Y, cell.Z),
            new SpatialCellId(cell.X, cell.Y, cell.Z - 1),
            new SpatialCellId(cell.X, cell.Y, cell.Z + 1),
            new SpatialCellId(cell.X, cell.Y - 1, cell.Z),
            new SpatialCellId(cell.X, cell.Y + 1, cell.Z),
        };
        for (int index = 0; index < candidates.Length; index++)
        {
            if (CanTraverseStep(cell, candidates[index]))
            {
                yield return candidates[index];
            }
        }
    }

    private static IReadOnlyList<SpatialCellId> Reconstruct(
        IReadOnlyDictionary<SpatialCellId, SpatialCellId> previous,
        SpatialCellId start,
        SpatialCellId goal)
    {
        List<SpatialCellId> reverse = new List<SpatialCellId> { goal };
        SpatialCellId current = goal;
        while (current != start)
        {
            current = previous[current];
            reverse.Add(current);
        }

        reverse.Reverse();
        return new ReadOnlyCollection<SpatialCellId>(reverse);
    }

    private void ValidateCells(IEnumerable<SpatialCellId> cells, string parameterName)
    {
        foreach (SpatialCellId cell in cells)
        {
            if (!Contains(cell))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}

}
