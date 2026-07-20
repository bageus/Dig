using System;
using System.Collections.Generic;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed partial class DigAgentVisual : MonoBehaviour
{
    private readonly ResidentVisualPresenter _visualPresenter = new ResidentVisualPresenter();
    private Material? _normalMaterial;
    private Material? _selectedMaterial;
    private DigResidentRig? _rig;
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

    internal void Initialize(AgentViewModel model, Material normalMaterial,
        Material selectedMaterial, DigResidentRig rig,
        ResidentAppearanceViewModel appearance)
    {
        Model = model;
        _normalMaterial = normalMaterial;
        _selectedMaterial = selectedMaterial;
        _rig = rig;
        _previousX = model.CellX;
        _previousY = model.CellY;
        _previousZ = model.CellZ;
        _currentX = model.CellX;
        _currentY = model.CellY;
        _currentZ = model.CellZ;
        transform.SetPositionAndRotation(ToWorld(model.CellX, model.CellY, model.CellZ),
            Quaternion.identity);
        _rig.ApplyAppearance(appearance);
        ApplyAction(isMoving: false);
        SetSelected(false);
    }

    internal void SetModel(AgentViewModel model, float duration)
    {
        Model = model;
        bool moving = _currentX != model.CellX || _currentY != model.CellY
            || _currentZ != model.CellZ;
        ApplyAction(moving);
        if (!moving) return;
        _previousX = _currentX;
        _previousY = _currentY;
        _previousZ = _currentZ;
        _currentX = model.CellX;
        _currentY = model.CellY;
        _currentZ = model.CellZ;
        _elapsed = 0f;
        _duration = Mathf.Max(0.01f, duration);
        Face(ToWorld(_currentX, _currentY, _currentZ) - transform.position);
    }

    internal void SetEquipment(ResidentEquipmentViewModel? equipment,
        Material equipmentMaterial)
    {
        if (equipment == null)
        {
            _equipmentVisual?.Clear();
            return;
        }
        if (!string.Equals(equipment.ResidentId, Model.Id, StringComparison.Ordinal))
            throw new ArgumentException("Equipment does not belong to this resident.",
                nameof(equipment));
        if (_equipmentVisual == null)
        {
            GameObject root = new GameObject("Equipment");
            root.transform.SetParent(ResolveSocket(DigResidentSocketKind.RightHand), false);
            _equipmentVisual = root.AddComponent<DigAgentEquipmentVisual>();
        }
        _equipmentVisual.Configure(equipment.ItemId, EquipmentAppearanceKind.Generic,
            equipmentMaterial);
    }

    internal Transform ResolveSocket(DigResidentSocketKind kind)
    {
        return _rig == null ? transform : _rig.ResolveSocket(kind);
    }

    internal void SetSelected(bool selected)
    {
        _rig?.SetSelected(selected);
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = selected ? _selectedMaterial : _normalMaterial;
    }

    private void ApplyAction(bool isMoving)
    {
        _rig?.ApplyAction(_visualPresenter.PresentAction(Model, isMoving,
            isCarrying: false));
    }

    private static Vector3 ToWorld(SpatialCellId cell)
    {
        return ToWorld(cell.X, cell.Y, cell.Z);
    }

    private static Vector3 ToWorld(float cellX, float cellY, float cellZ)
    {
        return DigTunnelProjection.ResidentWorldPosition(cellX, cellY, cellZ);
    }
}
}
