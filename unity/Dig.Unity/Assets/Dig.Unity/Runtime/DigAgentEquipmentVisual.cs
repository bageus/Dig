using System;
using Dig.Domain.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentEquipmentVisual : MonoBehaviour
    {
        private const string ClubItemId = "weapon.club";
        private const string PickaxeVisualId = "visual.work_tool.pickaxe";
        private const string AxeVisualId = "visual.work_tool.axe";
        private const string HammerVisualId = "visual.work_tool.hammer";

        private string? _itemId;
        private EquipmentAppearanceKind _appearanceKind;

        internal string? CurrentItemId => _itemId;

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
            transform.localPosition = new Vector3(0.58f, 0.04f, 0.08f);
            transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreatePart(
                "Handle",
                Vector3.zero,
                new Vector3(0.10f, 0.62f, 0.10f),
                material);

            if (string.Equals(normalized, ClubItemId, StringComparison.Ordinal))
            {
                CreatePart(
                    "Club Head",
                    new Vector3(0f, 0.25f, 0f),
                    new Vector3(0.20f, 0.30f, 0.20f),
                    material);
                return;
            }

            if (string.Equals(normalized, PickaxeVisualId, StringComparison.Ordinal))
            {
                CreatePart(
                    "Pickaxe Head",
                    new Vector3(0f, 0.31f, 0f),
                    new Vector3(0.48f, 0.09f, 0.10f),
                    material);
                CreatePart(
                    "Pickaxe Point",
                    new Vector3(0.23f, 0.27f, 0f),
                    new Vector3(0.14f, 0.07f, 0.07f),
                    material,
                    new Vector3(0f, 0f, -28f));
                return;
            }

            if (string.Equals(normalized, AxeVisualId, StringComparison.Ordinal))
            {
                CreatePart(
                    "Axe Blade",
                    new Vector3(0.15f, 0.28f, 0f),
                    new Vector3(0.28f, 0.22f, 0.08f),
                    material,
                    new Vector3(0f, 0f, -12f));
                return;
            }

            if (string.Equals(normalized, HammerVisualId, StringComparison.Ordinal))
            {
                CreatePart(
                    "Hammer Head",
                    new Vector3(0f, 0.30f, 0f),
                    new Vector3(0.34f, 0.18f, 0.18f),
                    material);
                return;
            }

            bool construction = appearanceKind == EquipmentAppearanceKind.Construction;
            bool generic = appearanceKind == EquipmentAppearanceKind.Generic;
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
            Material material,
            Vector3? localEulerAngles = null)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.layer = 2;
            part.transform.SetParent(transform, worldPositionStays: false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(
                localEulerAngles ?? Vector3.zero);
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
