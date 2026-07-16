using System;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentEquipmentVisual : MonoBehaviour
    {
        private string? _itemId;

        internal void Configure(string itemId, Material material)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Equipment item id is required.", nameof(itemId));
            }

            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            string normalized = itemId.Trim();
            if (string.Equals(_itemId, normalized, StringComparison.Ordinal)
                && transform.childCount > 0)
            {
                return;
            }

            Clear();
            _itemId = normalized;
            name = "Equipped " + normalized;
            transform.localPosition = new Vector3(0.58f, 0.04f, 0.08f);
            transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            CreatePart(
                "Handle",
                new Vector3(0f, 0f, 0f),
                new Vector3(0.09f, 0.62f, 0.09f),
                material);
            CreatePart(
                "Head",
                new Vector3(0f, 0.30f, 0f),
                new Vector3(0.38f, 0.10f, 0.12f),
                material);
        }

        internal void Clear()
        {
            _itemId = null;
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }

        private void CreatePart(
            string partName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(transform, worldPositionStays: false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}