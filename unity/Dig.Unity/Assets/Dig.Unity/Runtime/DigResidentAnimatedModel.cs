using System;
using UnityEngine;

namespace Dig.Unity
{
internal static class DigResidentAnimatedModel
{
    internal const string StableId = "resident.male.blackbeard";
    internal const string ResourcePath = "Residents/MaleBlackbeardDwarf";

    internal static bool TryResolveDefault(out DigVisualAsset asset)
    {
        GameObject prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null)
        {
            asset = default;
            return false;
        }

        asset = new DigVisualAsset(
            StableId,
            prefab,
            material: null,
            tint: Color.white,
            isFallback: false);
        return true;
    }

    internal static AnimationClip[] LoadAnimationClips()
    {
        return Resources.LoadAll<AnimationClip>(ResourcePath);
    }

    internal static bool IsDefaultAsset(string stableId)
    {
        return string.Equals(stableId, StableId, StringComparison.Ordinal);
    }
}
}
