using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Dig.Unity
{
    [CreateAssetMenu(
        fileName = "TerrainVisualCatalog",
        menuName = "Dig/Visual Catalogs/Terrain")]
    public sealed partial class DigTerrainVisualCatalog : DigVisualCatalog
    {
        [SerializeField]
        private DigTerrainVisualProfile[] profiles =
            Array.Empty<DigTerrainVisualProfile>();

        private Dictionary<string, DigTerrainVisualProfile>? _profileLookup;

        public int ProfileCount => profiles.Length;

        internal Material? ResolveSurface(
            string stableId,
            DigTerrainSurfaceRole role)
        {
            EnsureProfileLookup();
            if (!string.IsNullOrWhiteSpace(stableId)
                && _profileLookup!.TryGetValue(
                    stableId,
                    out DigTerrainVisualProfile? profile))
            {
                Material? surface = profile.Resolve(role);
                if (surface != null)
                {
                    return surface;
                }
            }

            return Resolve(stableId).Material;
        }

        public override IReadOnlyList<string> ValidateCatalog()
        {
            List<string> errors = new List<string>(base.ValidateCatalog());
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            bool[] kinds = new bool[6];
            for (int index = 0; index < profiles.Length; index++)
            {
                DigTerrainVisualProfile? profile = profiles[index];
                if (profile == null)
                {
                    errors.Add($"Terrain profile {index} is null.");
                    continue;
                }

                profile.AppendValidation(index, errors);
                if (!string.IsNullOrWhiteSpace(profile.StableId)
                    && !ids.Add(profile.StableId))
                {
                    errors.Add($"Duplicate terrain profile id '{profile.StableId}'.");
                }

                int kind = (int)profile.Kind;
                if (kind >= 0 && kind < kinds.Length)
                {
                    kinds[kind] = true;
                }
            }

            RequireKind(DigTerrainProfileKind.Sand, kinds, errors);
            RequireKind(DigTerrainProfileKind.Stone, kinds, errors);
            RequireKind(DigTerrainProfileKind.MetalBearing, kinds, errors);
            RequireKind(DigTerrainProfileKind.Crystalline, kinds, errors);
            RequireKind(DigTerrainProfileKind.Lava, kinds, errors);
            RequireKind(DigTerrainProfileKind.Unmineable, kinds, errors);
            AppendDepositValidation(errors);
            AppendCaveTemplateValidation(errors);
            return new ReadOnlyCollection<string>(errors);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _profileLookup = null;
            ResetDepositProfileLookup();
            ResetCaveTemplateProfileLookup();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _profileLookup = null;
            ResetDepositProfileLookup();
            ResetCaveTemplateProfileLookup();
        }

        private void EnsureProfileLookup()
        {
            if (_profileLookup != null)
            {
                return;
            }

            _profileLookup = new Dictionary<string, DigTerrainVisualProfile>(
                StringComparer.Ordinal);
            for (int index = 0; index < profiles.Length; index++)
            {
                DigTerrainVisualProfile? profile = profiles[index];
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
            DigTerrainProfileKind kind,
            IReadOnlyList<bool> kinds,
            ICollection<string> errors)
        {
            if (!kinds[(int)kind])
            {
                errors.Add($"Terrain catalog requires a {kind} profile.");
            }
        }
    }
}
