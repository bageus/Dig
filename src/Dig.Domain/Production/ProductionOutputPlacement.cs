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
        "No free explored output cell is available in front of the workstation.");
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

        (int forwardX, int forwardY, int lateralX, int lateralY) =
            ResolveAxes(building.Orientation);
        int leading = building.Footprint.Max(cell =>
            cell.X * forwardX + cell.Y * forwardY);
        List<CellId> values = new List<CellId>();
        AddCandidate(values, building, leading, 0, forwardX, forwardY, lateralX, lateralY);
        for (int distance = 1; distance <= maximumLateralDistance; distance++)
        {
            AddCandidate(
                values,
                building,
                leading,
                -distance,
                forwardX,
                forwardY,
                lateralX,
                lateralY);
            AddCandidate(
                values,
                building,
                leading,
                distance,
                forwardX,
                forwardY,
                lateralX,
                lateralY);
        }

        return values;
    }

    private static void AddCandidate(
        ICollection<CellId> values,
        BuildingSnapshot building,
        int leading,
        int lateralDistance,
        int forwardX,
        int forwardY,
        int lateralX,
        int lateralY)
    {
        int forwardCoordinate = leading + 1;
        values.Add(new CellId(
            forwardX == 0
                ? building.Origin.X + lateralX * lateralDistance
                : forwardCoordinate * forwardX + lateralX * lateralDistance,
            forwardY == 0
                ? building.Origin.Y + lateralY * lateralDistance
                : forwardCoordinate * forwardY + lateralY * lateralDistance,
            building.Origin.Z));
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
}

}
