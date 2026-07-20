using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigBuildingRenderer : MonoBehaviour
    {
        private const string CatalogResourcePath =
            "Dig/VisualCatalogs/Buildings";

        [SerializeField]
        private DigBuildingVisualCatalog? visualCatalog;

        private readonly Dictionary<string, DigBuildingVisual> _buildings =
            new Dictionary<string, DigBuildingVisual>(StringComparer.Ordinal);
        private Transform? _root;
        private DigBuildingVisual? _selected;
        private DigRepresentativeBuildingPrefabLibrary? _representatives;
        private TerrainVisualDetailLevel _buildingDetailLevel =
            TerrainVisualDetailLevel.Full;

        public string? SelectedBuildingId => _selected?.Model.Id;

        public BuildingWorldViewModel? SelectedModel => _selected?.Model;

        internal int InstanceCount => _buildings.Count;

        private void Awake()
        {
            _representatives = DigRepresentativeBuildingPrefabLibrary.Acquire();
            if (visualCatalog == null)
            {
                visualCatalog = Resources.Load<DigBuildingVisualCatalog>(
                    CatalogResourcePath);
            }

            DigVisualCatalogDiagnostics.LogValidation(
                visualCatalog,
                this,
                "Buildings");
            LogRepresentativeValidation();
        }

        public void SetVisualCatalog(DigBuildingVisualCatalog? catalog)
        {
            visualCatalog = catalog;
            DigVisualCatalogDiagnostics.LogValidation(
                visualCatalog,
                this,
                "Buildings");
            foreach (DigBuildingVisual visual in _buildings.Values)
            {
                visual.InvalidateAsset();
                visual.SetModel(visual.Model, Resolve(visual.Model));
                visual.SetDetailLevel(_buildingDetailLevel);
            }
        }

        public void Render(IReadOnlyList<BuildingWorldViewModel> buildings)
        {
            if (buildings == null)
            {
                throw new ArgumentNullException(nameof(buildings));
            }

            EnsureRoot();
            HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < buildings.Count; index++)
            {
                BuildingWorldViewModel model = buildings[index];
                visible.Add(model.Id);
                DigBuildingVisualResolution resolution = Resolve(model);
                if (_buildings.TryGetValue(model.Id, out DigBuildingVisual? visual))
                {
                    visual.SetModel(model, resolution);
                }
                else
                {
                    CreateBuilding(model, resolution);
                }
            }

            RemoveMissing(visible);
        }

        public bool TryGetBuilding(RaycastHit hit, out DigBuildingVisual building)
        {
            building = hit.collider == null
                ? null!
                : hit.collider.GetComponentInParent<DigBuildingVisual>();
            return building != null;
        }

        public DigBuildingVisual? Select(DigBuildingVisual? building)
        {
            if (_selected != null)
            {
                _selected.SetSelected(false);
            }

            _selected = building;
            if (_selected != null)
            {
                _selected.SetSelected(true);
            }

            return _selected;
        }

        public DigBuildingVisual? SelectById(string id)
        {
            return _buildings.TryGetValue(id, out DigBuildingVisual? building)
                ? Select(building)
                : Select(null);
        }

        private DigBuildingVisualResolution Resolve(BuildingWorldViewModel model)
        {
            DigBuildingVisualResolution catalogResolution = default;
            bool hasCatalogResolution = visualCatalog != null;
            if (visualCatalog != null)
            {
                catalogResolution = visualCatalog.ResolveBuilding(
                    model.DefinitionId,
                    model.VisualState);
                if (catalogResolution.HasProfile)
                {
                    return catalogResolution;
                }
            }

            if (_representatives != null
                && _representatives.TryResolve(
                    model.DefinitionId,
                    model.VisualState,
                    out DigBuildingVisualResolution representative))
            {
                return representative;
            }

            if (hasCatalogResolution)
            {
                return catalogResolution;
            }

            DigVisualAsset fallback = DigVisualAsset.CreateRuntimeFallback(
                model.DefinitionId,
                ResolveFallbackTint(model.VisualState));
            return new DigBuildingVisualResolution(
                fallback,
                Vector2Int.one,
                Vector2.zero,
                hasProfile: false);
        }

        private void CreateBuilding(
            BuildingWorldViewModel model,
            DigBuildingVisualResolution resolution)
        {
            GameObject root = new GameObject($"Building {model.Name}");
            root.transform.SetParent(_root, worldPositionStays: false);
            DigBuildingVisual visual = root.AddComponent<DigBuildingVisual>();
            visual.Initialize(model, resolution);
            visual.SetDetailLevel(_buildingDetailLevel);
            _buildings.Add(model.Id, visual);
        }

        private void RemoveMissing(HashSet<string> visible)
        {
            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, DigBuildingVisual> pair in _buildings)
            {
                if (!visible.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            for (int index = 0; index < removed.Count; index++)
            {
                string id = removed[index];
                DigBuildingVisual visual = _buildings[id];
                if (_selected == visual)
                {
                    _selected = null;
                }

                _buildings.Remove(id);
                Destroy(visual.gameObject);
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("Building Visuals").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }

        private void LogRepresentativeValidation()
        {
            if (_representatives == null)
            {
                return;
            }

            IReadOnlyList<string> errors = _representatives.ValidationErrors;
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError($"Buildings representative pack: {errors[index]}", this);
            }
        }

        private static Color ResolveFallbackTint(BuildingVisualState state)
        {
            return state switch
            {
                BuildingVisualState.BuildingBox => new Color(0.52f, 0.38f, 0.22f, 1f),
                BuildingVisualState.Assembly => new Color(0.76f, 0.57f, 0.28f, 1f),
                BuildingVisualState.Damaged => new Color(0.56f, 0.24f, 0.18f, 1f),
                BuildingVisualState.Packing => new Color(0.42f, 0.52f, 0.66f, 1f),
                _ => new Color(0.64f, 0.48f, 0.28f, 1f),
            };
        }
    }
}
