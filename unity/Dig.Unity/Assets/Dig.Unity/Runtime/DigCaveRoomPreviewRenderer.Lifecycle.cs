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
    }
}
