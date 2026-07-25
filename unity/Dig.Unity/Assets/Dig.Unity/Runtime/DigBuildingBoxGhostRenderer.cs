using System;
using Dig.Domain.Buildings;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigBuildingBoxGhostRenderer : MonoBehaviour
    {
        private const string CatalogResourcePath =
            "Dig/VisualCatalogs/Buildings";

        [SerializeField]
        private DigBuildingVisualCatalog? visualCatalog;

        private Transform? _root;
        private Transform? _previewContainer;
        private GameObject? _previewInstance;
        private DigVisualTintTarget? _previewTint;
        private string _assetKey = string.Empty;
        private Material? _markerMaterial;
        private GameObject? _workMarker;

        private void Awake()
        {
            InitializeRepresentatives();
            if (visualCatalog == null)
            {
                visualCatalog = Resources.Load<DigBuildingVisualCatalog>(
                    CatalogResourcePath);
            }
        }

        public void SetVisualCatalog(DigBuildingVisualCatalog? catalog)
        {
            visualCatalog = catalog;
            _assetKey = string.Empty;
        }

        private DigBuildingVisualResolution ResolveCatalogBuildingBox(string stableId)
        {
            if (visualCatalog == null)
            {
                throw new InvalidOperationException("Building visual catalog is unavailable.");
            }

            return visualCatalog.ResolveBuilding(
                stableId,
                BuildingVisualState.BuildingBox);
        }

        public void Render(BuildingBoxGhostViewModel preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            EnsureResources();
            DigBuildingVisualResolution resolution = Resolve(preview);
            EnsurePreviewInstance(preview, resolution);
            ApplyPreviewTransform(preview, resolution);
            RenderWorkMarker(preview);
        }

        public void Clear()
        {
            if (_previewContainer != null)
            {
                _previewContainer.gameObject.SetActive(false);
            }

            if (_workMarker != null)
            {
                _workMarker.SetActive(false);
            }
        }

        private void EnsurePreviewInstance(
            BuildingBoxGhostViewModel preview,
            DigBuildingVisualResolution resolution)
        {
            if (_previewInstance != null
                && _assetKey == resolution.Asset.StableId)
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
                $"Building ghost {preview.DefinitionId}",
                PrimitiveType.Cube);
            _assetKey = resolution.Asset.StableId;
            _previewTint = _previewInstance.GetComponent<DigVisualTintTarget>();
            DigBuildingDetailGroup[] groups =
                _previewInstance.GetComponentsInChildren<DigBuildingDetailGroup>(
                    includeInactive: true);
            for (int index = 0; index < groups.Length; index++)
            {
                groups[index].SetDetailLevel(TerrainVisualDetailLevel.Reduced);
            }

            SetLayerRecursively(_previewInstance, layer: 2);
            DisableColliders(_previewInstance);
            _previewContainer!.gameObject.SetActive(true);
        }

        private void ApplyPreviewTransform(
            BuildingBoxGhostViewModel preview,
            DigBuildingVisualResolution resolution)
        {
            _previewContainer!.localPosition =
                DigTunnelProjection.CellWorldPosition(preview.Origin)
                + (Vector3.up * 0.03f);
            _previewContainer.localRotation = ResolveOrientation(preview.Orientation)
                * (preview.IsValid
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 0f, 7f));
            _previewContainer.localScale = preview.IsValid
                ? Vector3.one
                : new Vector3(0.92f, 1.18f, 0.92f);

            if (resolution.Asset.IsFallback || resolution.Asset.Prefab == null)
            {
                ResolveLocalBounds(preview, out Vector2 center, out Vector2 size);
                _previewInstance!.transform.localPosition = new Vector3(
                    center.x,
                    0.12f,
                    center.y);
                _previewInstance.transform.localRotation = Quaternion.identity;
                _previewInstance.transform.localScale = new Vector3(
                    Mathf.Max(0.50f, size.x * 0.86f),
                    0.24f,
                    Mathf.Max(0.50f, size.y * 0.86f));
            }
            else
            {
                _previewInstance!.transform.localPosition = new Vector3(
                    -resolution.PivotCell.x,
                    0f,
                    -resolution.PivotCell.y);
                _previewInstance.transform.localRotation = Quaternion.identity;
                _previewInstance.transform.localScale = Vector3.one;
            }

            _previewTint?.SetTint(preview.IsValid
                ? new Color(0.25f, 0.82f, 0.56f, 0.72f)
                : new Color(0.92f, 0.32f, 0.28f, 0.82f));
        }

        private void RenderWorkMarker(BuildingBoxGhostViewModel preview)
        {
            if (!preview.WorkPosition.HasValue)
            {
                _workMarker!.SetActive(false);
                return;
            }

            CellId cell = preview.WorkPosition.Value;
            _workMarker!.SetActive(true);
            _workMarker.name = $"Building work position {cell}";
            _workMarker.transform.localPosition =
                DigTunnelProjection.CellWorldPosition(cell)
                + (Vector3.up * 0.24f);
            _workMarker.transform.localRotation = preview.IsValid
                ? Quaternion.identity
                : Quaternion.Euler(0f, 45f, 0f);
            _workMarker.transform.localScale = new Vector3(0.24f, 0.48f, 0.24f);
            Renderer renderer = _workMarker.GetComponent<Renderer>();
            renderer.sharedMaterial = _markerMaterial;
            renderer.GetComponent<DigVisualTintTarget>()?.SetTint(preview.IsValid
                ? new Color(0.25f, 0.82f, 0.56f, 0.82f)
                : new Color(0.92f, 0.32f, 0.28f, 0.92f));
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Building Placement Ghost").transform;
                // Preview coordinates come from DigTunnelProjection and are already
                // world-space. Keep the ghost root unrotated so the preview stays
                // under the pointer instead of being rotated outside the play area.
                _root.SetParent(transform, worldPositionStays: true);
                _previewContainer = new GameObject("Preview").transform;
                _previewContainer.SetParent(_root, worldPositionStays: false);
            }

            if (_workMarker != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No supported ghost shader was found.");
            }

            _markerMaterial = new Material(shader)
            {
                name = "Dig Building Ghost Marker",
            };
            _workMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _workMarker.layer = 2;
            _workMarker.transform.SetParent(_root, worldPositionStays: false);
            Collider? collider = _workMarker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            if (_workMarker.GetComponent<DigVisualPrefabRoot>() == null)
            {
                _workMarker.AddComponent<DigVisualPrefabRoot>();
            }

            _workMarker.AddComponent<DigVisualTintTarget>();
            _workMarker.SetActive(false);
        }

        private static void ResolveLocalBounds(
            BuildingBoxGhostViewModel preview,
            out Vector2 center,
            out Vector2 size)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for (int index = 0; index < preview.Footprint.Count; index++)
            {
                CellId cell = preview.Footprint[index];
                ResolveLocalOffset(
                    cell.X - preview.Origin.X,
                    cell.Y - preview.Origin.Y,
                    preview.Orientation,
                    out int x,
                    out int y);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            size = new Vector2((maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static void ResolveLocalOffset(
            int x,
            int y,
            BuildingOrientation orientation,
            out int localX,
            out int localY)
        {
            switch (orientation)
            {
                case BuildingOrientation.East:
                    localX = y;
                    localY = -x;
                    break;
                case BuildingOrientation.South:
                    localX = -x;
                    localY = -y;
                    break;
                case BuildingOrientation.West:
                    localX = -y;
                    localY = x;
                    break;
                default:
                    localX = x;
                    localY = y;
                    break;
            }
        }

        private static Quaternion ResolveOrientation(BuildingOrientation orientation)
        {
            float yaw = orientation switch
            {
                BuildingOrientation.North => 0f,
                BuildingOrientation.East => -90f,
                BuildingOrientation.South => 180f,
                BuildingOrientation.West => 90f,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
            };
            return Quaternion.Euler(0f, yaw, 0f);
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

        private void OnDestroy()
        {
            DisposeRepresentatives();
            if (_markerMaterial != null)
            {
                Destroy(_markerMaterial);
            }
        }
    }
}
