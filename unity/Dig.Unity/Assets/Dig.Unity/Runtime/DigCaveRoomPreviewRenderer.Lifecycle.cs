using System;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        internal void Initialize(DigOverlayManager overlays)
        {
            _overlays = overlays
                ?? throw new ArgumentNullException(nameof(overlays));
        }

        internal void Clear()
        {
            for (int index = 0; index < _edges.Count; index++)
            {
                _edges[index].enabled = false;
            }
        }
    }
}
