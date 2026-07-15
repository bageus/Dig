using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigBuildingVisual : MonoBehaviour
    {
        private readonly List<Transform> _parts = new List<Transform>();
        private readonly List<Vector2Int> _cells = new List<Vector2Int>();
        private Material? _normalMaterial;
        private Material? _selectedMaterial;
        private bool _selected;

        public BuildingWorldViewModel Model { get; private set; } = null!;

        internal void Initialize(
            BuildingWorldViewModel model,
            Material normalMaterial,
            Material selectedMaterial)
        {
            _normalMaterial = normalMaterial
                ?? throw new ArgumentNullException(nameof(normalMaterial));
            _selectedMaterial = selectedMaterial
                ?? throw new ArgumentNullException(nameof(selectedMaterial));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            RebuildGeometry();
            SetSelected(false);
        }

        internal void SetModel(BuildingWorldViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            bool rebuild = !GeometryMatches(model);
            Model = model;
            if (rebuild)
            {
                RebuildGeometry();
            }

            ApplySelection();
        }

        internal void SetSelected(bool selected)
        {
            _selected = selected;
            ApplySelection();
        }

        private bool GeometryMatches(BuildingWorldViewModel model)
        {
            if (_cells.Count != model.Footprint.Count)
            {
                return false;
            }

            for (int index = 0; index < _cells.Count; index++)
            {
                BuildingFootprintCellViewModel cell = model.Footprint[index];
                if (_cells[index].x != cell.X || _cells[index].y != cell.Y)
                {
                    return false;
                }
            }

            return true;
        }

        private void RebuildGeometry()
        {
            foreach (Transform part in _parts)
            {
                if (part != null)
                {
                    Destroy(part.gameObject);
                }
            }

            _parts.Clear();
            _cells.Clear();
            for (int index = 0; index < Model.Footprint.Count; index++)
            {
                BuildingFootprintCellViewModel cell = Model.Footprint[index];
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = $"{Model.Name} footprint {cell.X},{cell.Y}";
                part.transform.SetParent(transform, worldPositionStays: false);
                _parts.Add(part.transform);
                _cells.Add(new Vector2Int(cell.X, cell.Y));
            }

            ApplySelection();
        }

        private void ApplySelection()
        {
            if (_normalMaterial == null || _selectedMaterial == null)
            {
                return;
            }

            float height = _selected ? 0.62f : 0.42f;
            float width = _selected ? 0.92f : 0.82f;
            for (int index = 0; index < _parts.Count; index++)
            {
                Transform part = _parts[index];
                Vector2Int cell = _cells[index];
                part.localPosition = new Vector3(cell.x, height * 0.5f, cell.y);
                part.localScale = new Vector3(width, height, width);
                part.GetComponent<Renderer>().sharedMaterial = _selected
                    ? _selectedMaterial
                    : _normalMaterial;
            }

            gameObject.name = $"Building {Model.Name} [{Model.Status}]";
        }
    }
}
