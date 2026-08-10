using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal static class DigTunnelDemoRendererDepthSources
    {
        internal static void SetDepthExcavationSources(
            this DigTunnelDemoRenderer renderer,
            IReadOnlyCollection<CellId> sources)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            HashSet<CellId> allowed = new HashSet<CellId>(sources);
            DigTunnelCellVisual[] cells =
                renderer.GetComponentsInChildren<DigTunnelCellVisual>();
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index].SetCanExcavateDepth(
                    !cells[index].IsVerticalTunnel && allowed.Contains(cells[index].Cell));
            }
        }

        internal static bool TryGetCell(
            this DigTunnelDemoRenderer renderer,
            CellId cell,
            out DigTunnelCellVisual visual)
        {
            DigTunnelCellVisual[] cells =
                renderer.GetComponentsInChildren<DigTunnelCellVisual>();
            for (int index = 0; index < cells.Length; index++)
            {
                if (cells[index].Cell == cell)
                {
                    visual = cells[index];
                    return true;
                }
            }

            visual = null!;
            return false;
        }
    }
}
