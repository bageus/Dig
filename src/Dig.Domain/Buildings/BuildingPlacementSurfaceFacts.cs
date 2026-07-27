using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public sealed class BuildingPlacementSurfaceFactProjector
{
    private readonly PackableBuildingPlacementPolicyValidator _validator;

    public BuildingPlacementSurfaceFactProjector(
        PackableBuildingPlacementPolicyValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public IReadOnlyList<BuildingPlacementSurfaceCell> Project(
        PackableBuildingSurfacePolicy policy,
        CellId origin,
        WorldSnapshot world)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        Dictionary<CellId, CellSnapshot> cells = CreateCellIndex(world);
        BuildingPhysicalFootprint footprint = _validator.ResolveFootprint(policy, origin);
        CellId[] covered = footprint.CoveredCells.ToArray();
        Dictionary<(int X, int Z), int> bottomByColumn = ResolveBottomByColumn(covered);
        Dictionary<(int X, int Z), BuildingPlacementSurfaceCell> supportByColumn =
            new Dictionary<(int X, int Z), BuildingPlacementSurfaceCell>();

        foreach (KeyValuePair<(int X, int Z), int> column in bottomByColumn)
        {
            CellId bottom = new CellId(column.Key.X, column.Value, column.Key.Z);
            if (!TryResolveSupport(bottom, cells, out BuildingPlacementSurfaceCell support))
            {
                continue;
            }

            supportByColumn.Add(column.Key, support);
        }

        List<BuildingPlacementSurfaceCell> facts = new List<BuildingPlacementSurfaceCell>();
        foreach (CellId cell in covered)
        {
            if (!cells.TryGetValue(cell, out CellSnapshot snapshot)
                || !snapshot.State.IsExplored
                || snapshot.IsSolid
                || !supportByColumn.TryGetValue((cell.X, cell.Z), out BuildingPlacementSurfaceCell support))
            {
                continue;
            }

            facts.Add(new BuildingPlacementSurfaceCell(
                cell,
                support.Elevation,
                support.SurfaceKind));
        }

        return facts;
    }

    public static bool HasSupportingPlane(CellId origin, WorldSnapshot world)
    {
        return HasSupportingPlane(new[] { origin }, world);
    }

    public static bool HasSupportingPlane(
        IReadOnlyCollection<CellId> occupiedCells,
        WorldSnapshot world)
    {
        if (occupiedCells is null || occupiedCells.Count == 0)
        {
            throw new ArgumentException(
                "At least one occupied building cell is required.",
                nameof(occupiedCells));
        }

        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        Dictionary<CellId, CellSnapshot> cells = CreateCellIndex(world);
        foreach (CellId occupied in occupiedCells)
        {
            if (!cells.TryGetValue(occupied, out CellSnapshot target)
                || !target.State.IsExplored
                || target.IsSolid)
            {
                return false;
            }
        }

        Dictionary<(int X, int Z), int> bottomByColumn = ResolveBottomByColumn(occupiedCells);
        int? supportElevation = null;
        foreach (KeyValuePair<(int X, int Z), int> column in bottomByColumn)
        {
            CellId bottom = new CellId(column.Key.X, column.Value, column.Key.Z);
            if (!TryResolveSupport(bottom, cells, out BuildingPlacementSurfaceCell support))
            {
                return false;
            }

            int elevation = checked((int)support.Elevation);
            if (supportElevation.HasValue && supportElevation.Value != elevation)
            {
                return false;
            }

            supportElevation = elevation;
        }

        return supportElevation.HasValue;
    }

    private static Dictionary<(int X, int Z), int> ResolveBottomByColumn(
        IEnumerable<CellId> occupiedCells)
    {
        return occupiedCells
            .GroupBy(cell => (cell.X, cell.Z))
            .ToDictionary(group => group.Key, group => group.Max(cell => cell.Y));
    }

    private static bool TryResolveSupport(
        CellId bottomOccupiedCell,
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        out BuildingPlacementSurfaceCell support)
    {
        CellId below = new CellId(
            bottomOccupiedCell.X,
            bottomOccupiedCell.Y + 1,
            bottomOccupiedCell.Z);
        if (!cells.TryGetValue(below, out CellSnapshot floor)
            || !floor.State.IsExplored
            || !floor.IsSolid)
        {
            support = default;
            return false;
        }

        BuildingPlacementSurfaceKind kind = IsTunnel(bottomOccupiedCell, cells)
            ? BuildingPlacementSurfaceKind.Tunnel
            : BuildingPlacementSurfaceKind.OutdoorGround;
        support = new BuildingPlacementSurfaceCell(
            bottomOccupiedCell,
            elevation: below.Y,
            kind);
        return true;
    }

    private static Dictionary<CellId, CellSnapshot> CreateCellIndex(WorldSnapshot world)
    {
        return world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
    }

    private static bool IsTunnel(
        CellId cell,
        IReadOnlyDictionary<CellId, CellSnapshot> cells)
    {
        CellId aboveDepth = new CellId(cell.X, cell.Y, cell.Z + 1);
        return cells.TryGetValue(aboveDepth, out CellSnapshot ceiling) && ceiling.IsSolid;
    }
}

}
