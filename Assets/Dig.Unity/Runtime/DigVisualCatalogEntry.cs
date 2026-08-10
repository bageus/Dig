using System;
using UnityEngine;

namespace Dig.Unity
{
    [Serializable]
    public sealed class DigVisualCatalogEntry
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private GameObject? prefab;

        [SerializeField]
        private Material? material;

        [SerializeField]
        private Color tint = Color.white;

        public string StableId => stableId;

        public GameObject? Prefab => prefab;

        public Material? Material => material;

        public Color Tint => tint;

        internal bool HasUsableContent => prefab != null || material != null;

        internal bool HasValidPrefabRoot => prefab == null
            || prefab.GetComponent<DigVisualPrefabRoot>() != null;

        internal DigVisualAsset CreateAsset()
        {
            return new DigVisualAsset(
                stableId,
                prefab,
                material,
                tint,
                isFallback: false);
        }
    }
}
