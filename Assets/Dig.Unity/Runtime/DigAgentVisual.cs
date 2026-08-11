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
    private ResidentEquipmentViewModel? _equipmentModel;
    private Material? _equipmentMaterial;
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
    private double _previousVisualY;
    private double _currentVisualY;
    private double _previousVisualZ;
    private double _currentVisualZ;
    private CellId? _freeformDestinationCell;
    private float _freeformDestinationOffsetX;
    private float _freeformDestinationOffsetZ;
    private float _directionalLaneOffsetX;
    private ResidentDirectionalLane _directionalLane;
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
        ResolveSurfaceCoordinates(
            model, out _currentVisualX, out _currentVisualY, out _currentVisualZ);
        _previousVisualX = _currentVisualX;
        _previousVisualY = _currentVisualY;
        _previousVisualZ = _currentVisualZ;
        transform.SetPositionAndRotation(ToWorld(
                _currentVisualX, _currentVisualY, _currentVisualZ),
            Quaternion.identity);
    }

    internal void SetModel(AgentViewModel model, float duration)
    {
        bool moving = _currentX != model.CellX || _currentY != model.CellY
            || _currentZ != model.CellZ
            || Model.SurfaceFace != model.SurfaceFace
            || Model.SurfaceU != model.SurfaceU
            || Model.SurfaceV != model.SurfaceV;
        Model = model;
        ApplyAction(moving);
        if (!moving)
        {
            return;
        }
        if (_freeformDestinationCell.HasValue
            && _currentX == _freeformDestinationCell.Value.X
            && _currentY == _freeformDestinationCell.Value.Y
            && _currentZ == _freeformDestinationCell.Value.Z)
        {
            _freeformDestinationCell = null;
            _freeformDestinationOffsetX = 0f;
            _freeformDestinationOffsetZ = 0f;
        }

        _previousX = _currentX;
        _previousY = _currentY;
        _previousZ = _currentZ;
        _previousVisualX = _currentVisualX;
        _previousVisualY = _currentVisualY;
        _previousVisualZ = _currentVisualZ;
        _currentX = model.CellX;
        _currentY = model.CellY;
        _currentZ = model.CellZ;
        ResidentDirectionalLanePreference lane = ResidentDirectionalLaneResolver.Resolve(
            _previousX,
            _previousY,
            _previousZ,
            _currentX,
            _currentY,
            _currentZ);
        _directionalLane = lane.Lane;
        // Cell traffic is coordinated by the simulation. A visual lane offset
        // changes the endpoint of every step and causes a backward correction
        // when the route changes direction, so cell travel stays center-to-center.
        _directionalLaneOffsetX = 0f;
        ResolveSurfaceCoordinates(
            model, out _currentVisualX, out _currentVisualY, out _currentVisualZ);
        _elapsed = 0f;
        _duration = Mathf.Max(0.01f, duration);
        PrepareTraversalKind();
        Face(ToWorld(_currentVisualX, _currentVisualY, _currentVisualZ)
            - transform.position);
    }

    internal void SetFreeformDestination(
        CellId cell,
        float offsetX,
        float offsetZ = 0f)
    {
        float limit = (float)TunnelMovementTargetResolver.MaximumOffsetX;
        _freeformDestinationCell = cell;
        _freeformDestinationOffsetX = Mathf.Clamp(offsetX, -limit, limit);
        _freeformDestinationOffsetZ = Mathf.Clamp(offsetZ, -limit, limit);
        if (_currentX == cell.X && _currentY == cell.Y && _currentZ == cell.Z)
        {
            _currentVisualX = cell.X + _freeformDestinationOffsetX;
            _currentVisualY = cell.Y;
            _currentVisualZ = cell.Z + _freeformDestinationOffsetZ;
            if (_duration <= 0f)
            {
                _previousVisualX = _currentVisualX;
                _previousVisualY = _currentVisualY;
                _previousVisualZ = _currentVisualZ;
                transform.position = ToWorld(
                    _currentVisualX, _currentVisualY, _currentVisualZ);
            }
        }
    }

    internal ResidentDirectionalLane DirectionalLane => _directionalLane;

    internal float DirectionalLaneOffsetX => _directionalLaneOffsetX;

    private double ResolveVisualX(int cellX, int cellY, int cellZ)
    {
        return _freeformDestinationCell.HasValue
            && _freeformDestinationCell.Value.X == cellX
            && _freeformDestinationCell.Value.Y == cellY
            && _freeformDestinationCell.Value.Z == cellZ
                ? cellX + _freeformDestinationOffsetX
                : cellX + _directionalLaneOffsetX;
    }

    internal void SetEquipment(ResidentEquipmentViewModel? equipment,
        Material equipmentMaterial)
    {
        if (equipment != null
            && !string.Equals(equipment.ResidentId, Model.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException("Equipment does not belong to this resident.",
                nameof(equipment));
        }

        _equipmentModel = equipment;
        _equipmentMaterial = equipmentMaterial
            ?? throw new ArgumentNullException(nameof(equipmentMaterial));
        RefreshHandEquipment();
        if (_workToolVisualKind == Dig.Presentation.Jobs.ResidentWorkToolVisualKind.None
            && equipment != null)
        {
            if (_equipmentVisual == null)
            {
                throw new InvalidOperationException(
                    "Right-hand equipment visual was not initialized.");
            }

            _equipmentVisual.Configure(
                equipment.ItemId,
                EquipmentAppearanceKind.Generic,
                equipmentMaterial);
        }
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
                    color = DigMaterialColorUtility.GetColor(
                        renderer.sharedMaterial,
                        Color.white);
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

    private static Vector3 ToWorld(CellId cell)
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
