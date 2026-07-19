using UnityEngine;

namespace Dig.Unity
{
    internal static class DigVisualPrefabFactory
    {
        internal static GameObject Create(
            DigVisualAsset asset,
            Transform parent,
            string instanceName,
            PrimitiveType fallbackPrimitive)
        {
            GameObject instance = asset.Prefab == null
                ? GameObject.CreatePrimitive(fallbackPrimitive)
                : Object.Instantiate(asset.Prefab);
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
            return instance;
        }
    }
}
