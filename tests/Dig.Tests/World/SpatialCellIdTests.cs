using System;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests.World
{

public sealed class SpatialCellIdTests
{
    [Fact]
    public void Same_XY_On_Different_Depths_Are_Distinct()
    {
        SpatialCellId upper = new SpatialCellId(12, 7, 1);
        SpatialCellId lower = new SpatialCellId(12, 7, 2);

        Assert.NotEqual(upper, lower);
        Assert.NotEqual(upper.GetHashCode(), lower.GetHashCode());
        Assert.NotEqual(0, upper.CompareTo(lower));
    }

    [Theory]
    [InlineData(WorldDepth.Minimum - 1)]
    [InlineData(WorldDepth.Maximum + 1)]
    public void Constructor_Rejects_Depth_Outside_Authoritative_Range(int z)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SpatialCellId(0, 0, z));

        Assert.Equal("z", exception.ParamName);
    }

    [Theory]
    [InlineData(WorldDepth.Minimum)]
    [InlineData(WorldDepth.Maximum)]
    public void Constructor_Accepts_Authoritative_Depth_Boundaries(int z)
    {
        SpatialCellId cell = new SpatialCellId(3, 5, z);

        Assert.Equal(z, cell.Z);
    }
}

}
