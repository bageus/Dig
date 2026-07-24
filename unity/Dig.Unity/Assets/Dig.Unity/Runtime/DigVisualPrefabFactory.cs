using System;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigVisualPrefabFactory
    {
        private const float PlannedBuildingBoxOpacity = 0.48f;

        internal static GameObject Create(
            DigVisualAsset asset,
            Transform parent,
            string instanceName,
            PrimitiveType fallbackPrimitive)
        {
            GameObject instance = asset.Prefab == null
                ? GameObject.CreatePrimitive(fallbackPrimitive)
                : UnityEngine.Object.Instantiate(asset.Prefab);
            instance.name = instanceName;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            DigVisualPrefabRoot root = instance.GetComponent<DigVisualPrefabRoot>();
            if (root == null)
            {
                root = instance.AddComponent<DigVisualPrefabRoot>();
            }

            DigVisualTintTarget tint = instance.GetComponent<DigVisualTintTarget>();
            if (tint == null)
            {
                tint = instance.AddComponent<DigVisualTintTarget>();
            }

            tint.Configure(asset.Material, asset.Tint);
            bool placementGhost = instanceName.StartsWith(
                "Building ghost ",
                StringComparison.Ordinal);
            bool plannedBuildingBox = instanceName.EndsWith(
                " BuildingBox",
                StringComparison.Ordinal);
            if (placementGhost || plannedBuildingBox)
            {
                DigTransparentVisualSurface surface =
                    instance.GetComponent<DigTransparentVisualSurface>()
                    ?? instance.AddComponent<DigTransparentVisualSurface>();
                surface.Configure(plannedBuildingBox
                    ? PlannedBuildingBoxOpacity
                    : (float?)null);
            }

            return instance;
        }
    }
}
