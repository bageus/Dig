using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigTerrainVisualCatalog
    {
        [SerializeField]
        private DigTerrainDepositVisualProfile[] depositProfiles =
            Array.Empty<DigTerrainDepositVisualProfile>();

        private Dictionary<string, DigTerrainDepositVisualProfile>?
            _depositProfileLookup;

        public int DepositProfileCount => depositProfiles.Length;

        internal Material? ResolveDeposit(
            string stableId,
            TerrainDepositVisualState state)
        {
            if (state != TerrainDepositVisualState.Revealed
                && state != TerrainDepositVisualState.Damaged)
            {
                return null;
            }

            EnsureDepositProfileLookup();
            if (!string.IsNullOrWhiteSpace(stableId)
                && _depositProfileLookup!.TryGetValue(
                    stableId,
                    out DigTerrainDepositVisualProfile? profile))
            {
                Material? material = profile.Resolve(state);
                if (material != null)
                {
                    return material;
                }
            }

            return Resolve(stableId).Material;
        }

        private void AppendDepositValidation(ICollection<string> errors)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            bool[] kinds = new bool[5];
            for (int index = 0; index < depositProfiles.Length; index++)
            {
                DigTerrainDepositVisualProfile? profile = depositProfiles[index];
                if (profile == null)
                {
                    errors.Add($"Deposit profile {index} is null.");
                    continue;
                }

                profile.AppendValidation(index, errors);
                if (!string.IsNullOrWhiteSpace(profile.StableId)
                    && !ids.Add(profile.StableId))
                {
                    errors.Add(
                        $"Duplicate deposit profile id '{profile.StableId}'.");
                }

                int kind = (int)profile.Kind;
                if (kind >= 0 && kind < kinds.Length)
                {
                    kinds[kind] = true;
                }
            }

            RequireDepositKind(
                DigTerrainDepositProfileKind.Iron,
                kinds,
                errors);
            RequireDepositKind(
                DigTerrainDepositProfileKind.Gold,
                kinds,
                errors);
            RequireDepositKind(
                DigTerrainDepositProfileKind.Crystal,
                kinds,
                errors);
            RequireDepositKind(
                DigTerrainDepositProfileKind.Coal,
                kinds,
                errors);
            RequireDepositKind(
                DigTerrainDepositProfileKind.Stone,
                kinds,
                errors);
        }

        private void ResetDepositProfileLookup()
        {
            _depositProfileLookup = null;
        }

        private void EnsureDepositProfileLookup()
        {
            if (_depositProfileLookup != null)
            {
                return;
            }

            _depositProfileLookup =
                new Dictionary<string, DigTerrainDepositVisualProfile>(
                    StringComparer.Ordinal);
            for (int index = 0; index < depositProfiles.Length; index++)
            {
                DigTerrainDepositVisualProfile? profile = depositProfiles[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.StableId))
                {
                    continue;
                }

                if (!_depositProfileLookup.ContainsKey(profile.StableId))
                {
                    _depositProfileLookup.Add(profile.StableId, profile);
                }
            }
        }

        private static void RequireDepositKind(
            DigTerrainDepositProfileKind kind,
            IReadOnlyList<bool> kinds,
            ICollection<string> errors)
        {
            if (!kinds[(int)kind])
            {
                errors.Add($"Terrain catalog requires a {kind} deposit profile.");
            }
        }
    }
}
