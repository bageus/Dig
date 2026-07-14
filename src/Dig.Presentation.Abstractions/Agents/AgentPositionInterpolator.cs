using System;

namespace Dig.Presentation.Agents
{

public readonly struct AgentInterpolatedPosition
{
    public AgentInterpolatedPosition(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }
}

public static class AgentPositionInterpolator
{
    public static AgentInterpolatedPosition Interpolate(
        int previousX,
        int previousY,
        int currentX,
        int currentY,
        double progress)
    {
        if (double.IsNaN(progress) || double.IsInfinity(progress))
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        double clamped = Math.Max(0d, Math.Min(1d, progress));
        return new AgentInterpolatedPosition(
            previousX + ((currentX - previousX) * clamped),
            previousY + ((currentY - previousY) * clamped));
    }
}
}