using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentSpatialPositionPresentationTests
{
    [Fact]
    public void Interpolator_moves_across_all_three_axes()
    {
        AgentInterpolatedSpatialPosition position =
            AgentSpatialPositionInterpolator.Interpolate(
                fromX: 0,
                fromY: 2,
                fromZ: 0,
                toX: 8,
                toY: 6,
                toZ: 3,
                progress: 0.5d);

        Assert.Equal(4d, position.X);
        Assert.Equal(4d, position.Y);
        Assert.Equal(1.5d, position.Z);
    }

    [Theory]
    [InlineData(-1d, 0d)]
    [InlineData(2d, 1d)]
    public void Interpolator_clamps_progress(double progress, double expectedX)
    {
        AgentInterpolatedSpatialPosition position =
            AgentSpatialPositionInterpolator.Interpolate(
                0,
                0,
                0,
                1,
                1,
                1,
                progress);

        Assert.Equal(expectedX, position.X);
    }
}

}
