using System;
using System.Collections.Generic;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DigWorldItemVisual : MonoBehaviour
    {
        private readonly List<GameObject> _instances = new List<GameObject>();
        private readonly List<DigVisualTintTarget> _tints =
            new List<DigVisualTintTarget>();
        private BoxCollider? _interactionCollider;
        private string _assetKey = string.Empty;
        private Color _baseTint = Color.white;
        private int _poolCapacity;

        public WorldItemViewModel Model { get; private set; } = null!;

        internal string QuantityBadge { get; private set; } = string.Empty;
        internal int VisibleInstanceCount { get; private set; }
        internal int RebuildCount { get; private set; }

        internal void InvalidateAsset()
        {
            _assetKey = string.Empty;
        }

        internal void Configure(
            WorldItemViewModel model,
            ItemStackVisualLayoutViewModel layout,
            DigItemVisualResolution resolution)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            EnsureCollider();
            EnsurePool(resolution);
            ApplyRoot(layout, resolution);
            ApplyInstances(layout, resolution);
            ApplyInteraction(resolution);
        }

        internal void PrepareForPool()
        {
            EnsureCollider();
            _interactionCollider!.enabled = false;
            gameObject.layer = 2;
            for (int index = 0; index < _instances.Count; index++)
            {
                _instances[index].SetActive(false);
            }

            VisibleInstanceCount = 0;
            QuantityBadge = string.Empty;
            gameObject.SetActive(false);
        }

        private void EnsureCollider()
        {
            _interactionCollider ??= GetComponent<BoxCollider>();
            _interactionCollider.isTrigger = false;
        }

        private void EnsurePool(DigItemVisualResolution resolution)
        {
            bool rawMaterial = TryResolveRawMaterialTint(Model.ItemId, out Color materialTint);
            string key = rawMaterial
                ? $"raw-material:{Model.ItemId}"
                : resolution.Asset.StableId;
            int capacity = rawMaterial
                ? 1
                : Mathf.Clamp(resolution.MaxVisibleInstances, 1, 4);
            if (_assetKey == key && _poolCapacity == capacity && _instances.Count == capacity)
            {
                return;
            }

            ClearChildren();
            _assetKey = key;
            _poolCapacity = capacity;
            _baseTint = rawMaterial ? materialTint : resolution.Asset.Tint;
            for (int index = 0; index < capacity; index++)
            {
                GameObject instance = rawMaterial
                    ? CreateRawMaterialLump(resolution, index)
                    : DigVisualPrefabFactory.Create(
                        resolution.Asset,
                        transform,
                        $"Item instance {index}",
                        PrimitiveType.Cube);
                SetLayerRecursively(instance, layer: 2);
                DisableColliders(instance);
                _instances.Add(instance);
                _tints.Add(instance.GetComponent<DigVisualTintTarget>());
            }

            RebuildCount++;
        }

        private GameObject CreateRawMaterialLump(
            DigItemVisualResolution resolution,
            int index)
        {
            GameObject instance = DigVisualPrefabFactory.Create(
                resolution.Asset,
                transform,
                $"Raw material lump {index}",
                PrimitiveType.Cube);
            instance.transform.localRotation = Quaternion.Euler(17f, 31f, 9f);
            instance.transform.localScale = new Vector3(1.12f, 0.86f, 0.97f);
            DigVisualTintTarget tint = instance.GetComponent<DigVisualTintTarget>();
            tint.Configure(resolution.Asset.Material, _baseTint);
            return instance;
        }

        private void ApplyRoot(
            ItemStackVisualLayoutViewModel layout,
            DigItemVisualResolution resolution)
        {
            transform.position = DigTunnelProjection.ResidentWorldPosition(
                Model.CellX,
                Model.CellY,
                Model.CellZ) + new Vector3(0f, 0.22f, 0f);
            transform.rotation = layout.ReservationState switch
            {
                ItemReservationVisualState.Partial => Quaternion.Euler(0f, 0f, 3f),
                ItemReservationVisualState.Full => Quaternion.Euler(0f, 0f, 8f),
                _ => Quaternion.identity,
            };
            transform.localScale = layout.ReservationState switch
            {
                ItemReservationVisualState.Partial => new Vector3(1f, 1.06f, 1f),
                ItemReservationVisualState.Full => new Vector3(1f, 0.82f, 1f),
                _ => Vector3.one,
            };
            QuantityBadge = layout.QuantityBadge;
            name = $"World item {Model.ItemId} x{layout.QuantityBadge} "
                + $"reserved {layout.ReservedQuantity}";

            float width = Mathf.Max(resolution.WorldScale.x, resolution.WorldScale.z);
            _interactionCollider!.center = new Vector3(
                0f,
                resolution.WorldScale.y * 0.5f,
                0f);
            _interactionCollider.size = new Vector3(
                Mathf.Max(0.28f, width * 1.9f),
                Mathf.Max(0.28f, resolution.WorldScale.y * 1.5f),
                Mathf.Max(0.28f, width * 1.9f));
        }

        private void ApplyInstances(
            ItemStackVisualLayoutViewModel layout,
            DigItemVisualResolution resolution)
        {
            bool rawMaterial = TryResolveRawMaterialTint(Model.ItemId, out _);
            int visible = rawMaterial
                ? Math.Min(1, _instances.Count)
                : Math.Min(layout.Instances.Count, _instances.Count);
            Color tint = ResolveReservationTint(layout.ReservationState);
            for (int index = 0; index < _instances.Count; index++)
            {
                GameObject instance = _instances[index];
                bool active = index < visible;
                instance.SetActive(active);
                if (!active)
                {
                    continue;
                }

                if (rawMaterial)
                {
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.Euler(17f, 31f, 9f);
                    instance.transform.localScale = Vector3.Scale(
                        resolution.WorldScale,
                        new Vector3(1.12f, 0.86f, 0.97f));
                }
                else
                {
                    ItemStackLayoutInstanceViewModel placement = layout.Instances[index];
                    float scaleFactor = placement.ScalePercent / 100f;
                    instance.transform.localPosition = new Vector3(
                        placement.OffsetXPercent * 0.01f,
                        index * 0.025f,
                        placement.OffsetYPercent * 0.01f);
                    instance.transform.localRotation = ResolveRotation(
                        resolution.RotationPolicy,
                        layout.QuantityBand,
                        placement.QuarterTurns);
                    instance.transform.localScale = resolution.WorldScale * scaleFactor;
                }

                _tints[index].SetTint(tint);
            }

            VisibleInstanceCount = visible;
        }

        private void ApplyInteraction(DigItemVisualResolution resolution)
        {
            bool interactive = Model.IsInteractive
                && resolution.ColliderPolicy == DigItemColliderPolicy.InteractiveOnly;
            gameObject.layer = interactive ? 0 : 2;
            _interactionCollider!.enabled = interactive;
        }

        private static Quaternion ResolveRotation(
            DigItemRotationPolicy policy,
            ItemStackQuantityBand band,
            int quarterTurns)
        {
            return policy switch
            {
                DigItemRotationPolicy.Fixed => Quaternion.identity,
                DigItemRotationPolicy.StackQuarterTurns =>
                    Quaternion.Euler(0f, quarterTurns * 90f, 0f),
                DigItemRotationPolicy.LeanByQuantityBand => Quaternion.Euler(
                    0f,
                    quarterTurns * 90f,
                    band == ItemStackQuantityBand.Single ? 0f : 7f),
                _ => Quaternion.identity,
            };
        }

        private Color ResolveReservationTint(ItemReservationVisualState state)
        {
            return state switch
            {
                ItemReservationVisualState.Partial =>
                    Color.Lerp(_baseTint, Color.white, 0.22f),
                ItemReservationVisualState.Full =>
                    Color.Lerp(_baseTint, Color.black, 0.28f),
                _ => _baseTint,
            };
        }

        private static bool TryResolveRawMaterialTint(string itemId, out Color tint)
        {
            string value = itemId?.ToLowerInvariant() ?? string.Empty;
            if (value.Contains("gold"))
            {
                tint = new Color(0.95f, 0.78f, 0.12f, 1f);
                return true;
            }

            if (value.Contains("iron"))
            {
                tint = new Color(0.92f, 0.43f, 0.12f, 1f);
                return true;
            }

            if (value.Contains("coal"))
            {
                tint = new Color(0.055f, 0.055f, 0.065f, 1f);
                return true;
            }

            if (value.Contains("crystal"))
            {
                tint = new Color(0.25f, 0.78f, 0.96f, 1f);
                return true;
            }

            if (value.Contains("stone") || value.Contains("rock"))
            {
                tint = new Color(0.48f, 0.50f, 0.53f, 1f);
                return true;
            }

            tint = Color.white;
            return false;
        }

        private void ClearChildren()
        {
            for (int index = 0; index < _instances.Count; index++)
            {
                if (_instances[index] != null)
                {
                    Destroy(_instances[index]);
                }
            }

            _instances.Clear();
            _tints.Clear();
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
            Transform[] values = root.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int index = 0; index < values.Length; index++)
            {
                values[index].gameObject.layer = layer;
            }
        }
    }
}
