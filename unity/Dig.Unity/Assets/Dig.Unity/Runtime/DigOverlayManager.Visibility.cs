using System;
using System.Collections.Generic;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayManager
    {
        public void SetVisibilityProfile(OverlayVisibilityProfile profile)
        {
            if (!Enum.IsDefined(typeof(OverlayVisibilityProfile), profile))
            {
                throw new ArgumentOutOfRangeException(nameof(profile));
            }

            visibilityProfile = profile;
            ApplyVisibility();
        }

        public bool IsVisible(OverlayLayerKind layer)
        {
            return _snapshot != null && _snapshot.IsVisible(layer);
        }

        public void ToggleLayer(OverlayLayerKind layer)
        {
            bool next = !IsVisible(layer);
            _visibilityOverrides[layer] = next;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            _snapshot = _visibility.CreateSnapshot(
                visibilityProfile,
                _visibilityOverrides);
            foreach (KeyValuePair<OverlayLayerKind, Transform> pair in _roots)
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(_snapshot.IsVisible(pair.Key));
                }
            }
        }
    }
}
