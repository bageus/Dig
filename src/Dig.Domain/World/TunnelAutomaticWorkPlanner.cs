using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.World
{

public readonly struct TunnelAutomaticWorkSource
{
    public TunnelAutomaticWorkSource(EntityId stackId, CellId cell)
    {
        if (stackId.IsEmpty)
        {
            throw new ArgumentException("Source stack id cannot be empty.", nameof(stackId));
        }

        StackId = stackId;
        Cell = cell;
    }

    public EntityId StackId { get; }

    public CellId Cell { get; }
}

public static class TunnelAutomaticWorkPlanner
{
    public const int AutomaticBuildingRange = 30;

    public static bool IsWithinCompletedBuildingRange(
        CellId target,
        IReadOnlyCollection<CellId> completedBuildingCells)
    {
        if (completedBuildingCells is null)
        {
            throw new ArgumentNullException(nameof(completedBuildingCells));
        }

        return completedBuildingCells.Any(cell =>
            ManhattanDistance(cell, target) <= AutomaticBuildingRange);
    }

    public static TunnelAutomaticWorkSource? SelectSource(
        ItemId requiredItemId,
        CellId target,
        IReadOnlyCollection<ItemStackSnapshot> worldStacks,
        IReadOnlyCollection<CellId> revealedCells,
        IReadOnlyCollection<CellId> reachableCells)
    {
        if (requiredItemId.IsEmpty)
        {
            throw new ArgumentException("Required item id cannot be empty.", nameof(requiredItemId));
        }

        if (worldStacks is null || revealedCells is null || reachableCells is null)
        {
            throw new ArgumentNullException(nameof(worldStacks));
        }

        HashSet<CellId> revealed = revealedCells.ToHashSet();
        HashSet<CellId> reachable = reachableCells.ToHashSet();
        ItemStackSnapshot? source = worldStacks
            .Where(stack => stack.ItemId == requiredItemId
                && stack.Location.Kind == ItemLocationKind.World
                && stack.Location.HasCell
                && stack.AvailableQuantity > 0
                && revealed.Contains(stack.Location.CellId)
                && reachable.Contains(stack.Location.CellId))
            .OrderBy(stack => ManhattanDistance(stack.Location.CellId, target))
            .ThenBy(stack => stack.Location.CellId)
            .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();

        return source is null
            ? (TunnelAutomaticWorkSource?)null
            : new TunnelAutomaticWorkSource(source.StackId, source.Location.CellId);
    }

    public static int ManhattanDistance(CellId left, CellId right)
    {
        return Math.Abs(left.X - right.X)
            + Math.Abs(left.Y - right.Y)
            + Math.Abs(left.Z - right.Z);
    }
}
}
