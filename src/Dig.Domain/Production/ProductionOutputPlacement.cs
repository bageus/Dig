using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Production
{

public static partial class ProductionErrors
{
    public static readonly DomainError OutputSpaceUnavailable = new DomainError(
        "production.output_space_unavailable",
        "No free supported output cell is available in the workstation's right-side zone.");
}

public static class ProductionOutputPlacement
{
    public static Result<CellId> Resolve(
        BuildingSnapshot building,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> occupiedBuildingCells,
        IReadOnlyCollection<ItemStackSnapshot> inventoryStacks,
        int maximumLateralDistance = int.MaxValue)
    {
        Result<IReadOnlyList<CellId>> resolved = ResolveMany(
            building,
            world,
            occupiedBuildingCells,
            inventoryStacks,
            requiredCount: 1,
            maximumLateralDistance: maximumLateralDistance);
        return resolved.IsSuccess
            ? Result<CellId>.Success(resolved.Value[0])
            : Result<CellId>.Failure(resolved.Error!);
    }

    public static Result<IReadOnlyList<CellId>> ResolveMany(
        BuildingSnapshot building,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> occupiedBuildingCells,
        IReadOnlyCollection<ItemStackSnapshot> inventoryStacks,
        int requiredCount,
        int maximumLateralDistance = int.MaxValue)
    {
        if (building is null)
        {
            throw new ArgumentNullException(nameof(building));
        }

        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (occupiedBuildingCells is null)
        {
            throw new ArgumentNullException(nameof(occupiedBuildingCells));
        }

        if (inventoryStacks is null)
        {
            throw new ArgumentNullException(nameof(inventoryStacks));
        }

        if (maximumLateralDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLateralDistance));
        }

        if (requiredCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredCount));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> occupied = occupiedBuildingCells.ToHashSet();
        HashSet<CellId> itemCells = inventoryStacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.World
                && stack.Location.HasCell
                && stack.Quantity > 0)
            .Select(stack => stack.Location.CellId)
            .ToHashSet();

        int rightEdgeX = building.Footprint.Count == 0
            ? building.Origin.X
            : building.Footprint.Max(cell => cell.X);
        int worldMaximumDistance = Math.Max(0, world.Size.Width - rightEdgeX - 1);
        int effectiveMaximumDistance = Math.Min(
            maximumLateralDistance,
            Math.Max(0, worldMaximumDistance - 1));

        List<CellId> resolvedCells = new List<CellId>(requiredCount);
        foreach (CellId candidate in CreateCandidates(building, effectiveMaximumDistance))
        {
            CellId supportCell = new CellId(
                candidate.X,
                candidate.Y + 1,
                candidate.Z);
            if (!world.Size.Contains(candidate)
                || !world.Size.Contains(supportCell)
                || !cells.TryGetValue(candidate, out CellSnapshot snapshot)
                || !cells.TryGetValue(supportCell, out CellSnapshot support)
                || snapshot.IsSolid
                || !snapshot.State.IsExplored
                || !support.IsSolid
                || !support.State.IsExplored
                || occupied.Contains(candidate)
                || itemCells.Contains(candidate))
            {
                continue;
            }

            resolvedCells.Add(candidate);
            itemCells.Add(candidate);
            if (resolvedCells.Count == requiredCount)
            {
                return Result<IReadOnlyList<CellId>>.Success(resolvedCells);
            }
        }

        return Result<IReadOnlyList<CellId>>.Failure(
            ProductionErrors.OutputSpaceUnavailable);
    }

    public static IReadOnlyList<CellId> CreateCandidates(
        BuildingSnapshot building,
        int maximumLateralDistance)
    {
        if (building is null)
        {
            throw new ArgumentNullException(nameof(building));
        }

        if (maximumLateralDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLateralDistance));
        }

        if (building.Footprint.Count == 0)
        {
            return Array.Empty<CellId>();
        }

        int rightEdgeX = building.Footprint.Max(cell => cell.X);
        (int Y, int Z)[] rows = building.Footprint
            .Select(cell => (cell.Y, cell.Z))
            .Distinct()
            .OrderBy(value => Math.Abs(value.Y - building.Origin.Y))
            .ThenBy(value => Math.Abs(value.Z - building.Origin.Z))
            .ThenBy(value => value.Y)
            .ThenBy(value => value.Z)
            .ToArray();
        List<CellId> candidates = new List<CellId>(
            checked((maximumLateralDistance + 1) * rows.Length));

        for (int distance = 1; distance <= maximumLateralDistance + 1; distance++)
        {
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                candidates.Add(new CellId(
                    rightEdgeX + distance,
                    rows[rowIndex].Y,
                    rows[rowIndex].Z));
            }
        }

        return candidates;
    }
}

}
