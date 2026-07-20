using System;
using System.Collections.Generic;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayManager
    {
        [SerializeField]
        private OverlayVisibilityProfile visibilityProfile =
            OverlayVisibilityProfile.Debug;

        private readonly Dictionary<OverlayLayerKind, Transform> _roots =
            new Dictionary<OverlayLayerKind, Transform>();
        private readonly Dictionary<OverlayLayerKind, bool> _visibilityOverrides =
            new Dictionary<OverlayLayerKind, bool>();
        private readonly OverlayVisibilityResolver _visibility =
            OverlayVisibilityResolver.CreateDefault();
        private OverlayVisibilitySnapshot? _snapshot;

        public OverlayVisibilityProfile VisibilityProfile => visibilityProfile;

        public void RegisterLayer(OverlayLayerKind layer, Transform root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _roots[layer] = root;
            ApplyVisibility();
        }

        public void UnregisterLayer(OverlayLayerKind layer, Transform root)
        {
            if (_roots.TryGetValue(layer, out Transform? registered)
                && registered == root)
            {
                _roots.Remove(layer);
            }
        }

        public int ResolveSortingOrder(OverlayLayerKind layer)
        {
            return _visibility.ResolveLayer(layer).Priority;
        }
    }
}
