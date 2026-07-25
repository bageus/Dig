using System;
using Dig.Domain.Buildings;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigBuildingVisual : MonoBehaviour
    {
        private Transform? _modelContainer;
        private GameObject? _instance;
        private DigVisualTintTarget? _tint;
        private DigBuildingDetailGroup[] _detailGroups =
            Array.Empty<DigBuildingDetailGroup>();
        private MeshFilter[] _meshFilters = Array.Empty<MeshFilter>();
        private Renderer[] _renderers = Array.Empty<Renderer>();
        private string _assetKey = string.Empty;
        private Color _baseTint = Color.white;
        private TerrainVisualDetailLevel _detailLevel = TerrainVisualDetailLevel.Full;
        private bool _selected;

        public BuildingWorldViewModel Model { get; private set; } = null!;

        internal int RebuildCount { get; private set; }

        internal int VisibleRendererCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < _renderers.Length; index++)
                {
                    if (_renderers[index] != null
                        && _renderers[index].gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        internal int VisibleTriangleCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < _meshFilters.Length; index++)
                {
                    MeshFilter filter = _meshFilters[index];
                    if (filter == null || !filter.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Mesh? mesh = filter.sharedMesh;
                    if (mesh != null && mesh.subMeshCount > 0)
                    {
                        count += checked((int)(mesh.GetIndexCount(0) / 3u));
                    }
                }

                return count;
            }
        }

        internal void Initialize(
            BuildingWorldViewModel model,
            DigBuildingVisualResolution resolution)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            EnsureContainer();
            RebuildInstance(resolution);
            SetSelected(false);
        }

        internal void InvalidateAsset()
        {
            _assetKey = string.Empty;
        }

        internal void SetModel(
            BuildingWorldViewModel model,
            DigBuildingVisualResolution resolution)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            EnsureContainer();
            if (_instance == null || _assetKey != resolution.Asset.StableId)
            {
                RebuildInstance(resolution);
            }
            else
            {
                ApplyPlacement(resolution);
                ApplyPresentation();
            }
        }

        internal void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyPresentation();
        }

        internal void SetDetailLevel(TerrainVisualDetailLevel value)
        {
            if (_detailLevel == value)
            {
                return;
            }

            _detailLevel = value;
            ApplyDetailLevel();
        }

        private void EnsureContainer()
        {
            if (_modelContainer != null)
            {
                return;
            }

            _modelContainer = new GameObject("Model").transform;
            _modelContainer.SetParent(transform, worldPositionStays: false);
        }

        private void RebuildInstance(DigBuildingVisualResolution resolution)
        {
            if (_instance != null)
            {
                _instance.SetActive(false);
                Destroy(_instance);
            }

            _instance = DigVisualPrefabFactory.Create(
                resolution.Asset,
                _modelContainer!,
                $"{Model.Name} {Model.VisualState}",
                PrimitiveType.Cube);
            _assetKey = resolution.Asset.StableId;
            _baseTint = resolution.Asset.Tint;
            _tint = _instance.GetComponent<DigVisualTintTarget>();
            _detailGroups = _instance.GetComponentsInChildren<DigBuildingDetailGroup>(
                includeInactive: true);
            _meshFilters = _instance.GetComponentsInChildren<MeshFilter>(
                includeInactive: true);
            _renderers = _instance.GetComponentsInChildren<Renderer>(
                includeInactive: true);
            RebuildCount++;
            ApplyPlacement(resolution);
            ValidateFootprint(resolution);
            ApplyDetailLevel();
            ApplyPresentation();
        }

        private void ApplyDetailLevel()
        {
            for (int index = 0; index < _detailGroups.Length; index++)
            {
                _detailGroups[index].SetDetailLevel(_detailLevel);
            }
        }

        private void ApplyPlacement(DigBuildingVisualResolution resolution)
        {
            transform.localPosition = DigTunnelProjection.ResidentWorldPosition(
                Model.OriginX,
                Model.OriginY,
                Model.OriginZ) + (Vector3.up * DigTunnelProjection.ResidentFootSink);
            transform.localRotation = ResolveOrientation(Model.Orientation);
            transform.localScale = Vector3.one;

            if (_instance == null)
            {
                return;
            }

            if (resolution.Asset.IsFallback || resolution.Asset.Prefab == null)
            {
                ResolveLocalBounds(out Vector2 center, out Vector2 size);
                float height = ResolveFallbackHeight(Model.VisualState);
                _instance.transform.localPosition = new Vector3(
                    center.x,
                    height * 0.5f,
                    center.y);
                _instance.transform.localRotation = Quaternion.identity;
                _instance.transform.localScale = new Vector3(
                    Mathf.Max(0.50f, size.x * 0.86f),
                    height,
                    Mathf.Max(0.50f, size.y * 0.86f));
                return;
            }

            _instance.transform.localPosition = new Vector3(
                -resolution.PivotCell.x,
                0f,
                -resolution.PivotCell.y);
            _instance.transform.localRotation = Quaternion.identity;
            _instance.transform.localScale = Vector3.one;
        }

        private void ApplyPresentation()
        {
            if (_modelContainer == null || Model == null)
            {
                return;
            }

            float progressScale = 1f;
            if (Model.VisualState == BuildingVisualState.Assembly)
            {
                progressScale = Mathf.Lerp(
                    0.18f,
                    1f,
                    (float)Model.AssemblyProgress);
            }
            else if (Model.VisualState == BuildingVisualState.Packing)
            {
                progressScale = Mathf.Lerp(
                    1f,
                    0.65f,
                    (float)Model.Functions.PackingProgress);
            }

            float selectionScale = _selected ? 1.06f : 1f;
            _modelContainer.localPosition = Vector3.zero;
            _modelContainer.localRotation = Quaternion.identity;
            _modelContainer.localScale = new Vector3(
                selectionScale,
                progressScale * selectionScale,
                selectionScale);
            _tint?.SetTint(_selected
                ? Color.Lerp(_baseTint, Color.white, 0.34f)
                : _baseTint);
            gameObject.name = $"Building {Model.Name} [{Model.VisualState}]";
        }

        private void ValidateFootprint(DigBuildingVisualResolution resolution)
        {
            if (!resolution.HasProfile)
            {
                return;
            }

            ResolveLocalBounds(out _, out Vector2 actualSize);
            Vector2Int rounded = new Vector2Int(
                Mathf.RoundToInt(actualSize.x),
                Mathf.RoundToInt(actualSize.y));
            if (rounded != resolution.ExpectedFootprintSize)
            {
                Debug.LogWarning(
                    $"Building '{Model.DefinitionId}' footprint {rounded} does not "
                    + $"match visual profile {resolution.ExpectedFootprintSize}.",
                    this);
            }
        }

        private void ResolveLocalBounds(out Vector2 center, out Vector2 size)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for (int index = 0; index < Model.Footprint.Count; index++)
            {
                BuildingFootprintCellViewModel cell = Model.Footprint[index];
                ResolveLocalOffset(
                    cell.X - Model.OriginX,
                    cell.Y - Model.OriginY,
                    Model.Orientation,
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

        private static float ResolveFallbackHeight(BuildingVisualState state)
        {
            return state switch
            {
                BuildingVisualState.BuildingBox => 0.18f,
                BuildingVisualState.Assembly => 0.46f,
                BuildingVisualState.Damaged => 0.72f,
                BuildingVisualState.Packing => 0.62f,
                _ => 0.82f,
            };
        }
    }
}
