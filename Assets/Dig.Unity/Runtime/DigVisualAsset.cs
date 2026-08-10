using UnityEngine;

namespace Dig.Unity
{
    public readonly struct DigVisualAsset
    {
        public DigVisualAsset(
            string stableId,
            GameObject? prefab,
            Material? material,
            Color tint,
            bool isFallback)
        {
            StableId = stableId;
            Prefab = prefab;
            Material = material;
            Tint = tint;
            IsFallback = isFallback;
        }

        public string StableId { get; }

        public GameObject? Prefab { get; }

        public Material? Material { get; }

        public Color Tint { get; }

        public bool IsFallback { get; }

        public static DigVisualAsset CreateRuntimeFallback(
            string stableId,
            Color tint)
        {
            return new DigVisualAsset(
                stableId,
                prefab: null,
                material: null,
                tint,
                isFallback: true);
        }
    }
}
