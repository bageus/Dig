using System;
using System.Collections.Generic;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayAppearanceCatalog
    {
        [SerializeField]
        private DigOverlayAppearanceProfile[] profiles =
            Array.Empty<DigOverlayAppearanceProfile>();

        private Dictionary<OverlaySemanticKind, DigOverlayAppearanceProfile>?
            _lookup;

        internal DigOverlayAppearance Resolve(OverlaySemanticKind semantic)
        {
            EnsureLookup();
            return _lookup!.TryGetValue(
                semantic,
                out DigOverlayAppearanceProfile? profile)
                    ? profile.Resolve()
                    : DigOverlayAppearanceDefaults.Resolve(semantic);
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        private void OnValidate()
        {
            _lookup = null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<
                OverlaySemanticKind,
                DigOverlayAppearanceProfile>();
            for (int index = 0; index < profiles.Length; index++)
            {
                DigOverlayAppearanceProfile? profile = profiles[index];
                if (profile != null && !_lookup.ContainsKey(profile.Semantic))
                {
                    _lookup.Add(profile.Semantic, profile);
                }
            }
        }
    }
}
