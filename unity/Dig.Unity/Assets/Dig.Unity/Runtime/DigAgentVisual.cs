using System;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Navigation;
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
    private MaterialPropertyBlock? _hoverProperties;
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
    private double _previousVisualX;
    private double _currentVisualX;
    private SpatialCellId? _freeformDestinationCell;
    private float _freeformDestinationOffsetX;
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
        _previousVisualX = model.CellX;
        _currentVisualX = model.CellX;
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
        if (_freeformDestinationCell.HasValue
            && _currentX == _freeformDestinationCell.Value.X
            && _currentY == _freeformDestinationCell.Value.Y
            && _currentZ == _freeformDestinationCell.Value.Z)
        {
            _freeformDestinationCell = null;
            _freeformDestinationOffsetX = 0f;
        }

        _previousX = _currentX;
        _previousY = _currentY;
        _previousZ = _currentZ;
        _previousVisualX = _currentVisualX;
        _currentX = model.CellX;
        _currentY = model.CellY;
        _currentZ = model.CellZ;
        _currentVisualX = ResolveVisualX(model.CellX, model.CellY, model.CellZ);
        _elapsed = 0f;
        _duration = Mathf.Max(0.01f, duration);
        Face(ToWorld(_currentVisualX, _currentY, _currentZ) - transform.position);
    }

    internal void SetFreeformDestination(SpatialCellId cell, float offsetX)
    {
        float limit = (float)TunnelMovementTargetResolver.MaximumOffsetX;
        _freeformDestinationCell = cell;
        _freeformDestinationOffsetX = Mathf.Clamp(offsetX, -limit, limit);
        if (_currentX == cell.X && _currentY == cell.Y && _currentZ == cell.Z)
        {
            _previousVisualX = cell.X + _freeformDestinationOffsetX;
            _currentVisualX = _previousVisualX;
            transform.position = ToWorld(_currentVisualX, cell.Y, cell.Z);
        }
    }

    private double ResolveVisualX(int cellX, int cellY, int cellZ)
    {
        return _freeformDestinationCell.HasValue
            && _freeformDestinationCell.Value.X == cellX
            && _freeformDestinationCell.Value.Y == cellY
            && _freeformDestinationCell.Value.Z == cellZ
                ? cellX + _freeformDestinationOffsetX
                : cellX;
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
        if (_rig != null)
        {
            _rig.SetSelected(selected);
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].sharedMaterial = selected
                    ? _selectedMaterial
                    : _normalMaterial;
            }
        }

        _hoverApplied = false;
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
        MaterialPropertyBlock properties = ResolveHoverProperties();
        for (int index = 0; index < _hoverRenderers.Length; index++)
        {
            Renderer renderer = _hoverRenderers[index];
            properties.Clear();
            renderer.GetPropertyBlock(properties);
            Color color = properties.GetColor(BaseColorId);
            if (color == default)
            {
                color = properties.GetColor(ColorId);
                if (color == default && renderer.sharedMaterial != null)
                    color = renderer.sharedMaterial.color;
            }
            _hoverBaseColors[index] = color;
            Color highlighted = Color.Lerp(color, Color.white, HoverBlend);
            highlighted.a = color.a;
            properties.SetColor(BaseColorId, highlighted);
            properties.SetColor(ColorId, highlighted);
            renderer.SetPropertyBlock(properties);
        }
        _hoverApplied = _hoverRenderers.Length > 0;
    }

    private void RestoreHover()
    {
        MaterialPropertyBlock properties = ResolveHoverProperties();
        for (int index = 0; index < _hoverRenderers.Length; index++)
        {
            Renderer renderer = _hoverRenderers[index];
            properties.Clear();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, _hoverBaseColors[index]);
            properties.SetColor(ColorId, _hoverBaseColors[index]);
            renderer.SetPropertyBlock(properties);
        }
        _hoverApplied = false;
    }

    private MaterialPropertyBlock ResolveHoverProperties()
    {
        if (_hoverProperties == null)
        {
            _hoverProperties = new MaterialPropertyBlock();
        }

        return _hoverProperties;
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

    private static Vector3 ToWorld(double cellX, double cellY, double cellZ)
    {
        return DigTunnelProjection.ResidentWorldPosition(
            (float)cellX,
            (float)cellY,
            (float)cellZ);
    }
}
}
