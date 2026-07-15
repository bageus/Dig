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

    public static readonly DomainError WrongConstructionPolicy = new DomainError(
        "buildings.construction_policy.invalid",
        "The requested operation does not match the building construction policy.");

    public static readonly DomainError BoxPlanNotFound = new DomainError(
        "buildings.box_plan.not_found",
        "The requested building has no box construction plan.");

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

    public static BuildingPlacementResult Failure(DomainError error)
    {
        return new BuildingPlacementResult(
            false,
            error ?? throw new ArgumentNullException(nameof(error)),
            Array.Empty<CellId>(),
            default);
    }
}
}