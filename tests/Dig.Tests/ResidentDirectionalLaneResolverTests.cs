using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentDirectionalLaneResolverTests
{
    [Fact]
    public void Horizontal_direction_selects_a_deterministic_lane_without_resident_identity()
    {
        ResidentDirectionalLanePreference right = ResidentDirectionalLaneResolver.Resolve(
            previousX: 2,
            previousY: 4,
            previousZ: 1,
            currentX: 3,
            currentY: 4,
            currentZ: 1);
        ResidentDirectionalLanePreference left = ResidentDirectionalLaneResolver.Resolve(
            previousX: 3,
            previousY: 4,
            previousZ: 1,
            currentX: 2,
            currentY: 4,
            currentZ: 1);

        Assert.Equal(ResidentDirectionalLane.Right, right.Lane);
        Assert.Equal(ResidentDirectionalLaneResolver.HorizontalOffsetX, right.OffsetX);
        Assert.Equal(ResidentDirectionalLane.Left, left.Lane);
        Assert.Equal(-ResidentDirectionalLaneResolver.HorizontalOffsetX, left.OffsetX);
    }

    [Theory]
    [InlineData(2, 4, 1, 2, 4, 1)]
    [InlineData(2, 4, 1, 2, 5, 1)]
    [InlineData(2, 4, 1, 2, 4, 2)]
    public void Stationary_depth_and_vertical_transitions_stay_centered(
        int previousX,
        int previousY,
        int previousZ,
        int currentX,
        int currentY,
        int currentZ)
    {
        ResidentDirectionalLanePreference lane = ResidentDirectionalLaneResolver.Resolve(
            previousX,
            previousY,
            previousZ,
            currentX,
            currentY,
            currentZ);

        Assert.Equal(ResidentDirectionalLane.Center, lane.Lane);
        Assert.Equal(0d, lane.OffsetX);
    }
}

}
