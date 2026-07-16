using System;
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
        private int _currentX;
        private int _currentY;
        private float _elapsed;
        private float _duration;

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
            _currentX = model.CellX;
            _currentY = model.CellY;
            transform.position = ToWorld(model.CellX, model.CellY);
            SetSelected(false);
        }

        internal void SetModel(AgentViewModel model, float duration)
        {
            Model = model;
            if (_currentX == model.CellX && _currentY == model.CellY)
            {
                return;
            }

            _previousX = _currentX;
            _previousY = _currentY;
            _currentX = model.CellX;
            _currentY = model.CellY;
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, duration);
            Vector3 direction = ToWorld(_currentX, _currentY) - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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
            if (_duration <= 0f)
            {
                return;
            }

            _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
            double progress = _elapsed / _duration;
            AgentInterpolatedPosition position = AgentPositionInterpolator.Interpolate(
                _previousX,
                _previousY,
                _currentX,
                _currentY,
                progress);
            transform.position = ToWorld((float)position.X, (float)position.Y);
            if (_elapsed >= _duration)
            {
                _duration = 0f;
            }
        }

        private static Vector3 ToWorld(float cellX, float cellY)
        {
            return new Vector3(cellX, 0.68f, cellY);
        }
    }
}