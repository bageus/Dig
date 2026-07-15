using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigBuildingRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigBuildingVisual> _buildings =
            new Dictionary<string, DigBuildingVisual>(StringComparer.Ordinal);
        private Transform? _root;
        private Material? _normalMaterial;
        private Material? _selectedMaterial;
        private DigBuildingVisual? _selected;

        public string? SelectedBuildingId => _selected?.Model.Id;

        public BuildingWorldViewModel? SelectedModel => _selected?.Model;

        public void Render(IReadOnlyList<BuildingWorldViewModel> buildings)
        {
            if (buildings == null)
            {
                throw new ArgumentNullException(nameof(buildings));
            }

            EnsureResources();
            HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < buildings.Count; index++)
            {
                BuildingWorldViewModel model = buildings[index];
                visible.Add(model.Id);
                if (_buildings.TryGetValue(model.Id, out DigBuildingVisual? visual))
                {
                    visual.SetModel(model);
                }
                else
                {
                    CreateBuilding(model);
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

        private void CreateBuilding(BuildingWorldViewModel model)
        {
            GameObject root = new GameObject($"Building {model.Name}");
            root.transform.SetParent(_root, worldPositionStays: false);
            DigBuildingVisual visual = root.AddComponent<DigBuildingVisual>();
            visual.Initialize(model, _normalMaterial!, _selectedMaterial!);
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

            foreach (string id in removed)
            {
                DigBuildingVisual visual = _buildings[id];
                if (_selected == visual)
                {
                    _selected = null;
                }

                _buildings.Remove(id);
                Destroy(visual.gameObject);
            }
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Building Visuals").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            if (_normalMaterial != null)
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
                throw new InvalidOperationException("No supported building shader was found.");
            }

            _normalMaterial = new Material(shader)
            {
                name = "Dig Building",
                color = new Color(0.64f, 0.48f, 0.28f, 1f),
            };
            _selectedMaterial = new Material(shader)
            {
                name = "Dig Building Selected",
                color = new Color(1f, 0.76f, 0.24f, 1f),
            };
        }

        private void OnDestroy()
        {
            if (_normalMaterial != null)
            {
                Destroy(_normalMaterial);
            }

            if (_selectedMaterial != null)
            {
                Destroy(_selectedMaterial);
            }
        }
    }
}
