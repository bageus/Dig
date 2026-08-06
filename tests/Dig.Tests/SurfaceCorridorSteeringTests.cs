using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SurfaceCorridorSteeringTests
{
    [Theory]
    [InlineData(1, 0, 1000, 500, 0, 500)]
    [InlineData(-1, 0, 0, 500, 1000, 500)]
    [InlineData(0, 1, 500, 1000, 500, 0)]
    [InlineData(0, -1, 500, 0, 500, 1000)]
    public void Horizontal_step_uses_matching_boundary_points(
        int deltaX,
        int deltaZ,
        int exitU,
        int exitV,
        int entryU,
        int entryV)
    {
        CellId from = new CellId(4, 3, 2);
        CellId to = new CellId(4 + deltaX, 3, 2 + deltaZ);

        bool built = SurfaceCorridorSteering.TryBuildBoundaryPoses(
            from,
            to,
            out SurfacePose exit,
            out SurfacePose entry);

        Assert.True(built);
        Assert.Equal(new SurfacePose(from, SurfaceFace.Floor, exitU, exitV), exit);
        Assert.Equal(new SurfacePose(to, SurfaceFace.Floor, entryU, entryV), entry);
    }

    [Fact]
    public void Vertical_step_is_deferred_to_surface_transition_graph()
    {
        Assert.False(SurfaceCorridorSteering.TryBuildBoundaryPoses(
            new CellId(2, 3, 1),
            new CellId(2, 4, 1),
            out _,
            out _));
    }
}

}
