using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed partial class CaveRoomPlanner
{
    public static int InterpolateWidth(CaveRoomPreset preset, int level)
    {
        if (preset is null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        if (level < 0 || level >= preset.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (preset.Height == 1)
        {
            return preset.BaseWidth;
        }

        double progress = level / (double)(preset.Height - 1);
        double width = preset.BaseWidth
            + ((preset.TopWidth - preset.BaseWidth) * progress);
        return (int)Math.Round(width, MidpointRounding.AwayFromZero);
    }

    public static int ResolveRowMinX(
        CaveRoomPreset preset,
        int anchorX,
        int level)
    {
        return ResolveRowProfile(preset, anchorX, level).MinCellX;
    }

    public static CaveRoomRowProfile ResolveRowProfile(
        CaveRoomPreset preset,
        int anchorX,
        int level)
    {
        if (preset == null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        int width = InterpolateWidth(preset, level);
        int baseMinX = anchorX - ((preset.BaseWidth - 1) / 2);
        int center2 = (baseMinX * 2) + preset.BaseWidth - 1;
        int leftBoundary2 = center2 - width;
        int rightBoundary2 = center2 + width;
        int minCellX = (int)Math.Ceiling(leftBoundary2 / 2d);
        int maxCellX = (int)Math.Floor(rightBoundary2 / 2d);
        Dictionary<int, ExcavationQuarter> required =
            new Dictionary<int, ExcavationQuarter>();
        for (int x = minCellX; x <= maxCellX; x++)
        {
            ExcavationQuarter mask = ExcavationQuarter.All;
            if (x == minCellX && leftBoundary2 % 2 == 0)
            {
                mask &= ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight;
            }

            if (x == maxCellX && rightBoundary2 % 2 == 0)
            {
                mask &= ExcavationQuarter.UpperLeft | ExcavationQuarter.LowerLeft;
            }

            required.Add(x, mask);
        }

        return new CaveRoomRowProfile(
            level,
            width,
            leftBoundary2,
            rightBoundary2,
            required);
    }

 }

}
