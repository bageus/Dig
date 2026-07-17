using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentVisual : MonoBehaviour
    {
        private Material? _normalMaterial;
        private Material? _selectedMaterial;
        private DigAgentEquipmentVisual? _equipmentVisual;
        private int _previousX;
        private int _previousY;
        private int _previousZ;
        private int _currentX;
        private int _currentY;
        private int _currentZ;
        private float _elapsed;
        private float _duration;
        private IReadOnlyList<SpatialCellId>? _route;
        private int _routeIndex;
        private float _routeElapsed;
        private float _routeStepDuration;

        public AgentViewModel Model { get; private set; } = null!;

        internal void Initialize(
            AgentViewModel model,
            Material normalMaterial,
            Material selectedMaterial)
        {
            Model = model;
            _normalMaterial = normalMaterial;
            _selectedMaterial = selectedMaterial;
            _previousX = model.CellX;
            _previousY = model.CellY;
            _previousZ = model.CellZ;
            _currentX = model.CellX;
            _currentY = model.CellY;
            _currentZ = model.CellZ;
            transform.localPosition = ToLocal(model.CellX, model.CellY, model.CellZ);
            SetSelected(false);
        }

        internal void SetModel(AgentViewModel model, float duration)
        {
            Model = model;
            if (_currentX == model.CellX
                && _currentY == model.CellY
                && _currentZ == model.CellZ)
            {
                return;
            }

            _previousX = _currentX;
            _previousY = _currentY;
            _previousZ = _currentZ;
            _currentX = model.CellX;
            _currentY = model.CellY;
            _currentZ = model.CellZ;
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, duration);
            Face(ToLocal(_currentX, _currentY, _currentZ) - transform.localPosition);
        }

        internal void PlayRoute(
            IReadOnlyList<SpatialCellId> cells,
            float stepDuration)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count == 0)
            {
                return;
            }

            _route = cells;
            _routeIndex = 0;
            _routeElapsed = 0f;
            _routeStepDuration = Mathf.Max(0.03f, stepDuration);
            _duration = 0f;
            transform.localPosition = ToLocal(cells[0]);
            if (cells.Count > 1)
            {
                Face(ToLocal(cells[1]) - transform.localPosition);
            }
        }

        internal void SetEquipment(
            ResidentEquipmentViewModel? equipment,
            Material equipmentMaterial)
        {
            if (equipment == null)
            {
                _equipmentVisual?.Clear();
                return;
            }

            if (!string.Equals(equipment.ResidentId, Model.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Equipment does not belong to this resident.",
                    nameof(equipment));
            }

            if (_equipmentVisual == null)
            {
                GameObject root = new GameObject("Equipment");
                root.transform.SetParent(transform, worldPositionStays: false);
                _equipmentVisual = root.AddComponent<DigAgentEquipmentVisual>();
            }

            _equipmentVisual.Configure(equipment.ItemId, equipmentMaterial);
        }

        internal void SetSelected(bool selected)
        {
            Renderer renderer = GetComponent<Renderer>();
            renderer.sharedMaterial = selected ? _selectedMaterial : _normalMaterial;
        }

        private void Update()
        {
            if (_route != null)
            {
                UpdateRoute();
                return;
            }

            if (_duration <= 0f)
            {
                return;
            }

            _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
            double progress = _elapsed / _duration;
            AgentInterpolatedSpatialPosition position =
                AgentSpatialPositionInterpolator.Interpolate(
                    _previousX,
                    _previousY,
                    _previousZ,
                    _currentX,
                    _currentY,
                    _currentZ,
                    progress);
            transform.localPosition = ToLocal(
                (float)position.X,
                (float)position.Y,
                (float)position.Z);
            if (_elapsed >= _duration)
            {
                _duration = 0f;
            }
        }

        private void UpdateRoute()
        {
            if (_route == null || _routeIndex + 1 >= _route.Count)
            {
                _route = null;
                return;
            }

            SpatialCellId from = _route[_routeIndex];
            SpatialCellId to = _route[_routeIndex + 1];
            _routeElapsed = Mathf.Min(
                _routeStepDuration,
                _routeElapsed + Time.deltaTime);
            float progress = _routeElapsed / _routeStepDuration;
            transform.localPosition = Vector3.Lerp(ToLocal(from), ToLocal(to), progress);
            if (_routeElapsed < _routeStepDuration)
            {
                return;
            }

            _routeIndex++;
            _routeElapsed = 0f;
            if (_routeIndex + 1 < _route.Count)
            {
                Face(ToLocal(_route[_routeIndex + 1]) - transform.localPosition);
            }
            else
            {
                transform.localPosition = ToLocal(_route[_route.Count - 1]);
                _route = null;
            }
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.localRotation = Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
            }
        }

        private static Vector3 ToLocal(SpatialCellId cell)
        {
            return ToLocal(cell.X, cell.Y, cell.Z);
        }

        private static Vector3 ToLocal(float cellX, float cellY, float cellZ)
        {
            return DigTunnelProjection.ResidentLocalPosition(cellX, cellY, cellZ);
        }
    }
}
