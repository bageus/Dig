using System;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SideViewCameraTests
{
    [Fact]
    public void Orbit_tracks_mouse_deltas_and_resets_to_side_view()
    {
        SideViewCameraOrbitState orbit = new SideViewCameraOrbitState();

        orbit.Rotate(horizontalDelta: 10f, verticalDelta: -5f, degreesPerUnit: 0.5f);

        Assert.Equal(5f, orbit.Yaw);
        Assert.Equal(-2.5f, orbit.Pitch);

        orbit.Reset();

        Assert.Equal(0f, orbit.Yaw);
        Assert.Equal(0f, orbit.Pitch);
    }

    [Fact]
    public void Orbit_clamps_to_side_view_readability_limits()
    {
        SideViewCameraOrbitState orbit = new SideViewCameraOrbitState(
            minimumYaw: -20f,
            maximumYaw: 20f,
            minimumPitch: -10f,
            maximumPitch: 15f);

        orbit.Rotate(100f, 100f, degreesPerUnit: 1f);
        Assert.Equal(20f, orbit.Yaw);
        Assert.Equal(15f, orbit.Pitch);

        orbit.Rotate(-200f, -200f, degreesPerUnit: 1f);
        Assert.Equal(-20f, orbit.Yaw);
        Assert.Equal(-10f, orbit.Pitch);
    }

    [Fact]
    public void Orbit_rejects_invalid_limits_and_sensitivity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SideViewCameraOrbitState(1f, 1f, -10f, 10f));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SideViewCameraOrbitState(-10f, 10f, 2f, 2f));

        SideViewCameraOrbitState orbit = new SideViewCameraOrbitState();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            orbit.Rotate(1f, 1f, degreesPerUnit: -0.1f));
    }

    [Fact]
    public void Perspective_framing_fits_square_and_wide_side_view_worlds()
    {
        float squareDistance = SideViewCameraFraming.CalculateDistance(
            width: 2f,
            height: 2f,
            aspect: 1f,
            verticalFieldOfViewDegrees: 90f,
            padding: 1f);
        float wideDistance = SideViewCameraFraming.CalculateDistance(
            width: 4f,
            height: 2f,
            aspect: 2f,
            verticalFieldOfViewDegrees: 90f,
            padding: 1f);

        Assert.InRange(squareDistance, 0.999f, 1.001f);
        Assert.InRange(wideDistance, 0.999f, 1.001f);
    }

    [Fact]
    public void Perspective_framing_applies_padding_and_validates_inputs()
    {
        float distance = SideViewCameraFraming.CalculateDistance(
            width: 2f,
            height: 2f,
            aspect: 1f,
            verticalFieldOfViewDegrees: 90f,
            padding: 1.25f);

        Assert.InRange(distance, 1.249f, 1.251f);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SideViewCameraFraming.CalculateDistance(0f, 2f, 1f, 90f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SideViewCameraFraming.CalculateDistance(2f, 2f, 0f, 90f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SideViewCameraFraming.CalculateDistance(2f, 2f, 1f, 90f, 0.9f));
    }
}
}
