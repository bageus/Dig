using System;
using UnityEngine;

namespace Dig.Unity
{
internal static class DigResidentAnimatedModel
{
    internal const string StableId = "resident.dwarf.hi3d.lowpoly70k.rigged";
    internal const string ResourcePath = "Residents/Dwarf_Hi3D_LowPoly_70k_Rigged";

    internal static bool TryResolveDefault(out DigVisualAsset asset)
    {
        GameObject prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning(
                $"Default resident visual resource '{ResourcePath}' could not be loaded as a GameObject. "
                + "The procedural resident fallback will be used. Check the glTF import in Unity.");
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
