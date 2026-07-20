using System;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainVisualDetailPolicyTests
{
    [Theory]
    [InlineData(40f, TerrainVisualDetailLevel.Full)]
    [InlineData(16f, TerrainVisualDetailLevel.Reduced)]
    [InlineData(4f, TerrainVisualDetailLevel.Marker)]
    public void Marker_state_enters_the_expected_detail_band(
        float pixelsPerCell,
        TerrainVisualDetailLevel expected)
    {
        TerrainVisualDetailPolicy policy = new TerrainVisualDetailPolicy();

        Assert.Equal(
            expected,
            policy.Resolve(pixelsPerCell, TerrainVisualDetailLevel.Marker));
    }

    [Fact]
    public void Full_detail_uses_exit_threshold_before_downgrading()
    {
        TerrainVisualDetailPolicy policy = new TerrainVisualDetailPolicy();

        Assert.Equal(
            TerrainVisualDetailLevel.Full,
            policy.Resolve(24f, TerrainVisualDetailLevel.Full));
        Assert.Equal(
            TerrainVisualDetailLevel.Reduced,
            policy.Resolve(20f, TerrainVisualDetailLevel.Full));
    }

    [Fact]
    public void Marker_detail_uses_enter_threshold_before_upgrading()
    {
        TerrainVisualDetailPolicy policy = new TerrainVisualDetailPolicy();

        Assert.Equal(
            TerrainVisualDetailLevel.Marker,
            policy.Resolve(10f, TerrainVisualDetailLevel.Marker));
        Assert.Equal(
            TerrainVisualDetailLevel.Reduced,
            policy.Resolve(12f, TerrainVisualDetailLevel.Marker));
    }

    [Fact]
    public void Reduced_detail_can_skip_directly_to_full_after_large_zoom_change()
    {
        TerrainVisualDetailPolicy policy = new TerrainVisualDetailPolicy();

        Assert.Equal(
            TerrainVisualDetailLevel.Full,
            policy.Resolve(32f, TerrainVisualDetailLevel.Reduced));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Invalid_pixel_measurement_is_rejected(float pixelsPerCell)
    {
        TerrainVisualDetailPolicy policy = new TerrainVisualDetailPolicy();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            policy.Resolve(pixelsPerCell, TerrainVisualDetailLevel.Full));
    }

    [Fact]
    public void Invalid_threshold_order_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TerrainVisualDetailPolicy(
            reducedExitPixelsPerCell: 12f,
            reducedEnterPixelsPerCell: 8f,
            fullExitPixelsPerCell: 22f,
            fullEnterPixelsPerCell: 28f));
    }
}

}
