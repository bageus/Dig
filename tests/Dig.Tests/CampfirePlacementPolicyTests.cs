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
    public void Campfire_uses_one_logical_cell_on_every_supported_depth_layer()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Definition
            .Placement
            .ToSurfacePolicy();

        BuildingPhysicalFootprint footprint = _validator.ResolveFootprint(
            policy,
            new CellId(4, 5, 0));

        Assert.Equal(1m, footprint.WidthCells);
        Assert.Equal(1m, footprint.DepthCells);
        Assert.Equal(new[] { new CellId(4, 5, 0) }, footprint.CoveredCells);
    }

    [Fact]
    public void Physical_depth_extends_across_world_depth_layers_not_vertical_cells()
    {
        PackableBuildingSurfacePolicy policy = new PackableBuildingSurfacePolicy(
            widthCells: 2m,
            depthCells: 2m,
            requiresFlatSurface: true,
            outdoorOnly: false,
            allowsTunnel: true);

        BuildingPhysicalFootprint footprint = _validator.ResolveFootprint(
            policy,
            new CellId(4, 5, 1));

        Assert.Equal(new[]
        {
            new CellId(4, 5, 1),
            new CellId(5, 5, 1),
            new CellId(4, 5, 2),
            new CellId(5, 5, 2),
        }, footprint.CoveredCells);
    }

    [Fact]
    public void Depth_two_building_is_rejected_when_started_on_last_depth_layer()
    {
        PackableBuildingSurfacePolicy policy = new PackableBuildingSurfacePolicy(
            widthCells: 1m,
            depthCells: 2m,
            requiresFlatSurface: true,
            outdoorOnly: false,
            allowsTunnel: true);
        CellId origin = new CellId(2, 3, CellId.MaximumDepth);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            new[]
            {
                new BuildingPlacementSurfaceCell(
                    origin,
                    elevation: 4m,
                    BuildingPlacementSurfaceKind.Tunnel),
            },
            new CellId[0]);

        Assert.Equal(
            PackableBuildingPlacementErrors.PhysicalFootprintOutOfBounds,
            result.Error);
        Assert.Contains(
            new CellId(origin.X, origin.Y, CellId.MaximumDepth + 1),
            result.Footprint.CoveredCells);
    }

    [Fact]
    public void Campfire_accepts_complete_flat_tunnel_surface()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Definition
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            FlatSurface(origin, BuildingPlacementSurfaceKind.Tunnel),
            new CellId[0]);

        Assert.True(result.Succeeded, result.Error?.ToString());
    }

    [Fact]
    public void Outdoor_only_profile_still_rejects_tunnel_surface()
    {
        PackableBuildingSurfacePolicy policy = new PackableBuildingSurfacePolicy(
            widthCells: 1m,
            depthCells: 1m,
            requiresFlatSurface: true,
            outdoorOnly: true,
            allowsTunnel: false);
        CellId origin = new CellId(2, 3, 1);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            FlatSurface(origin, BuildingPlacementSurfaceKind.Tunnel),
            new CellId[0]);

        Assert.Equal(PackableBuildingPlacementErrors.TunnelForbidden, result.Error);
    }

    [Fact]
    public void Flat_surface_policy_rejects_different_support_elevations()
    {
        PackableBuildingSurfacePolicy policy = new PackableBuildingSurfacePolicy(
            widthCells: 2m,
            depthCells: 1m,
            requiresFlatSurface: true,
            outdoorOnly: false,
            allowsTunnel: true);
        CellId origin = new CellId(2, 3, 1);
        List<BuildingPlacementSurfaceCell> surface = new List<BuildingPlacementSurfaceCell>
        {
            new BuildingPlacementSurfaceCell(
                origin,
                elevation: 4m,
                BuildingPlacementSurfaceKind.Tunnel),
            new BuildingPlacementSurfaceCell(
                new CellId(origin.X + 1, origin.Y, origin.Z),
                elevation: 5m,
                BuildingPlacementSurfaceKind.Tunnel),
        };

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
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Definition
            .Placement
            .ToSurfacePolicy();
        CellId origin = new CellId(2, 3, 0);

        PackableBuildingPlacementPolicyResult result = _validator.Validate(
            policy,
            origin,
            FlatSurface(origin, BuildingPlacementSurfaceKind.OutdoorGround),
            new[] { origin });

        Assert.Equal(
            PackableBuildingPlacementErrors.PhysicalFootprintOccupied,
            result.Error);
    }

    [Fact]
    public void Campfire_requires_surface_facts_for_every_covered_cell()
    {
        PackableBuildingSurfacePolicy policy = CampfireBuildingBoxContent.Definition
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
            new BuildingPlacementSurfaceCell(origin, 0m, kind),
        };
    }
}

}
