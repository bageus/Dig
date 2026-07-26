using System;

namespace Dig.Presentation.Agents
{

public enum ResidentDirectionalLane
{
    Center = 0,
    Left = 1,
    Right = 2,
}

public readonly struct ResidentDirectionalLanePreference
{
    public ResidentDirectionalLanePreference(
        ResidentDirectionalLane lane,
        double offsetX)
    {
        if (offsetX < -ResidentDirectionalLaneResolver.HorizontalOffsetX
            || offsetX > ResidentDirectionalLaneResolver.HorizontalOffsetX)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetX));
        }

        Lane = lane;
        OffsetX = offsetX;
    }

    public ResidentDirectionalLane Lane { get; }
    public double OffsetX { get; }
}

public static class ResidentDirectionalLaneResolver
{
    public const double HorizontalOffsetX = 0.18d;

    public static ResidentDirectionalLanePreference Resolve(
        int previousX,
        int previousY,
        int previousZ,
        int currentX,
        int currentY,
        int currentZ)
    {
        if (previousY != currentY || previousZ != currentZ)
        {
            return Center();
        }

        if (currentX > previousX)
        {
            return new ResidentDirectionalLanePreference(
                ResidentDirectionalLane.Right,
                HorizontalOffsetX);
        }

        if (currentX < previousX)
        {
            return new ResidentDirectionalLanePreference(
                ResidentDirectionalLane.Left,
                -HorizontalOffsetX);
        }

        return Center();
    }

    private static ResidentDirectionalLanePreference Center()
    {
        return new ResidentDirectionalLanePreference(
            ResidentDirectionalLane.Center,
            offsetX: 0d);
    }
}

}
