using System;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SurfacePoseSteeringTests
{
    [Fact]
    public void Long_floor_motion_advances_inside_cell_without_snapping_to_target()
    {
        CellId cell = new CellId(4, 2, 1);
        SurfacePose start = new SurfacePose(cell, SurfaceFace.Floor, 0, 500);
        SurfacePose target = new SurfacePose(cell, SurfaceFace.Floor, 1_000, 500);

        SurfacePose next = SurfacePoseSteering.MoveTowards(start, target);

        Assert.Equal(cell, next.Cell);
        Assert.Equal(200, next.U);
        Assert.Equal(500, next.V);
        Assert.NotEqual(target, next);
    }

    [Fact]
    public void Repeated_steps_reach_arbitrary_two_axis_floor_point_exactly()
    {
        CellId cell = new CellId(3, 1, 2);
        SurfacePose pose = new SurfacePose(cell, SurfaceFace.Floor, 73, 914);
        SurfacePose target = new SurfacePose(cell, SurfaceFace.Floor, 847, 126);

        for (int index = 0; index < 20 && pose != target; index++)
        {
            pose = SurfacePoseSteering.MoveTowards(pose, target);
        }

        Assert.Equal(target, pose);
    }

    [Fact]
    public void Steering_rejects_a_cell_boundary_teleport()
    {
        SurfacePose current = SurfacePose.FloorCentre(new CellId(0, 0, 0));
        SurfacePose target = SurfacePose.FloorCentre(new CellId(1, 0, 0));

        Assert.Throws<ArgumentException>(() =>
            SurfacePoseSteering.MoveTowards(current, target));
    }
}
}
