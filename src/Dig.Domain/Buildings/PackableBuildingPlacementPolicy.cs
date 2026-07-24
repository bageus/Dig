using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public enum BuildingPlacementSurfaceKind
{
    OutdoorGround = 0,
    Tunnel = 1,
}

public readonly struct BuildingPlacementSurfaceCell
{
    public BuildingPlacementSurfaceCell(
        CellId cell,
        decimal elevation,
        BuildingPlacementSurfaceKind surfaceKind)
    {
        if (!Enum.IsDefined(typeof(BuildingPlacementSurfaceKind), surfaceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(surfaceKind));
        }

        Cell = cell;
        Elevation = elevation;
        SurfaceKind = surfaceKind;
    }

    public CellId Cell { get; }

    public decimal Elevation { get; }

    public BuildingPlacementSurfaceKind SurfaceKind { get; }
}

public sealed class BuildingPhysicalFootprint
{
    public BuildingPhysicalFootprint(
        decimal widthCells,
        decimal depthCells,
        IReadOnlyCollection<CellId> coveredCells)
    {
        if (widthCells <= 0m || depthCells <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(widthCells));
        }

        if (coveredCells is null || coveredCells.Count == 0)
        {
            throw new ArgumentException("Physical footprint cells are required.", nameof(coveredCells));
        }

        WidthCells = widthCells;
        DepthCells = depthCells;
        CoveredCells = new ReadOnlyCollection<CellId>(coveredCells
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray());
    }

    public decimal WidthCells { get; }

    public decimal DepthCells { get; }

    public IReadOnlyList<CellId> CoveredCells { get; }
}

public sealed class PackableBuildingSurfacePolicy
{
    public PackableBuildingSurfacePolicy(
        decimal widthCells,
        decimal depthCells,
        bool requiresFlatSurface,
        bool outdoorOnly,
        bool allowsTunnel)
    {
        if (widthCells <= 0m || depthCells <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(widthCells));
        }

        if (outdoorOnly && allowsTunnel)
        {
            throw new ArgumentException("Outdoor-only placement cannot allow tunnels.");
        }

        WidthCells = widthCells;
        DepthCells = depthCells;
        RequiresFlatSurface = requiresFlatSurface;
        OutdoorOnly = outdoorOnly;
        AllowsTunnel = allowsTunnel;
    }

    public decimal WidthCells { get; }

    public decimal DepthCells { get; }

    public bool RequiresFlatSurface { get; }

    public bool OutdoorOnly { get; }

    public bool AllowsTunnel { get; }
}

public static class PackableBuildingPlacementErrors
{
    public static readonly DomainError SurfaceMissing = new DomainError(
        "buildings.placement.surface_missing",
        "The complete physical building footprint requires known surface data.");

    public static readonly DomainError TunnelForbidden = new DomainError(
        "buildings.placement.tunnel_forbidden",
        "This building cannot be placed in a tunnel.");

    public static readonly DomainError SurfaceNotFlat = new DomainError(
        "buildings.placement.surface_not_flat",
        "This building requires a flat surface across its physical footprint.");

    public static readonly DomainError PhysicalFootprintOccupied = new DomainError(
        "buildings.placement.physical_footprint_occupied",
        "The physical building footprint overlaps occupied space.");
}

public sealed class PackableBuildingPlacementPolicyResult
{
    private PackableBuildingPlacementPolicyResult(
        bool succeeded,
        DomainError? error,
        BuildingPhysicalFootprint footprint)
    {
        Succeeded = succeeded;
        Error = error;
        Footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
    }

    public bool Succeeded { get; }

    public DomainError? Error { get; }

    public BuildingPhysicalFootprint Footprint { get; }

    public static PackableBuildingPlacementPolicyResult Success(
        BuildingPhysicalFootprint footprint)
    {
        return new PackableBuildingPlacementPolicyResult(true, null, footprint);
    }

    public static PackableBuildingPlacementPolicyResult Failure(
        DomainError error,
        BuildingPhysicalFootprint footprint)
    {
        return new PackableBuildingPlacementPolicyResult(false, error, footprint);
    }
}

public sealed class PackableBuildingPlacementPolicyValidator
{
    public PackableBuildingPlacementPolicyResult Validate(
        PackableBuildingSurfacePolicy policy,
        CellId origin,
        IReadOnlyCollection<BuildingPlacementSurfaceCell> surfaceCells,
        IReadOnlyCollection<CellId> occupiedCells)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (surfaceCells is null || occupiedCells is null)
        {
            throw new ArgumentNullException(nameof(surfaceCells));
        }

        BuildingPhysicalFootprint footprint = ResolveFootprint(policy, origin);
        Dictionary<CellId, BuildingPlacementSurfaceCell> surfaces = surfaceCells
            .GroupBy(value => value.Cell)
            .ToDictionary(group => group.Key, group => group.First());
        if (footprint.CoveredCells.Any(cell => !surfaces.ContainsKey(cell)))
        {
            return PackableBuildingPlacementPolicyResult.Failure(
                PackableBuildingPlacementErrors.SurfaceMissing,
                footprint);
        }

        BuildingPlacementSurfaceCell[] covered = footprint.CoveredCells
            .Select(cell => surfaces[cell])
            .ToArray();
        if ((!policy.AllowsTunnel || policy.OutdoorOnly)
            && covered.Any(value => value.SurfaceKind == BuildingPlacementSurfaceKind.Tunnel))
        {
            return PackableBuildingPlacementPolicyResult.Failure(
                PackableBuildingPlacementErrors.TunnelForbidden,
                footprint);
        }

        if (policy.RequiresFlatSurface
            && covered.Select(value => value.Elevation).Distinct().Skip(1).Any())
        {
            return PackableBuildingPlacementPolicyResult.Failure(
                PackableBuildingPlacementErrors.SurfaceNotFlat,
                footprint);
        }

        HashSet<CellId> occupied = new HashSet<CellId>(occupiedCells);
        if (footprint.CoveredCells.Any(occupied.Contains))
        {
            return PackableBuildingPlacementPolicyResult.Failure(
                PackableBuildingPlacementErrors.PhysicalFootprintOccupied,
                footprint);
        }

        return PackableBuildingPlacementPolicyResult.Success(footprint);
    }

    public BuildingPhysicalFootprint ResolveFootprint(
        PackableBuildingSurfacePolicy policy,
        CellId origin)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        int width = checked((int)Math.Ceiling(policy.WidthCells));
        int depth = checked((int)Math.Ceiling(policy.DepthCells));
        List<CellId> cells = new List<CellId>(width * depth);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < depth; y++)
            {
                cells.Add(new CellId(origin.X + x, origin.Y + y, origin.Z));
            }
        }

        return new BuildingPhysicalFootprint(policy.WidthCells, policy.DepthCells, cells);
    }
}

}