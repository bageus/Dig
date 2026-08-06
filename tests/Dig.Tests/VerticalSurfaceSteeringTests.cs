using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class VerticalSurfaceSteeringTests
{
    [Fact]
    public void Unsupported_floor_pose_attaches_to_nearest_exposed_wall()
    {
        SurfacePose floor = new SurfacePose(
            new CellId(2, 3, 1), SurfaceFace.Floor, 850, 400);

        bool attached = VerticalSurfaceSteering.TryAttachToWall(
            floor,
            face => face == SurfaceFace.NegativeX || face == SurfaceFace.PositiveX,
            out SurfacePose wall);

        Assert.True(attached);
        Assert.Equal(SurfaceFace.PositiveX, wall.Face);
        Assert.Equal(400, wall.U);
        Assert.Equal(SurfacePose.CellCentre, wall.V);
    }

    [Fact]
    public void Unsupported_floor_pose_rejects_missing_exposed_wall()
    {
        bool attached = VerticalSurfaceSteering.TryAttachToWall(
            SurfacePose.FloorCentre(new CellId(2, 3, 1)),
            face => false,
            out _);

        Assert.False(attached);
    }

    [Fact]
    public void Vertical_step_attaches_climbs_and_crosses_at_matching_world_points()
    {
        CellId from = new CellId(3, 4, 0);
        CellId to = new CellId(3, 5, 0);
        SurfacePose floor = new SurfacePose(
            from, SurfaceFace.Floor, 900, 100);

        Assert.True(VerticalSurfaceSteering.TryBuildNextPose(
            floor, to, out SurfacePose attached, out bool attachedCrosses));
        Assert.False(attachedCrosses);
        Assert.Equal(new SurfacePose(
            from, SurfaceFace.PositiveX, 100, 500), attached);

        Assert.True(VerticalSurfaceSteering.TryBuildNextPose(
            attached, to, out SurfacePose exit, out bool exitCrosses));
        Assert.False(exitCrosses);
        Assert.Equal(new SurfacePose(
            from, SurfaceFace.PositiveX, 100, 1000), exit);

        Assert.True(VerticalSurfaceSteering.TryBuildNextPose(
            exit, to, out SurfacePose entry, out bool entryCrosses));
        Assert.True(entryCrosses);
        Assert.Equal(new SurfacePose(
            to, SurfaceFace.PositiveX, 100, 0), entry);
    }

    [Fact]
    public void Z0_attachment_never_selects_the_external_negative_z_face()
    {
        SurfacePose floor = new SurfacePose(
            new CellId(2, 3, 0), SurfaceFace.Floor, 500, 0);

        Assert.True(VerticalSurfaceSteering.TryBuildNextPose(
            floor,
            new CellId(2, 4, 0),
            out SurfacePose attached,
            out _));

        Assert.NotEqual(SurfaceFace.NegativeZ, attached.Face);
        Assert.True(SurfaceTraversalPolicy.CanUse(
            SurfaceMoverKind.Resident,
            attached));
    }

    [Fact]
    public void Wall_detaches_to_the_same_floor_edge()
    {
        SurfacePose wall = new SurfacePose(
            new CellId(2, 3, 1), SurfaceFace.PositiveZ, 125, 500);

        Assert.True(VerticalSurfaceSteering.TryDetachToFloor(
            wall,
            out SurfacePose floor));
        Assert.Equal(new SurfacePose(
            wall.Cell, SurfaceFace.Floor, 125, 1000), floor);
    }

    [Fact]
    public void Vertical_step_is_rejected_when_no_external_face_is_available()
    {
        SurfacePose floor = SurfacePose.FloorCentre(new CellId(2, 3, 1));

        bool built = VerticalSurfaceSteering.TryBuildNextPose(
            floor,
            new CellId(2, 4, 1),
            _ => false,
            out _,
            out _);

        Assert.False(built);
    }
}

}
