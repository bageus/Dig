using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public static class BuildingErrors
{
    public static readonly DomainError AlreadyExists = new DomainError(
        "buildings.already_exists",
        "A building project with the same id already exists.");

    public static readonly DomainError NotFound = new DomainError(
        "buildings.not_found",
        "The requested building project does not exist.");

    public static readonly DomainError InvalidStatus = new DomainError(
        "buildings.invalid_status",
        "The building cannot perform that transition from its current status.");

    public static readonly DomainError PlacementOutOfBounds = new DomainError(
        "buildings.placement.out_of_bounds",
        "Part of the building footprint is outside the world.");

    public static readonly DomainError PlacementSolid = new DomainError(
        "buildings.placement.solid",
        "The building footprint requires empty terrain.");

    public static readonly DomainError PlacementUnexplored = new DomainError(
        "buildings.placement.unexplored",
        "The building footprint contains unexplored terrain.");

    public static readonly DomainError PlacementOccupied = new DomainError(
        "buildings.placement.occupied",
        "The building footprint overlaps another active project or building.");

    public static readonly DomainError NoReachableWorkPosition = new DomainError(
        "buildings.placement.no_reachable_work_position",
        "No configured work position is currently reachable.");

    public static readonly DomainError MaterialsUnavailable = new DomainError(
        "buildings.materials.unavailable",
        "The construction site does not contain all required materials.");

    public static readonly DomainError WorkIncomplete = new DomainError(
        "buildings.work.incomplete",
        "Construction work has not reached the required amount.");
}

public sealed class BuildingPlacementResult
{
    private BuildingPlacementResult(
        bool succeeded,
        DomainError? error,
        IReadOnlyCollection<CellId> footprint,
        CellId workPosition)
    {
        Succeeded = succeeded;
        Error = error;
        Footprint = new ReadOnlyCollection<CellId>(footprint.OrderBy(cell => cell).ToArray());
        WorkPosition = workPosition;
    }

    public bool Succeeded { get; }

    public DomainError? Error { get; }

    public IReadOnlyList<CellId> Footprint { get; }

    public CellId WorkPosition { get; }

    public static BuildingPlacementResult Success(
        IReadOnlyCollection<CellId> footprint,
        CellId workPosition)
    {
        return new BuildingPlacementResult(true, null, footprint, workPosition);
    }

    public static BuildingPlacementResult Failure(
        DomainError error,
        IReadOnlyCollection<CellId> footprint)
    {
        return new BuildingPlacementResult(false, error, footprint, default);
    }
}

public sealed class BuildingPlacementValidator
{
    public BuildingPlacementResult Validate(
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> occupiedCells,
        IReadOnlyCollection<CellId> reachableCells)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (occupiedCells is null || reachableCells is null)
        {
            throw new ArgumentNullException(nameof(occupiedCells));
        }

        IReadOnlyList<CellId> footprint = definition.ResolveFootprint(origin, orientation);
        if (footprint.Any(cell => !world.Size.Contains(cell)))
        {
            return BuildingPlacementResult.Failure(
                BuildingErrors.PlacementOutOfBounds,
                footprint);
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        if (footprint.Any(cell => cells[cell].IsSolid))
        {
            return BuildingPlacementResult.Failure(BuildingErrors.PlacementSolid, footprint);
        }

        if (footprint.Any(cell => !cells[cell].State.IsExplored))
        {
            return BuildingPlacementResult.Failure(
                BuildingErrors.PlacementUnexplored,
                footprint);
        }

        HashSet<CellId> occupied = new HashSet<CellId>(occupiedCells);
        if (footprint.Any(occupied.Contains))
        {
            return BuildingPlacementResult.Failure(BuildingErrors.PlacementOccupied, footprint);
        }

        HashSet<CellId> reachable = new HashSet<CellId>(reachableCells);
        CellId? workPosition = definition
            .ResolveWorkPositions(origin, orientation)
            .Where(world.Size.Contains)
            .Where(cell => cells.TryGetValue(cell, out CellSnapshot snapshot)
                && !snapshot.IsSolid
                && snapshot.State.IsExplored)
            .Where(reachable.Contains)
            .OrderBy(cell => cell)
            .Cast<CellId?>()
            .FirstOrDefault();
        if (!workPosition.HasValue)
        {
            return BuildingPlacementResult.Failure(
                BuildingErrors.NoReachableWorkPosition,
                footprint);
        }

        return BuildingPlacementResult.Success(footprint, workPosition.Value);
    }
}
}
