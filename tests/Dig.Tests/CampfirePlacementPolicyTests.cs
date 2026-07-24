using System.Collections.Generic;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfirePlacementPolicyTests
{
    private readonly PackableBuildingPlacementPolicyValidator _validator =
        new PackableBuildingPlacementPolicyValidator();

    [Fact]
    public void Campfire_uses_two_by_two_logical_coverage_for_one_point_five_square()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Create()
            .Placement
            .ToSurfacePolicy();

        BuildingPhysicalFootprint footprint = _validator.ResolveFootprint(
            policy,
            new CellId(4, 5, 0));

        Assert.Equal(1.5m, footprint.WidthCells);
        Assert.Equal(1.5m, footprint.DepthCells);
        Assert.Equal(4, footprint.CoveredCells.Count);
        Assert.Contains(new CellId(4, 5, 0), footprint.CoveredCells);
        Assert.Contains(new CellId(5, 6, 0), footprint.CoveredCells);
    }

    [Fact]
    public void Campfire_accepts_complete_flat_outdoor_surface()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Create()
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            FlatSurface(origin, BuildingPlacementSurfaceKind.OutdoorGround),
            new CellId[0]);

        Assert.True(result.Succeeded, result.Error?.ToString());
    }

    [Fact]
    public void Campfire_rejects_any_tunnel_cell_under_physical_footprint()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Create()
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);
        List<BuildingPlacementSurfaceCell> surface = FlatSurface(
            origin,
            BuildingPlacementSurfaceKind.OutdoorGround);
        surface[3] = new BuildingPlacementSurfaceCell(
            surface[3].Cell,
            elevation: 0m,
            BuildingPlacementSurfaceKind.Tunnel);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            surface,
            new CellId[0]);

        Assert.Equal(PackableBuildingPlacementErrors.TunnelForbidden, result.Error);
    }

    [Fact]
    public void Campfire_rejects_non_flat_surface()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Create()
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);
        List<BuildingPlacementSurfaceCell> surface = FlatSurface(
            origin,
            BuildingPlacementSurfaceKind.OutdoorGround);
        surface[1] = new BuildingPlacementSurfaceCell(
            surface[1].Cell,
            elevation: 0.25m,
            BuildingPlacementSurfaceKind.OutdoorGround);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            surface,
            new CellId[0]);

        Assert.Equal(PackableBuildingPlacementErrors.SurfaceNotFlat, result.Error);
    }

    [Fact]
    public void Campfire_rejects_overlap_in_any_conservatively_covered_cell()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Create()
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            FlatSurface(origin, BuildingPlacementSurfaceKind.OutdoorGround),
            new[] { new CellId(3, 4, 0) });

        Assert.Equal(
            PackableBuildingPlacementErrors.PhysicalFootprintOccupied,
            result.Error);
    }

    [Fact]
    public void Campfire_requires_surface_facts_for_every_covered_cell()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Create()
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);
        List<BuildingPlacementSurfaceCell> surface = FlatSurface(
            origin,
            BuildingPlacementSurfaceKind.OutdoorGround);
        surface.RemoveAt(surface.Count - 1);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            surface,
            new CellId[0]);

        Assert.Equal(PackableBuildingPlacementErrors.SurfaceMissing, result.Error);
    }

    private static List<BuildingPlacementSurfaceCell> FlatSurface(
        CellId origin,
        BuildingPlacementSurfaceKind kind)
    {
        return new List<BuildingPlacementSurfaceCell>
        {
            new BuildingPlacementSurfaceCell(
                new CellId(origin.X, origin.Y, origin.Z), 0m, kind),
            new BuildingPlacementSurfaceCell(
                new CellId(origin.X + 1, origin.Y, origin.Z), 0m, kind),
            new BuildingPlacementSurfaceCell(
                new CellId(origin.X, origin.Y + 1, origin.Z), 0m, kind),
            new BuildingPlacementSurfaceCell(
                new CellId(origin.X + 1, origin.Y + 1, origin.Z), 0m, kind),
        };
    }
}

}