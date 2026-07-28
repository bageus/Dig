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
        "No free explored output cell is available around the workstation.");
}

public static class ProductionOutputPlacement
{
    public static Result<CellId> Resolve(
        BuildingSnapshot building,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> occupiedBuildingCells,
        IReadOnlyCollection<ItemStackSnapshot> inventoryStacks,
        int maximumLateralDistance = 6)
    {
        if (building is null || world is null
            || occupiedBuildingCells is null || inventoryStacks is null)
        {
            throw new ArgumentNullException(nameof(building));
        }

        if (maximumLateralDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLateralDistance));
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
        foreach (CellId candidate in CreateCandidates(building, maximumLateralDistance))
        {
            if (!world.Size.Contains(candidate)
                || !cells.TryGetValue(candidate, out CellSnapshot snapshot)
                || snapshot.IsSolid
                || !snapshot.State.IsExplored
                || occupied.Contains(candidate)
                || itemCells.Contains(candidate))
            {
                continue;
            }

            return Result<CellId>.Success(candidate);
        }

        return Result<CellId>.Failure(ProductionErrors.OutputSpaceUnavailable);
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

        (int forwardX, int forwardY, int lateralX, int lateralY) =
            ResolveAxes(building.Orientation);
        int minimumX = building.Footprint.Min(cell => cell.X);
        int maximumX = building.Footprint.Max(cell => cell.X);
        int minimumY = building.Footprint.Min(cell => cell.Y);
        int maximumY = building.Footprint.Max(cell => cell.Y);
        HashSet<CellId> footprint = building.Footprint.ToHashSet();
        List<PlacementCandidate> candidates = new List<PlacementCandidate>();

        for (int ring = 1; ring <= maximumLateralDistance + 1; ring++)
        {
            int outerMinimumX = minimumX - ring;
            int outerMaximumX = maximumX + ring;
            int outerMinimumY = minimumY - ring;
            int outerMaximumY = maximumY + ring;
            for (int y = outerMinimumY; y <= outerMaximumY; y++)
            {
                for (int x = outerMinimumX; x <= outerMaximumX; x++)
                {
                    bool boundary = x == outerMinimumX
                        || x == outerMaximumX
                        || y == outerMinimumY
                        || y == outerMaximumY;
                    if (!boundary)
                    {
                        continue;
                    }

                    CellId cell = new CellId(x, y, building.Origin.Z);
                    if (footprint.Contains(cell))
                    {
                        continue;
                    }

                    int relativeX = x - building.Origin.X;
                    int relativeY = y - building.Origin.Y;
                    int forward = relativeX * forwardX + relativeY * forwardY;
                    int lateral = relativeX * lateralX + relativeY * lateralY;
                    candidates.Add(new PlacementCandidate(
                        cell,
                        ring,
                        forward,
                        lateral));
                }
            }
        }

        return candidates
            .OrderBy(value => value.Ring)
            .ThenByDescending(value => value.Forward)
            .ThenBy(value => Math.Abs(value.Lateral))
            .ThenBy(value => value.Lateral)
            .ThenBy(value => value.Cell.Y)
            .ThenBy(value => value.Cell.X)
            .Select(value => value.Cell)
            .ToArray();
    }

    private static (int ForwardX, int ForwardY, int LateralX, int LateralY)
        ResolveAxes(BuildingOrientation orientation)
    {
        return orientation switch
        {
            BuildingOrientation.North => (0, -1, 1, 0),
            BuildingOrientation.East => (1, 0, 0, 1),
            BuildingOrientation.South => (0, 1, 1, 0),
            BuildingOrientation.West => (-1, 0, 0, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
        };
    }

    private readonly struct PlacementCandidate
    {
        internal PlacementCandidate(CellId cell, int ring, int forward, int lateral)
        {
            Cell = cell;
            Ring = ring;
            Forward = forward;
            Lateral = lateral;
        }

        internal CellId Cell { get; }
        internal int Ring { get; }
        internal int Forward { get; }
        internal int Lateral { get; }
    }
}

}