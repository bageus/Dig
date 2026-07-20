using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    [CreateAssetMenu(
        fileName = "BuildingVisualCatalog",
        menuName = "Dig/Visual Catalogs/Buildings")]
    public sealed class DigBuildingVisualCatalog : DigVisualCatalog
    {
        [SerializeField]
        private DigBuildingVisualProfile[] profiles =
            Array.Empty<DigBuildingVisualProfile>();

        private Dictionary<string, DigBuildingVisualProfile>? _profileLookup;

        public int ProfileCount => profiles.Length;

        internal DigBuildingVisualResolution ResolveBuilding(
            string stableId,
            BuildingVisualState state)
        {
            DigVisualAsset fallback = Resolve(stableId);
            EnsureProfileLookup();
            if (!string.IsNullOrWhiteSpace(stableId)
                && _profileLookup!.TryGetValue(
                    stableId,
                    out DigBuildingVisualProfile? profile))
            {
                return profile.Resolve(state, fallback);
            }

            return new DigBuildingVisualResolution(
                fallback,
                Vector2Int.one,
                Vector2.zero,
                hasProfile: false);
        }

        public override IReadOnlyList<string> ValidateCatalog()
        {
            List<string> errors = new List<string>(base.ValidateCatalog());
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            bool[] kinds = new bool[3];
            for (int index = 0; index < profiles.Length; index++)
            {
                DigBuildingVisualProfile? profile = profiles[index];
                if (profile == null)
                {
                    errors.Add($"Building profile {index} is null.");
                    continue;
                }

                profile.AppendValidation(index, errors);
                if (!string.IsNullOrWhiteSpace(profile.StableId)
                    && !ids.Add(profile.StableId))
                {
                    errors.Add(
                        $"Duplicate building profile id '{profile.StableId}'.");
                }

                int kind = (int)profile.Kind;
                if (kind >= 0 && kind < kinds.Length)
                {
                    kinds[kind] = true;
                }
            }

            RequireKind(DigBuildingProfileKind.Campfire, kinds, errors);
            RequireKind(DigBuildingProfileKind.Furnace, kinds, errors);
            RequireKind(DigBuildingProfileKind.Storage, kinds, errors);
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

            _profileLookup = new Dictionary<string, DigBuildingVisualProfile>(
                StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigBuildingVisualProfile? profile = profiles[index];
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

        private static void RequireKind(
            DigBuildingProfileKind kind,
            IReadOnlyList<bool> kinds,
            ICollection<string> errors)
        {
            if (!kinds[(int)kind])
            {
                errors.Add($"Building catalog requires a {kind} profile.");
            }
        }
    }
}
