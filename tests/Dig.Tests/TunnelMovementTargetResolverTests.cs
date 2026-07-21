using System;
using Dig.Domain.World;
using Dig.Presentation.Navigation;
using Xunit;

namespace Dig.Tests
{
public sealed class TunnelMovementTargetResolverTests
{
    [Fact]
    public void ResolvePreservesFreeformOffsetInsideHiddenCell()
    {
        TunnelMovementTarget target = new TunnelMovementTargetResolver().Resolve(
            new[]
            {
                new SpatialCellId(4, 3, 1),
                new SpatialCellId(5, 3, 1),
            },
            logicalX: 4.31d,
            logicalY: 3d);

        Assert.Equal(new SpatialCellId(4, 3, 1), target.Cell);
        Assert.Equal(0.31d, target.OffsetX, precision: 6);
    }

    [Fact]
    public void ResolveClampsVisualOffsetWithoutChangingNavigationCell()
    {
        TunnelMovementTarget target = new TunnelMovementTargetResolver().Resolve(
            new[] { new SpatialCellId(4, 3, 2) },
            logicalX: 4.9d,
            logicalY: 3d);

        Assert.Equal(new SpatialCellId(4, 3, 2), target.Cell);
        Assert.Equal(TunnelMovementTargetResolver.MaximumOffsetX, target.OffsetX);
    }

    [Fact]
    public void ResolveUsesVerticalPositionForShaftSurface()
    {
        TunnelMovementTarget target = new TunnelMovementTargetResolver().Resolve(
            new[]
            {
                new SpatialCellId(7, 2, 3),
                new SpatialCellId(7, 3, 3),
                new SpatialCellId(7, 4, 3),
            },
            logicalX: 7.1d,
            logicalY: 3.74d);

        Assert.Equal(new SpatialCellId(7, 4, 3), target.Cell);
        Assert.Equal(0.1d, target.OffsetX, precision: 6);
    }

    [Fact]
    public void ResolveRejectsEmptySurface()
    {
        Assert.Throws<ArgumentException>(() =>
            new TunnelMovementTargetResolver().Resolve(
                Array.Empty<SpatialCellId>(),
                logicalX: 0d,
                logicalY: 0d));
    }
}
}
