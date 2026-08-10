using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigTerrainVisualCatalog
    {
        [SerializeField]
        private DigCaveTemplateVisualProfile[] caveTemplateProfiles =
            Array.Empty<DigCaveTemplateVisualProfile>();

        private Dictionary<string, DigCaveTemplateVisualProfile>?
            _caveTemplateProfileLookup;

        public int CaveTemplateProfileCount => caveTemplateProfiles.Length;

        internal Material? ResolveCaveTemplate(
            string stableId,
            CaveTemplateTrimRole role)
        {
            EnsureCaveTemplateProfileLookup();
            if (!string.IsNullOrWhiteSpace(stableId)
                && _caveTemplateProfileLookup!.TryGetValue(
                    stableId,
                    out DigCaveTemplateVisualProfile? profile))
            {
                Material? material = profile.Resolve(role);
                if (material != null)
                {
                    return material;
                }
            }

            return Resolve(stableId).Material;
        }

        private void AppendCaveTemplateValidation(ICollection<string> errors)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            bool[] kinds = new bool[4];
            for (int index = 0; index < caveTemplateProfiles.Length; index++)
            {
                DigCaveTemplateVisualProfile? profile = caveTemplateProfiles[index];
                if (profile == null)
                {
                    errors.Add($"Cave template profile {index} is null.");
                    continue;
                }

                profile.AppendValidation(index, errors);
                if (!string.IsNullOrWhiteSpace(profile.StableId)
                    && !ids.Add(profile.StableId))
                {
                    errors.Add(
                        $"Duplicate cave template profile id '{profile.StableId}'.");
                }

                int kind = (int)profile.Kind;
                if (kind >= 0 && kind < kinds.Length)
                {
                    kinds[kind] = true;
                }
            }

            RequireCaveTemplateKind(DigCaveTemplateProfileKind.Small, kinds, errors);
            RequireCaveTemplateKind(DigCaveTemplateProfileKind.Medium, kinds, errors);
            RequireCaveTemplateKind(DigCaveTemplateProfileKind.Large, kinds, errors);
            RequireCaveTemplateKind(DigCaveTemplateProfileKind.Tall, kinds, errors);
        }

        private void ResetCaveTemplateProfileLookup()
        {
            _caveTemplateProfileLookup = null;
        }

        private void EnsureCaveTemplateProfileLookup()
        {
            if (_caveTemplateProfileLookup != null)
            {
                return;
            }

            _caveTemplateProfileLookup =
                new Dictionary<string, DigCaveTemplateVisualProfile>(
                    StringComparer.Ordinal);
            for (int index = 0; index < caveTemplateProfiles.Length; index++)
            {
                DigCaveTemplateVisualProfile? profile = caveTemplateProfiles[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.StableId))
                {
                    continue;
                }

                if (!_caveTemplateProfileLookup.ContainsKey(profile.StableId))
                {
                    _caveTemplateProfileLookup.Add(profile.StableId, profile);
                }
            }
        }

        private static void RequireCaveTemplateKind(
            DigCaveTemplateProfileKind kind,
            IReadOnlyList<bool> kinds,
            ICollection<string> errors)
        {
            if (!kinds[(int)kind])
            {
                errors.Add(
                    $"Terrain catalog requires a {kind} cave template profile.");
            }
        }
    }
}
