using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class TunnelInfrastructureState
{
    public Result RegisterCompletedStoneFloorTrim(CellId cell, long tick)
    {
        ValidateManualTick(tick);
        if (!ContainsHorizontalCell(cell))
        {
            return Result.Failure(TunnelInfrastructureErrors.AnchorOutsideSegment);
        }

        if (!_completedStoneFloorTrimCells.Add(cell))
        {
            return Result.Success();
        }

        Version = checked(Version + 1);
        Raise(new TunnelStoneFloorTrimCompleted(tick, cell));
        return Result.Success();
    }

    private bool ContainsHorizontalCell(CellId cell)
    {
        return _segments.Values.Any(segment =>
            segment.CaptureSnapshot().OrderedHorizontalCells.Contains(cell));
    }

    private void RemoveOrphanedStoneFloorTrimCells(long tick)
    {
        CellId[] removed = _completedStoneFloorTrimCells
            .Where(cell => !ContainsHorizontalCell(cell))
            .OrderBy(cell => cell)
            .ToArray();
        foreach (CellId cell in removed)
        {
            _completedStoneFloorTrimCells.Remove(cell);
            Version = checked(Version + 1);
            Raise(new TunnelStoneFloorTrimCompletionRemoved(tick, cell));
        }
    }

    private static void ValidateManualTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}

public sealed class TunnelStoneFloorTrimCompleted : IDomainEvent
{
    public TunnelStoneFloorTrimCompleted(long tick, CellId cell)
    {
        Tick = tick;
        Cell = cell;
    }

    public long Tick { get; }
    public CellId Cell { get; }
}

public sealed class TunnelStoneFloorTrimCompletionRemoved : IDomainEvent
{
    public TunnelStoneFloorTrimCompletionRemoved(long tick, CellId cell)
    {
        Tick = tick;
        Cell = cell;
    }

    public long Tick { get; }
    public CellId Cell { get; }
}

}
