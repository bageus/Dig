using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigTunnelCellVisual : MonoBehaviour
    {
        public CellId Cell { get; private set; }

        public bool IsVerticalTunnel { get; private set; }

        public bool CanExcavateDepth { get; private set; }

        internal void Configure(
            CellId cell,
            bool isVerticalTunnel,
            bool canExcavateDepth = false)
        {
            Cell = cell;
            IsVerticalTunnel = isVerticalTunnel;
            CanExcavateDepth = canExcavateDepth;
            name = isVerticalTunnel
                ? $"Vertical tunnel {cell}"
                : $"Tunnel {cell}";
        }

        internal void SetCanExcavateDepth(bool canExcavateDepth)
        {
            CanExcavateDepth = canExcavateDepth;
        }
    }
}
