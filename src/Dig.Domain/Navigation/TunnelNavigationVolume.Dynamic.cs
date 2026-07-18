using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public sealed partial class TunnelNavigationVolume
{
    public TunnelNavigationVolume WithAdditionalOpenCells(
        IReadOnlyCollection<SpatialCellId> additionalOpenCells)
    {
        if (additionalOpenCells is null)
        {
            throw new ArgumentNullException(nameof(additionalOpenCells));
        }

        HashSet<SpatialCellId> open = new HashSet<SpatialCellId>(Cells);
        foreach (SpatialCellId cell in additionalOpenCells)
        {
            if (!Contains(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(additionalOpenCells));
            }

            open.Add(cell);
        }

        return new TunnelNavigationVolume(
            Width,
            Height,
            Depth,
            open,
            VerticalCells,
            DemoLayout);
    }
}

}
