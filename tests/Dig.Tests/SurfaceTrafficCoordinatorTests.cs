using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SurfaceTrafficCoordinatorTests
{
    private static readonly EntityId AgentA =
        EntityId.Parse("92000000000000000000000000000001");
    private static readonly EntityId AgentB =
        EntityId.Parse("92000000000000000000000000000002");

    [Fact]
    public void Matching_boundary_poses_from_adjacent_cells_share_world_position()
    {
        SurfaceCorridorSteering.TryBuildBoundaryPoses(
            new CellId(1, 2, 0),
            new CellId(2, 2, 0),
            out SurfacePose exit,
            out SurfacePose entry);

        Assert.Equal(0, SurfaceSpatialMath.DistanceSquared(exit, entry));
    }

    [Fact]
    public void Occupied_boundary_never_defers_authoritative_movement()
    {
        SurfaceTrafficCoordinator traffic = new SurfaceTrafficCoordinator();
        SurfacePose leftCentre = SurfacePose.FloorCentre(new CellId(1, 2, 0));
        SurfacePose rightCentre = SurfacePose.FloorCentre(new CellId(2, 2, 0));
        SurfaceCorridorSteering.TryBuildBoundaryPoses(
            leftCentre.Cell,
            rightCentre.Cell,
            out SurfacePose boundary,
            out _);
        traffic.BeginTick(10, new[]
        {
            Pair(AgentA, leftCentre),
            Pair(AgentB, rightCentre),
        });

        Assert.True(traffic.CanOccupy(AgentA, boundary, 10));
        traffic.RecordPose(AgentA, boundary, 10);
        Assert.True(traffic.CanOccupy(AgentB, boundary, 10));
        Assert.True(traffic.CanOccupy(AgentB, boundary, 10));
    }

    [Fact]
    public void Vertical_climbers_never_block_each_other()
    {
        SurfaceTrafficCoordinator traffic = new SurfaceTrafficCoordinator();
        SurfacePose wall = new SurfacePose(
            new CellId(2, 2, 1),
            SurfaceFace.PositiveX,
            500,
            500);
        traffic.BeginTick(15, new[] { Pair(AgentA, wall) });

        Assert.True(traffic.CanOccupy(AgentB, wall, 15));
    }

    [Fact]
    public void Separated_positions_inside_one_cell_remain_available()
    {
        SurfaceTrafficCoordinator traffic = new SurfaceTrafficCoordinator();
        CellId cell = new CellId(3, 2, 0);
        SurfacePose left = new SurfacePose(cell, SurfaceFace.Floor, 100, 500);
        SurfacePose right = new SurfacePose(cell, SurfaceFace.Floor, 900, 500);
        traffic.BeginTick(20, new[] { Pair(AgentA, left) });

        Assert.True(traffic.CanOccupy(AgentB, right, 20));
    }

    private static KeyValuePair<EntityId, SurfacePose> Pair(
        EntityId id,
        SurfacePose pose)
    {
        return new KeyValuePair<EntityId, SurfacePose>(id, pose);
    }
}

}
