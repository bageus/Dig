using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public enum RoomTemporaryStockCellPlanStatus
{
    Assigned = 0,
    Retained = 1,
    BlockedNoFreeReachableCell = 2,
}

public sealed class RoomTemporaryStockCellPlan
{
    public RoomTemporaryStockCellPlan(
        RoomTemporaryStockCellPlanStatus status,
        CellId? cell)
    {
        if (status == RoomTemporaryStockCellPlanStatus.BlockedNoFreeReachableCell
            && cell.HasValue)
        {
            throw new ArgumentException("A blocked stock-cell plan cannot contain a cell.", nameof(cell));
        }

        if (status != RoomTemporaryStockCellPlanStatus.BlockedNoFreeReachableCell
            && !cell.HasValue)
        {
            throw new ArgumentException("An assigned stock-cell plan requires a cell.", nameof(cell));
        }

        Status = status;
        Cell = cell;
    }

    public RoomTemporaryStockCellPlanStatus Status { get; }
    public CellId? Cell { get; }
}

public sealed class RoomTemporaryStockCellPlanner
{
    public RoomTemporaryStockCellPlan Plan(
        CompletedRoomInfrastructureProvenance room,
        WorldSnapshot world,
        IEnumerable<CellId> reachableCells,
        IEnumerable<CellId> occupiedCells,
        CellId? retainedCell = null)
    {
        if (room == null)
        {
            throw new ArgumentNullException(nameof(room));
        }

        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (reachableCells == null || occupiedCells == null)
        {
            throw new ArgumentNullException(nameof(reachableCells));
        }

        HashSet<CellId> roomCells = room.OrderedRoomCells.ToHashSet();
        Dictionary<CellId, CellSnapshot> worldCells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> reachable = reachableCells.ToHashSet();
        HashSet<CellId> occupied = occupiedCells.ToHashSet();
        if (retainedCell.HasValue
            && roomCells.Contains(retainedCell.Value)
            && IsCandidate(retainedCell.Value, worldCells, reachable, occupied))
        {
            return new RoomTemporaryStockCellPlan(
                RoomTemporaryStockCellPlanStatus.Retained,
                retainedCell.Value);
        }

        int centerX2 = room.OrderedRoomCells.Min(cell => cell.X)
            + room.OrderedRoomCells.Max(cell => cell.X);
        int centerY2 = room.OrderedRoomCells.Min(cell => cell.Y)
            + room.OrderedRoomCells.Max(cell => cell.Y);
        int centerZ2 = room.OrderedRoomCells.Min(cell => cell.Z)
            + room.OrderedRoomCells.Max(cell => cell.Z);
        CellId? selected = room.OrderedRoomCells
            .Where(cell => IsCandidate(cell, worldCells, reachable, occupied))
            .OrderBy(cell => DistanceToDoubledCenter(
                cell,
                centerX2,
                centerY2,
                centerZ2))
            .ThenBy(cell => cell)
            .Cast<CellId?>()
            .FirstOrDefault();
        return selected.HasValue
            ? new RoomTemporaryStockCellPlan(
                RoomTemporaryStockCellPlanStatus.Assigned,
                selected.Value)
            : new RoomTemporaryStockCellPlan(
                RoomTemporaryStockCellPlanStatus.BlockedNoFreeReachableCell,
                cell: null);
    }

    private static bool IsCandidate(
        CellId cell,
        IReadOnlyDictionary<CellId, CellSnapshot> world,
        HashSet<CellId> reachable,
        HashSet<CellId> occupied)
    {
        return reachable.Contains(cell)
            && !occupied.Contains(cell)
            && world.TryGetValue(cell, out CellSnapshot snapshot)
            && (!snapshot.IsSolid || snapshot.State.IsExcavationOpen);
    }

    private static long DistanceToDoubledCenter(
        CellId cell,
        int centerX2,
        int centerY2,
        int centerZ2)
    {
        return Math.Abs(((long)cell.X * 2L) - centerX2)
            + Math.Abs(((long)cell.Y * 2L) - centerY2)
            + Math.Abs(((long)cell.Z * 2L) - centerZ2);
    }
}

}
