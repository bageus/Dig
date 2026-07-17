using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigTunnelCellVisual : MonoBehaviour
    {
        public SpatialCellId Cell { get; private set; }

        public bool IsVerticalTunnel { get; private set; }

        internal void Configure(
            SpatialCellId cell,
            bool isVerticalTunnel,
            Material material)
        {
            Cell = cell;
            IsVerticalTunnel = isVerticalTunnel;
            name = isVerticalTunnel
                ? $"Vertical tunnel {cell}"
                : $"Tunnel {cell}";
            GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
