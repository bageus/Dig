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

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        BuildingPhysicalFootprint footprint = _validator.ResolveFootprint(policy, origin);
        List<BuildingPlacementSurfaceCell> facts = new List<BuildingPlacementSurfaceCell>();
        foreach (CellId cell in footprint.CoveredCells)
        {
            if (!cells.TryGetValue(cell, out CellSnapshot snapshot)
                || !snapshot.State.IsExplored
                || snapshot.IsSolid)
            {
                continue;
            }

            BuildingPlacementSurfaceKind kind = IsTunnel(cell, cells)
                ? BuildingPlacementSurfaceKind.Tunnel
                : BuildingPlacementSurfaceKind.OutdoorGround;
            facts.Add(new BuildingPlacementSurfaceCell(
                cell,
                elevation: cell.Z,
                kind));
        }

        return facts;
    }

    private static bool IsTunnel(
        CellId cell,
        IReadOnlyDictionary<CellId, CellSnapshot> cells)
    {
        CellId above = new CellId(cell.X, cell.Y, cell.Z + 1);
        return cells.TryGetValue(above, out CellSnapshot ceiling) && ceiling.IsSolid;
    }
}

}