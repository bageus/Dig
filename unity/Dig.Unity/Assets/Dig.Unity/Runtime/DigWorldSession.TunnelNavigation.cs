using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigWorldSession
    {
        private readonly HashSet<CellId> _plannedTunnelCells =
            new HashSet<CellId>();
        private readonly HashSet<CellId> _plannedVerticalTunnelCells =
            new HashSet<CellId>();

        internal IReadOnlyList<CellId> PlannedTunnelCells =>
            _plannedTunnelCells.OrderBy(cell => cell).ToArray();

        internal IReadOnlyList<CellId> PlannedVerticalTunnelCells =>
            _plannedVerticalTunnelCells.OrderBy(cell => cell).ToArray();

        internal bool IsPlannedHorizontalTunnel(CellId cell)
        {
            return _plannedTunnelCells.Contains(cell)
                && !_plannedVerticalTunnelCells.Contains(cell);
        }

        internal void SetTunnelPlan(CellId cell, bool active, bool vertical)
        {
            if (active)
            {
                _plannedTunnelCells.Add(cell);
                if (vertical)
                {
                    _plannedVerticalTunnelCells.Add(cell);
                }
                else
                {
                    _plannedVerticalTunnelCells.Remove(cell);
                }

                return;
            }

            _plannedTunnelCells.Remove(cell);
            _plannedVerticalTunnelCells.Remove(cell);
        }

        internal void SetVerticalTunnelPlan(CellId cell, bool active)
        {
            SetTunnelPlan(cell, active, vertical: active);
        }

        private void InitializeDemoTunnelPlan(TunnelDemoLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            for (int y = layout.SurfaceY; y <= layout.CaveFloorY; y++)
            {
                CellId shaft = new CellId(layout.ShaftX, y);
                _plannedTunnelCells.Add(shaft);
                _plannedVerticalTunnelCells.Add(shaft);
            }

            int corridorMinX = Math.Min(layout.ShaftX, layout.CaveMinX);
            int corridorMaxX = Math.Max(layout.ShaftX, layout.CaveMinX);
            for (int x = corridorMinX; x <= corridorMaxX; x++)
            {
                CellId corridor = new CellId(x, layout.CaveFloorY);
                _plannedTunnelCells.Add(corridor);
                if (x != layout.ShaftX)
                {
                    _plannedVerticalTunnelCells.Remove(corridor);
                }
            }
        }
    }
}
