using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Dig.Unity
{
    [CreateAssetMenu(
        fileName = "ItemVisualCatalog",
        menuName = "Dig/Visual Catalogs/Items")]
    public sealed class DigItemVisualCatalog : DigVisualCatalog
    {
        [SerializeField]
        private DigItemVisualProfile[] profiles = Array.Empty<DigItemVisualProfile>();

        private Dictionary<string, DigItemVisualProfile>? _profileLookup;

        internal DigItemVisualResolution ResolveItem(string stableId)
        {
            EnsureProfileLookup();
            DigVisualAsset fallback = Resolve(stableId);
            if (!string.IsNullOrWhiteSpace(stableId)
                && _profileLookup!.TryGetValue(
                    stableId,
                    out DigItemVisualProfile? profile))
            {
                return profile.Resolve(fallback);
            }

            return new DigItemVisualResolution(
                fallback,
                icon: null,
                DigItemCarrySocketPolicy.None,
                new Vector3(0.34f, 0.34f, 0.34f),
                new Vector3(0.28f, 0.28f, 0.28f),
                DigItemRotationPolicy.StackQuarterTurns,
                DigItemColliderPolicy.InteractiveOnly,
                maxVisibleInstances: 4,
                hasProfile: false);
        }

        public override IReadOnlyList<string> ValidateCatalog()
        {
            List<string> errors = new List<string>(base.ValidateCatalog());
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigItemVisualProfile? profile = profiles[index];
                if (profile == null)
                {
                    errors.Add($"Item profile {index} is null.");
                    continue;
                }

                profile.AppendValidation(index, errors);
                if (!string.IsNullOrWhiteSpace(profile.StableId)
                    && !ids.Add(profile.StableId))
                {
                    errors.Add($"Duplicate item profile id '{profile.StableId}'.");
                }
            }

            return new ReadOnlyCollection<string>(errors);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _profileLookup = null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _profileLookup = null;
        }

        private void EnsureProfileLookup()
        {
            if (_profileLookup != null)
            {
                return;
            }

            _profileLookup = new Dictionary<string, DigItemVisualProfile>(
                StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigItemVisualProfile? profile = profiles[index];
                if (profile == null
                    || string.IsNullOrWhiteSpace(profile.StableId)
                    || _profileLookup.ContainsKey(profile.StableId))
                {
                    continue;
                }

                _profileLookup.Add(profile.StableId, profile);
            }
        }
    }
}
