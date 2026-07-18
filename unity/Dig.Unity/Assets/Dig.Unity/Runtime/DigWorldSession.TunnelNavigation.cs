using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigWorldSession
    {
        private readonly HashSet<CellId> _plannedVerticalTunnelCells =
            new HashSet<CellId>();

        internal IReadOnlyList<CellId> PlannedVerticalTunnelCells =>
            _plannedVerticalTunnelCells.OrderBy(cell => cell).ToArray();

        internal void SetVerticalTunnelPlan(CellId cell, bool active)
        {
            if (active)
            {
                _plannedVerticalTunnelCells.Add(cell);
            }
            else
            {
                _plannedVerticalTunnelCells.Remove(cell);
            }
        }
    }
}
