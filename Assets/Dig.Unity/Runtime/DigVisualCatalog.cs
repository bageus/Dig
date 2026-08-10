using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Dig.Unity
{
    public abstract class DigVisualCatalog : ScriptableObject
    {
        [SerializeField]
        private DigVisualCatalogEntry[] entries = Array.Empty<DigVisualCatalogEntry>();

        [SerializeField]
        private GameObject? fallbackPrefab;

        [SerializeField]
        private Material? fallbackMaterial;

        [SerializeField]
        private Color fallbackTint = Color.magenta;

        private Dictionary<string, DigVisualCatalogEntry>? _lookup;

        public int EntryCount => entries.Length;

        public DigVisualAsset Resolve(string stableId)
        {
            EnsureLookup();
            if (!string.IsNullOrWhiteSpace(stableId)
                && _lookup!.TryGetValue(stableId, out DigVisualCatalogEntry? entry)
                && entry.HasUsableContent
                && entry.HasValidPrefabRoot)
            {
                return entry.CreateAsset();
            }

            return CreateFallback(stableId);
        }

        public virtual IReadOnlyList<string> ValidateCatalog()
        {
            List<string> errors = new List<string>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Length; index++)
            {
                DigVisualCatalogEntry? entry = entries[index];
                if (entry == null)
                {
                    errors.Add($"Entry {index} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.StableId))
                {
                    errors.Add($"Entry {index} has no stable id.");
                    continue;
                }

                if (!ids.Add(entry.StableId))
                {
                    errors.Add($"Duplicate stable id '{entry.StableId}'.");
                }

                if (!entry.HasUsableContent)
                {
                    errors.Add(
                        $"Entry '{entry.StableId}' has neither prefab nor material.");
                }

                if (!entry.HasValidPrefabRoot)
                {
                    errors.Add(
                        $"Prefab for '{entry.StableId}' requires DigVisualPrefabRoot.");
                }
            }

            if (fallbackPrefab != null
                && fallbackPrefab.GetComponent<DigVisualPrefabRoot>() == null)
            {
                errors.Add("Fallback prefab requires DigVisualPrefabRoot.");
            }

            return new ReadOnlyCollection<string>(errors);
        }

        private DigVisualAsset CreateFallback(string stableId)
        {
            GameObject? prefab = fallbackPrefab;
            if (prefab != null && prefab.GetComponent<DigVisualPrefabRoot>() == null)
            {
                prefab = null;
            }

            return new DigVisualAsset(
                string.IsNullOrWhiteSpace(stableId) ? "<missing>" : stableId,
                prefab,
                fallbackMaterial,
                fallbackTint,
                isFallback: true);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, DigVisualCatalogEntry>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Length; index++)
            {
                DigVisualCatalogEntry? entry = entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.StableId))
                {
                    continue;
                }

                if (!_lookup.ContainsKey(entry.StableId))
                {
                    _lookup.Add(entry.StableId, entry);
                }
            }
        }

        protected virtual void OnEnable()
        {
            _lookup = null;
        }

        protected virtual void OnValidate()
        {
            _lookup = null;
        }
    }
}
