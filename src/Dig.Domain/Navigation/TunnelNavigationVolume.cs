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

public enum TunnelTraversalKind
{
    Invalid = 0,
    SupportedWalk = 1,
    VerticalClimb = 2,
    ShaftGapTraverse = 3,
    DepthTraverse = 4,
}

public sealed class TunnelPath
{
    public TunnelPath(IReadOnlyCollection<CellId> cells)
        : this(
            cells,
            Enumerable.Repeat(
                TunnelTraversalKind.SupportedWalk,
                Math.Max(0, (cells ?? throw new ArgumentNullException(nameof(cells))).Count - 1))
                .ToArray())
    {
    }

    public TunnelPath(
        IReadOnlyCollection<CellId> cells,
        IReadOnlyCollection<TunnelTraversalKind> traversalKinds)
    {
        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (traversalKinds is null)
        {
            throw new ArgumentNullException(nameof(traversalKinds));
        }

        if (cells.Count == 0)
        {
            throw new ArgumentException(
                "A tunnel path requires at least one cell.",
                nameof(cells));
        }

        if (traversalKinds.Count != cells.Count - 1
            || traversalKinds.Any(value => value == TunnelTraversalKind.Invalid
                || !Enum.IsDefined(typeof(TunnelTraversalKind), value)))
        {
            throw new ArgumentException(
                "Tunnel traversal kinds must describe every path edge.",
                nameof(traversalKinds));
        }

        Cells = new ReadOnlyCollection<CellId>(cells.ToArray());
        TraversalKinds = new ReadOnlyCollection<TunnelTraversalKind>(
            traversalKinds.ToArray());
    }

    public IReadOnlyList<CellId> Cells { get; }

    public IReadOnlyList<TunnelTraversalKind> TraversalKinds { get; }

    public TunnelTraversalKind GetTraversalKind(int fromCellIndex)
    {
        if (fromCellIndex < 0 || fromCellIndex >= TraversalKinds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromCellIndex));
        }

        return TraversalKinds[fromCellIndex];
    }
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
    private readonly HashSet<CellId> _supportedCells;
    private readonly HashSet<CellId> _shaftGapCells;

    public TunnelNavigationVolume(
        int width,
        int height,
        int depth,
        IReadOnlyCollection<CellId> openCells,
        IReadOnlyCollection<CellId> verticalCells,
        TunnelDemoLayout? demoLayout = null)
        : this(
            width,
            height,
            depth,
            openCells,
            verticalCells,
            openCells,
            demoLayout)
    {
    }

    public TunnelNavigationVolume(
        int width,
        int height,
        int depth,
        IReadOnlyCollection<CellId> openCells,
        IReadOnlyCollection<CellId> verticalCells,
        IReadOnlyCollection<CellId> supportedCells,
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

        if (supportedCells is null)
        {
            throw new ArgumentNullException(nameof(supportedCells));
        }

        Width = width;
        Height = height;
        Depth = depth;
        DemoLayout = demoLayout;
        _openCells = new HashSet<CellId>(openCells);
        _verticalCells = new HashSet<CellId>(verticalCells);
        _supportedCells = new HashSet<CellId>(supportedCells);
        ValidateCells(_openCells, nameof(openCells));
        ValidateCells(_verticalCells, nameof(verticalCells));
        ValidateCells(_supportedCells, nameof(supportedCells));
        if (!_verticalCells.IsSubsetOf(_openCells))
        {
            throw new ArgumentException(
                "Vertical tunnel cells must also be open.",
                nameof(verticalCells));
        }

        if (!_supportedCells.IsSubsetOf(_openCells))
        {
            throw new ArgumentException(
                "Supported tunnel cells must also be open.",
                nameof(supportedCells));
        }

        _shaftGapCells = new HashSet<CellId>(
            _openCells.Where(cell => !_supportedCells.Contains(cell)
                && IsVerticalTopologyCell(cell)));

        Cells = new ReadOnlyCollection<CellId>(
            _openCells.OrderBy(cell => cell).ToArray());
        VerticalCells = new ReadOnlyCollection<CellId>(
            _verticalCells.OrderBy(cell => cell).ToArray());
        SupportedCells = new ReadOnlyCollection<CellId>(
            _supportedCells.OrderBy(cell => cell).ToArray());
    }

    public int Width { get; }

    public int Height { get; }

    public int Depth { get; }

    public TunnelDemoLayout? DemoLayout { get; }

    public IReadOnlyList<CellId> Cells { get; }

    public IReadOnlyList<CellId> VerticalCells { get; }

    public IReadOnlyList<CellId> SupportedCells { get; }

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

    public bool HasFullActorSupport(CellId cell)
    {
        return _supportedCells.Contains(cell);
    }

    public bool IsShaftGapCell(CellId cell)
    {
        return _shaftGapCells.Contains(cell);
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
