using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigBuildingBoxGhostRenderer
    {
        private const string ItemCatalogResourcePath = "Dig/VisualCatalogs/Items";
        private static readonly Color ValidItemGhostTint =
            new Color(0.25f, 0.82f, 0.56f, 0.72f);
        private static readonly Color InvalidItemGhostTint =
            new Color(0.92f, 0.32f, 0.28f, 0.82f);
        private static readonly Color PlannedItemGhostTint =
            new Color(0.18f, 0.48f, 0.94f, 0.68f);

        [SerializeField]
        private DigItemVisualCatalog? itemVisualCatalog;

        private readonly Dictionary<string, PlannedBoxVisual> _plannedBoxes =
            new Dictionary<string, PlannedBoxVisual>(StringComparer.Ordinal);
        private Transform? _plannedContainer;

        public void SetItemVisualCatalog(DigItemVisualCatalog? catalog)
        {
            itemVisualCatalog = catalog;
            _assetKey = string.Empty;
        }

        internal void RenderPlans(
            IReadOnlyList<BuildingBoxRelocationPlanViewModel> plans)
        {
            if (plans == null)
            {
                throw new ArgumentNullException(nameof(plans));
            }

            EnsureResources();
            EnsurePlannedContainer();
            HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < plans.Count; index++)
            {
                BuildingBoxRelocationPlanViewModel plan = plans[index];
                string jobId = plan.JobId.ToString();
                visible.Add(jobId);
                if (!_plannedBoxes.TryGetValue(jobId, out PlannedBoxVisual? visual))
                {
                    visual = CreatePlannedBox(plan);
                    _plannedBoxes.Add(jobId, visual);
                }

                visual.Root.localRotation = Quaternion.identity;
                visual.Root.localScale = Vector3.one;
                visual.Root.gameObject.SetActive(true);
                DigWorldItemGrounding.PlaceOnFloor(
                    visual.Root,
                    DigWorldItemVisualPolicy.ResolveFloorAnchor(
                        plan.Destination,
                        Vector2.zero));
                visual.Tint.SetTint(PlannedItemGhostTint);
            }

            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, PlannedBoxVisual> pair in _plannedBoxes)
            {
                if (!visible.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            for (int index = 0; index < removed.Count; index++)
            {
                string key = removed[index];
                PlannedBoxVisual visual = _plannedBoxes[key];
                _plannedBoxes.Remove(key);
                Destroy(visual.Root.gameObject);
            }
        }

        private void InitializeItemVisuals()
        {
            if (itemVisualCatalog == null)
            {
                itemVisualCatalog = Resources.Load<DigItemVisualCatalog>(
                    ItemCatalogResourcePath);
            }
        }

        private void RenderBuildingBoxItemPreview(BuildingBoxGhostViewModel preview)
        {
            string itemId = preview.SourceItemId?.ToString()
                ?? throw new InvalidOperationException(
                    "BuildingBox relocation preview requires its source item id.");
            DigItemVisualResolution resolution = DigWorldItemVisualPolicy.Resolve(
                itemVisualCatalog,
                itemId);
            EnsureItemPreviewInstance(preview, resolution);
            _previewContainer!.localRotation = preview.IsValid
                ? Quaternion.identity
                : Quaternion.Euler(0f, 0f, 7f);
            _previewContainer.localScale = preview.IsValid
                ? Vector3.one
                : new Vector3(0.92f, 1.18f, 0.92f);
            _previewContainer.gameObject.SetActive(true);
            DigWorldItemGrounding.PlaceOnFloor(
                _previewContainer,
                DigWorldItemVisualPolicy.ResolveFloorAnchor(
                    preview.Origin,
                    Vector2.zero));
            _previewTint!.SetTint(preview.IsValid
                ? ValidItemGhostTint
                : InvalidItemGhostTint);
            RenderWorkMarker(preview);
        }

        private void EnsureItemPreviewInstance(
            BuildingBoxGhostViewModel preview,
            DigItemVisualResolution resolution)
        {
            string key = $"item:{resolution.Asset.StableId}";
            if (_previewInstance != null && _assetKey == key)
            {
                _previewContainer!.gameObject.SetActive(true);
                return;
            }

            if (_previewInstance != null)
            {
                _previewInstance.SetActive(false);
                Destroy(_previewInstance);
            }

            _previewInstance = DigVisualPrefabFactory.Create(
                resolution.Asset,
                _previewContainer!,
                $"BuildingBox ghost {preview.SourceItemId}",
                PrimitiveType.Cube);
            _assetKey = key;
            _previewInstance.transform.localPosition = Vector3.zero;
            _previewInstance.transform.localRotation = Quaternion.identity;
            _previewInstance.transform.localScale = resolution.WorldScale;
            SetLayerRecursively(_previewInstance, layer: 2);
            DisableColliders(_previewInstance);
            DigTransparentVisualSurface transparent =
                _previewInstance.GetComponent<DigTransparentVisualSurface>()
                ?? _previewInstance.AddComponent<DigTransparentVisualSurface>();
            transparent.Configure(fixedOpacity: 0.62f);
            _previewTint = _previewInstance.GetComponent<DigVisualTintTarget>();
            _previewContainer!.gameObject.SetActive(true);
        }

        private PlannedBoxVisual CreatePlannedBox(
            BuildingBoxRelocationPlanViewModel plan)
        {
            DigItemVisualResolution resolution = DigWorldItemVisualPolicy.Resolve(
                itemVisualCatalog,
                plan.ItemId.ToString());
            Transform root = new GameObject(
                $"Planned BuildingBox {plan.JobId}").transform;
            root.gameObject.layer = 2;
            root.SetParent(_plannedContainer, worldPositionStays: false);
            GameObject instance = DigVisualPrefabFactory.Create(
                resolution.Asset,
                root,
                $"Planned BuildingBox {plan.ItemId}",
                PrimitiveType.Cube);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = resolution.WorldScale;
            SetLayerRecursively(instance, layer: 2);
            DisableColliders(instance);
            DigTransparentVisualSurface transparent =
                instance.GetComponent<DigTransparentVisualSurface>()
                ?? instance.AddComponent<DigTransparentVisualSurface>();
            transparent.Configure(fixedOpacity: 0.52f);
            DigVisualTintTarget tint = instance.GetComponent<DigVisualTintTarget>()
                ?? instance.AddComponent<DigVisualTintTarget>();
            return new PlannedBoxVisual(root, tint);
        }

        private void EnsurePlannedContainer()
        {
            if (_plannedContainer != null)
            {
                return;
            }

            _plannedContainer = new GameObject("Planned BuildingBox Relocations").transform;
            _plannedContainer.gameObject.layer = 2;
            _plannedContainer.SetParent(_root, worldPositionStays: false);
        }

        private void DisposeItemVisuals()
        {
            _plannedBoxes.Clear();
        }

        private sealed class PlannedBoxVisual
        {
            internal PlannedBoxVisual(
                Transform root,
                DigVisualTintTarget tint)
            {
                Root = root;
                Tint = tint;
            }

            internal Transform Root { get; }
            internal DigVisualTintTarget Tint { get; }
        }
    }
}
