using System;
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
    private const float HoverBlend = 0.42f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private readonly ResidentVisualPresenter _visualPresenter = new ResidentVisualPresenter();
    private readonly MaterialPropertyBlock _hoverProperties = new MaterialPropertyBlock();
    private Material? _normalMaterial;
    private Material? _selectedMaterial;
    private DigResidentRig? _rig;
    private DigAgentEquipmentVisual? _equipmentVisual;
    private Renderer[] _hoverRenderers = Array.Empty<Renderer>();
    private Color[] _hoverBaseColors = Array.Empty<Color>();
    private bool _selected;
    private bool _hovered;
    private bool _hoverApplied;
    private int _previousX;
    private int _previousY;
    private int _previousZ;
    private int _currentX;
    private int _currentY;
    private int _currentZ;
    private float _elapsed;
    private float _duration;

    public AgentViewModel Model { get; private set; } = null!;

    internal void Initialize(AgentViewModel model, Material normalMaterial,
        Material selectedMaterial, DigResidentRig rig,
        ResidentAppearanceViewModel appearance)
    {
        InitializeCommon(model, normalMaterial, selectedMaterial);
        _rig = rig ?? throw new ArgumentNullException(nameof(rig));
        _rig.ApplyAppearance(appearance);
        ApplyAction(isMoving: false);
        SetSelected(false);
        SetHovered(false);
    }

    internal void InitializeSimple(AgentViewModel model, Material normalMaterial,
        Material selectedMaterial)
    {
        InitializeCommon(model, normalMaterial, selectedMaterial);
        _rig = null;
        SetSelected(false);
        SetHovered(false);
    }

    private void InitializeCommon(AgentViewModel model, Material normalMaterial,
        Material selectedMaterial)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _normalMaterial = normalMaterial
            ?? throw new ArgumentNullException(nameof(normalMaterial));
        _selectedMaterial = selectedMaterial
            ?? throw new ArgumentNullException(nameof(selectedMaterial));
        _previousX = model.CellX;
        _previousY = model.CellY;
        _previousZ = model.CellZ;
        _currentX = model.CellX;
        _currentY = model.CellY;
        _currentZ = model.CellZ;
        transform.SetPositionAndRotation(ToWorld(model.CellX, model.CellY, model.CellZ),
            Quaternion.identity);
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
        _selected = selected;
        _rig?.SetSelected(selected);
        _hoverApplied = false;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = selected ? _selectedMaterial : _normalMaterial;
        if (_hovered && !_selected) ApplyHover();
    }

    internal void SetHovered(bool hovered)
    {
        if (_hovered == hovered) return;
        if (_hoverApplied) RestoreHover();
        _hovered = hovered;
        if (_hovered && !_selected) ApplyHover();
    }

    private void ApplyHover()
    {
        if (_hoverRenderers.Length == 0)
        {
            _hoverRenderers = _rig == null
                ? GetComponentsInChildren<Renderer>(includeInactive: true)
                : _rig.GetComponentsInChildren<Renderer>(includeInactive: true);
            _hoverBaseColors = new Color[_hoverRenderers.Length];
        }
        for (int index = 0; index < _hoverRenderers.Length; index++)
        {
            Renderer renderer = _hoverRenderers[index];
            _hoverProperties.Clear();
            renderer.GetPropertyBlock(_hoverProperties);
            Color color = _hoverProperties.GetColor(BaseColorId);
            if (color == default)
            {
                color = _hoverProperties.GetColor(ColorId);
                if (color == default && renderer.sharedMaterial != null)
                    color = renderer.sharedMaterial.color;
            }
            _hoverBaseColors[index] = color;
            Color highlighted = Color.Lerp(color, Color.white, HoverBlend);
            highlighted.a = color.a;
            _hoverProperties.SetColor(BaseColorId, highlighted);
            _hoverProperties.SetColor(ColorId, highlighted);
            renderer.SetPropertyBlock(_hoverProperties);
        }
        _hoverApplied = _hoverRenderers.Length > 0;
    }

    private void RestoreHover()
    {
        for (int index = 0; index < _hoverRenderers.Length; index++)
        {
            Renderer renderer = _hoverRenderers[index];
            _hoverProperties.Clear();
            renderer.GetPropertyBlock(_hoverProperties);
            _hoverProperties.SetColor(BaseColorId, _hoverBaseColors[index]);
            _hoverProperties.SetColor(ColorId, _hoverBaseColors[index]);
            renderer.SetPropertyBlock(_hoverProperties);
        }
        _hoverApplied = false;
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
