using System;
using Dig.Domain.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentEquipmentVisual : MonoBehaviour
    {
        private string? _itemId;
        private EquipmentAppearanceKind _appearanceKind;

        internal void Configure(
            string itemId,
            EquipmentAppearanceKind appearanceKind,
            Material material)
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
                && _appearanceKind == appearanceKind
                && transform.childCount > 0)
            {
                return;
            }

            Clear();
            _itemId = normalized;
            _appearanceKind = appearanceKind;
            name = "Equipped " + normalized;
            bool construction = appearanceKind == EquipmentAppearanceKind.Construction;
            bool generic = appearanceKind == EquipmentAppearanceKind.Generic;
            transform.localPosition = new Vector3(0.58f, 0.04f, 0.08f);
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                construction ? -10f : -22f);
            CreatePart(
                "Handle",
                Vector3.zero,
                generic
                    ? new Vector3(0.12f, 0.52f, 0.12f)
                    : new Vector3(0.09f, 0.62f, 0.09f),
                material);
            if (!generic)
            {
                CreatePart(
                    construction ? "Construction Head" : "Mining Head",
                    new Vector3(0f, 0.30f, 0f),
                    construction
                        ? new Vector3(0.30f, 0.18f, 0.18f)
                        : new Vector3(0.38f, 0.10f, 0.12f),
                    material);
            }
        }

        internal void Clear()
        {
            _itemId = null;
            _appearanceKind = EquipmentAppearanceKind.Generic;
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
            part.layer = 2;
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
