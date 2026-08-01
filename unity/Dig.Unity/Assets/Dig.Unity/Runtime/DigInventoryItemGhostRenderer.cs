using System;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigInventoryItemGhostRenderer : MonoBehaviour
    {
        private const string CatalogResourcePath = "Dig/VisualCatalogs/Items";
        private static readonly Color ValidTint = new Color(0.58f, 0.92f, 0.68f, 0.48f);
        private static readonly Color InvalidTint = new Color(0.96f, 0.30f, 0.24f, 0.48f);

        private DigItemVisualCatalog? _catalog;
        private Transform? _root;
        private GameObject? _instance;
        private DigVisualTintTarget? _tint;
        private string _itemId = string.Empty;
        private DigItemVisualResolution _resolution;

        private void Awake()
        {
            _catalog = Resources.Load<DigItemVisualCatalog>(CatalogResourcePath);
        }

        internal void Render(string itemId, Dig.Domain.World.CellId cell, bool valid)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Item id is required.", nameof(itemId));
            }

            EnsureRoot();
            if (_instance == null || !string.Equals(_itemId, itemId, StringComparison.Ordinal))
            {
                Rebuild(itemId);
            }

            _root!.rotation = Quaternion.identity;
            _root.localScale = Vector3.one;
            _root.gameObject.SetActive(true);
            DigWorldItemGrounding.PlaceOnFloor(
                _root,
                DigWorldItemVisualPolicy.ResolveFloorAnchor(cell, Vector2.zero));
            _tint!.SetTint(valid ? ValidTint : InvalidTint);
        }

        internal void Clear()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("Inventory item placement ghost").transform;
            _root.SetParent(transform, worldPositionStays: true);
        }

        private void Rebuild(string itemId)
        {
            if (_instance != null)
            {
                Destroy(_instance);
            }

            _itemId = itemId;
            _resolution = DigWorldItemVisualPolicy.Resolve(_catalog, itemId);
            _instance = DigBasketVisualPolicy.CreateInstance(
                itemId,
                _resolution,
                _root!,
                $"Inventory item ghost {itemId}");
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
            _instance.transform.localScale = _resolution.WorldScale;
            DisableColliders(_instance);
            SetLayerRecursively(_instance, layer: 2);
            _tint = _instance.GetComponent<DigVisualTintTarget>();
            DigTransparentVisualSurface transparent =
                _instance.GetComponent<DigTransparentVisualSurface>()
                ?? _instance.AddComponent<DigTransparentVisualSurface>();
            transparent.Configure(fixedOpacity: 0.48f);
        }

        private static void DisableColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = false;
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int index = 0; index < transforms.Length; index++)
            {
                transforms[index].gameObject.layer = layer;
            }
        }
    }
}
