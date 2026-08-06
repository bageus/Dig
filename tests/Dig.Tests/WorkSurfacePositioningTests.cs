using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class WorkSurfacePositioningTests
{
    [Theory]
    [InlineData(1, 0, 850, 500)]
    [InlineData(-1, 0, 150, 500)]
    [InlineData(0, 1, 500, 850)]
    [InlineData(0, -1, 500, 150)]
    public void Work_pose_faces_horizontal_target(
        int deltaX, int deltaZ, int expectedU, int expectedV)
    {
        CellId work = new CellId(4, 3, 2);

        SurfacePose pose = WorkSurfacePositioning.Resolve(
            work,
            new CellId(work.X + deltaX, work.Y, work.Z + deltaZ));

        Assert.Equal(new SurfacePose(
            work, SurfaceFace.Floor, expectedU, expectedV), pose);
    }

    [Fact]
    public void Vertical_target_keeps_worker_at_floor_centre()
    {
        CellId work = new CellId(4, 3, 2);

        SurfacePose pose = WorkSurfacePositioning.Resolve(
            work,
            new CellId(work.X, work.Y + 1, work.Z));

        Assert.Equal(SurfacePose.FloorCentre(work), pose);
    }
}

}
