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
    public TunnelPath(IReadOnlyCollection<CellId> cells)
    {
        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (cells.Count == 0)
        {
            throw new ArgumentException("A tunnel path requires at least one cell.", nameof(cells));
        }

        Cells = new ReadOnlyCollection<CellId>(cells.ToArray());
    }

    public IReadOnlyList<CellId> Cells { get; }
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
    private readonly HashSet<CellId> _openCells;
    private readonly HashSet<CellId> _verticalCells;

    public TunnelNavigationVolume(
        int width,
        int height,
        int depth,
        IReadOnlyCollection<CellId> openCells,
        IReadOnlyCollection<CellId> verticalCells,
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
        _openCells = new HashSet<CellId>(openCells);
        _verticalCells = new HashSet<CellId>(verticalCells);
        ValidateCells(_openCells, nameof(openCells));
        ValidateCells(_verticalCells, nameof(verticalCells));
        if (!_verticalCells.IsSubsetOf(_openCells))
        {
            throw new ArgumentException(
                "Vertical tunnel cells must also be open.",
                nameof(verticalCells));
        }

        Cells = new ReadOnlyCollection<CellId>(
            _openCells.OrderBy(cell => cell).ToArray());
        VerticalCells = new ReadOnlyCollection<CellId>(
            _verticalCells.OrderBy(cell => cell).ToArray());
    }

    public int Width { get; }

    public int Height { get; }

    public int Depth { get; }

    public TunnelDemoLayout? DemoLayout { get; }

    public IReadOnlyList<CellId> Cells { get; }

    public IReadOnlyList<CellId> VerticalCells { get; }

    public bool Contains(CellId cell)
    {
        return cell.X >= 0
            && cell.Y >= 0
            && cell.Z >= 0
            && cell.X < Width
            && cell.Y < Height
            && cell.Z < Depth;
    }

    public bool IsOpen(CellId cell)
    {
        return _openCells.Contains(cell);
    }

    public bool IsVerticalTunnel(CellId cell)
    {
        return _verticalCells.Contains(cell);
    }

    public bool CanTraverseStep(CellId from, CellId to)
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

    public TunnelPathResult FindPath(CellId start, CellId goal)
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

        Queue<CellId> frontier = new Queue<CellId>();
        Dictionary<CellId, CellId> previous =
            new Dictionary<CellId, CellId>();
        HashSet<CellId> visited = new HashSet<CellId> { start };
        frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            CellId current = frontier.Dequeue();
            foreach (CellId neighbor in GetNeighbors(current))
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

    private IEnumerable<CellId> GetNeighbors(CellId cell)
    {
        CellId[] candidates =
        {
            new CellId(cell.X - 1, cell.Y, cell.Z),
            new CellId(cell.X + 1, cell.Y, cell.Z),
            new CellId(cell.X, cell.Y, cell.Z - 1),
            new CellId(cell.X, cell.Y, cell.Z + 1),
            new CellId(cell.X, cell.Y - 1, cell.Z),
            new CellId(cell.X, cell.Y + 1, cell.Z),
        };
        for (int index = 0; index < candidates.Length; index++)
        {
            if (CanTraverseStep(cell, candidates[index]))
            {
                yield return candidates[index];
            }
        }
    }

    private static IReadOnlyList<CellId> Reconstruct(
        IReadOnlyDictionary<CellId, CellId> previous,
        CellId start,
        CellId goal)
    {
        List<CellId> reverse = new List<CellId> { goal };
        CellId current = goal;
        while (current != start)
        {
            current = previous[current];
            reverse.Add(current);
        }

        reverse.Reverse();
        return new ReadOnlyCollection<CellId>(reverse);
    }

    private void ValidateCells(IEnumerable<CellId> cells, string parameterName)
    {
        foreach (CellId cell in cells)
        {
            if (!Contains(cell))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}

}
