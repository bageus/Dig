using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
    [CreateAssetMenu(
        fileName = "CreatureVisualCatalog",
        menuName = "Dig/Visual Catalogs/Creatures")]
    public sealed class DigCreatureVisualCatalog : DigVisualCatalog
    {
        [SerializeField]
        private DigCreatureVisualProfile[] profiles =
            Array.Empty<DigCreatureVisualProfile>();

        private Dictionary<string, DigCreatureVisualProfile>? _profileLookup;

        public int ProfileCount => profiles.Length;

        internal DigCreatureVisualResolution ResolveCreature(
            string stableSpeciesId,
            CreatureVisualFamily fallbackFamily,
            string fallbackRigId)
        {
            DigVisualAsset fallback = Resolve(stableSpeciesId);
            EnsureProfileLookup();
            if (!string.IsNullOrWhiteSpace(stableSpeciesId)
                && _profileLookup!.TryGetValue(
                    stableSpeciesId,
                    out DigCreatureVisualProfile? profile))
            {
                return profile.Resolve(fallback);
            }

            return new DigCreatureVisualResolution(
                fallback,
                fallbackRigId,
                fallbackFamily,
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
                DigCreatureVisualProfile? profile = profiles[index];
                if (profile == null)
                {
                    errors.Add($"Creature profile {index} is null.");
                    continue;
                }

                try
                {
                    profile.Validate();
                }
                catch (InvalidOperationException exception)
                {
                    errors.Add($"Creature profile {index}: {exception.Message}");
                }

                if (!string.IsNullOrWhiteSpace(profile.StableSpeciesId)
                    && !ids.Add(profile.StableSpeciesId))
                {
                    errors.Add(
                        $"Duplicate creature profile id '{profile.StableSpeciesId}'.");
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

            _profileLookup = new Dictionary<string, DigCreatureVisualProfile>(
                StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigCreatureVisualProfile? profile = profiles[index];
                if (profile == null
                    || string.IsNullOrWhiteSpace(profile.StableSpeciesId))
                {
                    continue;
                }

                if (!_profileLookup.ContainsKey(profile.StableSpeciesId))
                {
                    _profileLookup.Add(profile.StableSpeciesId, profile);
                }
            }
        }
    }
}