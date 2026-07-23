using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private static readonly Color DepthDesignationColor =
            new Color(0.74f, 0.62f, 0.90f, 1f);

        internal void SetDepthDesignationTint(CellId target)
        {
            if (_cells.TryGetValue(target, out DigCellVisual? visual))
            {
                visual.SetDesignationTint(DepthDesignationColor);
            }
        }
    }
}
