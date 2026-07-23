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
            string key = resolution.Asset.StableId;
            int capacity = Mathf.Clamp(resolution.MaxVisibleInstances, 1, 4);
            if (_assetKey == key && _poolCapacity == capacity && _instances.Count == capacity)
            {
                return;
            }

            ClearChildren();
            _assetKey = key;
            _poolCapacity = capacity;
            _baseTint = resolution.Asset.Tint;
            for (int index = 0; index < capacity; index++)
            {
                GameObject instance = DigVisualPrefabFactory.Create(
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
            int visible = Math.Min(layout.Instances.Count, _instances.Count);
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