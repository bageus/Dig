using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigCaveRoomPreviewRenderer : MonoBehaviour
    {
        private const int BoxEdgeCount = 12;
        private const int EdgeCount = 14;

        private readonly List<LineRenderer> _edges =
            new List<LineRenderer>(EdgeCount);
        private Transform? _root;
        private DigOverlayManager? _overlays;
    }
}
