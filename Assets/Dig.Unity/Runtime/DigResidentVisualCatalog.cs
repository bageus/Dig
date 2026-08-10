using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
    [CreateAssetMenu(
        fileName = "ResidentVisualCatalog",
        menuName = "Dig/Visual Catalogs/Residents")]
    public sealed class DigResidentVisualCatalog : DigVisualCatalog
    {
        [SerializeField]
        private DigResidentVisualProfile[] profiles =
            Array.Empty<DigResidentVisualProfile>();

        private Dictionary<string, DigResidentVisualProfile>? _profileLookup;

        public int ProfileCount => profiles.Length;

        internal DigResidentVisualResolution ResolveResident(string stableId)
        {
            DigVisualAsset fallback = Resolve(stableId);
            EnsureProfileLookup();
            if (!string.IsNullOrWhiteSpace(stableId)
                && _profileLookup!.TryGetValue(
                    stableId,
                    out DigResidentVisualProfile? profile))
            {
                return profile.Resolve(fallback);
            }

            return new DigResidentVisualResolution(
                fallback,
                ResidentBodyVariant.Neutral,
                Vector3.one,
                maximumRenderers: 12,
                hasProfile: false);
        }

        public override IReadOnlyList<string> ValidateCatalog()
        {
            List<string> errors = new List<string>(base.ValidateCatalog());
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigResidentVisualProfile? profile = profiles[index];
                if (profile == null)
                {
                    errors.Add($"Resident profile {index} is null.");
                    continue;
                }

                try
                {
                    profile.Validate();
                }
                catch (InvalidOperationException exception)
                {
                    errors.Add($"Resident profile {index}: {exception.Message}");
                }

                if (!string.IsNullOrWhiteSpace(profile.StableId)
                    && !ids.Add(profile.StableId))
                {
                    errors.Add($"Duplicate resident profile id '{profile.StableId}'.");
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

            _profileLookup = new Dictionary<string, DigResidentVisualProfile>(
                StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigResidentVisualProfile? profile = profiles[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.StableId))
                {
                    continue;
                }

                if (!_profileLookup.ContainsKey(profile.StableId))
                {
                    _profileLookup.Add(profile.StableId, profile);
                }
            }
        }
    }
}