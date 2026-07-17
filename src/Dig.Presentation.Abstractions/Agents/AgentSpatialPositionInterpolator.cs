using System;

namespace Dig.Presentation.Agents
{

public readonly struct AgentInterpolatedSpatialPosition
{
    public AgentInterpolatedSpatialPosition(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }

    public double Y { get; }

    public double Z { get; }
}

public static class AgentSpatialPositionInterpolator
{
    public static AgentInterpolatedSpatialPosition Interpolate(
        double fromX,
        double fromY,
        double fromZ,
        double toX,
        double toY,
        double toZ,
        double progress)
    {
        if (double.IsNaN(progress) || double.IsInfinity(progress))
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        double clamped = Math.Max(0d, Math.Min(1d, progress));
        return new AgentInterpolatedSpatialPosition(
            fromX + ((toX - fromX) * clamped),
            fromY + ((toY - fromY) * clamped),
            fromZ + ((toZ - fromZ) * clamped));
    }
}

}
