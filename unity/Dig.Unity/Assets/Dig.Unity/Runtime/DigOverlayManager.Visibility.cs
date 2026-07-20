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
            _visibilityOverrides[layer] = !IsVisible(layer);
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            _snapshot = _visibility.CreateSnapshot(
                visibilityProfile,
                _visibilityOverrides);
            foreach (KeyValuePair<OverlayLayerKind, List<Transform>> pair in _roots)
            {
                bool visible = _snapshot.IsVisible(pair.Key);
                for (int index = 0; index < pair.Value.Count; index++)
                {
                    Transform root = pair.Value[index];
                    if (root != null)
                    {
                        root.gameObject.SetActive(visible);
                    }
                }
            }
        }
    }
}
