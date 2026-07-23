using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private void Awake()
        {
            _overlays = GetComponent<DigOverlayManager>()
                ?? gameObject.AddComponent<DigOverlayManager>();
        }

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

            if (_fillRenderer != null)
            {
                _fillRenderer.enabled = false;
            }
        }
    }
}
