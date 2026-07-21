using System;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
public enum DigCreatureAnchorKind
{
    Equipment = 0,
    Drop = 1,
    InsideCreature = 2,
    Vfx = 3,
}

[DisallowMultipleComponent]
public sealed class DigCreatureRig : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer[] _bodyRenderers = Array.Empty<Renderer>();
    private Transform[] _markerShapes = Array.Empty<Transform>();
    private Transform[] _anchors = Array.Empty<Transform>();
    private Transform? _primaryPivot;
    private Transform? _secondaryPivot;
    private MaterialPropertyBlock? _properties;
    private CreatureAppearanceViewModel? _appearance;
    private Vector3 _profileScale = Vector3.one;
    private bool _selected;
    private bool _bodyVisible = true;

    internal void Initialize(
        Renderer[] bodyRenderers,
        Transform[] markerShapes,
        Transform[] anchors,
        Transform primaryPivot,
        Transform secondaryPivot)
    {
        if (bodyRenderers == null || bodyRenderers.Length < 3 || bodyRenderers.Length > 32)
            throw new ArgumentOutOfRangeException(nameof(bodyRenderers));
        if (markerShapes == null || markerShapes.Length != 3)
            throw new ArgumentException("Creature rig requires three marker shapes.", nameof(markerShapes));
        if (anchors == null || anchors.Length != 4)
            throw new ArgumentException("Creature rig requires four anchors.", nameof(anchors));
        _bodyRenderers = bodyRenderers;
        _markerShapes = markerShapes;
        _anchors = anchors;
        _primaryPivot = primaryPivot;
        _secondaryPivot = secondaryPivot;
    }

    internal void ConfigureScale(Vector3 profileScale)
    {
        _profileScale = profileScale;
        RefreshScale();
    }

    internal Transform ResolveAnchor(DigCreatureAnchorKind kind)
    {
        int index = (int)kind;
        return index >= 0 && index < _anchors.Length ? _anchors[index] : transform;
    }

    internal void ApplyAppearance(CreatureAppearanceViewModel appearance)
    {
        _appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        RefreshScale();
        RefreshColors();
        RefreshMarkers();
    }

    internal void SetSelected(bool selected)
    {
        _selected = selected;
        RefreshColors();
    }

    internal void ApplyAction(CreatureActionVisualViewModel action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        ResetPose();
        float phase = (float)(action.NormalizedProgress * Mathf.PI * 2f);
        float swing = Mathf.Sin(phase) * 24f;
        switch (action.State)
        {
            case CreatureActionVisualState.Move:
                SetPivots(swing, -swing);
                break;
            case CreatureActionVisualState.Attack:
                SetPivots(-48f, 24f);
                break;
            case CreatureActionVisualState.Hit:
                transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
                break;
            case CreatureActionVisualState.Death:
                transform.localRotation = Quaternion.Euler(0f, 0f, 86f);
                break;
            case CreatureActionVisualState.Growth:
                SetPivots(swing * 0.25f, swing * 0.25f);
                break;
            case CreatureActionVisualState.Special:
                SetPivots(-60f, -60f);
                break;
        }
    }

    internal void ApplyLod(CreatureLodViewModel lod)
    {
        if (lod == null) throw new ArgumentNullException(nameof(lod));
        _bodyVisible = lod.RenderBody;
        int visibleCount = lod.Tier == CreatureLodTier.Far
            ? Mathf.Max(1, _bodyRenderers.Length / 2)
            : _bodyRenderers.Length;
        for (int index = 0; index < _bodyRenderers.Length; index++)
            _bodyRenderers[index].enabled = _bodyVisible && index < visibleCount;
        RefreshMarkers();
    }

    private void RefreshScale()
    {
        float lifecycleScale = _appearance?.LifecycleStage switch
        {
            CreatureLifecycleVisualStage.Seed => 0.35f,
            CreatureLifecycleVisualStage.Egg => 0.45f,
            CreatureLifecycleVisualStage.Larva => 0.55f,
            CreatureLifecycleVisualStage.Child => 0.72f,
            _ => 1f,
        };
        transform.localScale = _profileScale * lifecycleScale;
    }

    private void RefreshColors()
    {
        if (_appearance == null) return;
        Color body = ResolveBodyColor(_appearance.Family, _appearance.BodyPaletteIndex);
        Color accent = ResolveAccentColor(_appearance.AccentPaletteIndex);
        for (int index = 0; index < _bodyRenderers.Length; index++)
        {
            Renderer renderer = _bodyRenderers[index];
            string part = renderer.gameObject.name;
            Color color = part.Contains("Accent", StringComparison.Ordinal)
                || part.Contains("Head", StringComparison.Ordinal)
                || part.Contains("Leaf", StringComparison.Ordinal)
                ? accent
                : body;
            if (_selected)
                color = Color.Lerp(color, new Color(1f, 0.78f, 0.18f, 1f), 0.55f);
            ApplyColor(renderer, color);
        }

        Color markerColor = _appearance.Disposition switch
        {
            CreatureDisposition.Tamed => new Color(0.30f, 0.78f, 1f, 1f),
            CreatureDisposition.Hostile => new Color(1f, 0.25f, 0.18f, 1f),
            _ => new Color(0.85f, 0.85f, 0.72f, 1f),
        };
        for (int index = 0; index < _markerShapes.Length; index++)
        {
            Renderer[] renderers = _markerShapes[index].GetComponentsInChildren<Renderer>(true);
            for (int markerIndex = 0; markerIndex < renderers.Length; markerIndex++)
                ApplyColor(renderers[markerIndex], markerColor);
        }
    }

    private void RefreshMarkers()
    {
        if (_appearance == null) return;
        int activeIndex = (int)_appearance.MarkerShape;
        for (int index = 0; index < _markerShapes.Length; index++)
            _markerShapes[index].gameObject.SetActive(_bodyVisible && index == activeIndex);
    }

    private void ApplyColor(Renderer renderer, Color color)
    {
        MaterialPropertyBlock properties = ResolveProperties();
        properties.Clear();
        renderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColorId, color);
        properties.SetColor(ColorId, color);
        renderer.SetPropertyBlock(properties);
    }

    private MaterialPropertyBlock ResolveProperties()
    {
        if (_properties == null)
        {
            _properties = new MaterialPropertyBlock();
        }

        return _properties;
    }

    private void ResetPose()
    {
        transform.localRotation = Quaternion.identity;
        SetPivots(0f, 0f);
    }

    private void SetPivots(float primary, float secondary)
    {
        if (_primaryPivot != null)
            _primaryPivot.localRotation = Quaternion.Euler(primary, 0f, 0f);
        if (_secondaryPivot != null)
            _secondaryPivot.localRotation = Quaternion.Euler(secondary, 0f, 0f);
    }

    private static Color ResolveBodyColor(CreatureVisualFamily family, int index)
    {
        Color[] familyColors =
        {
            new Color(0.30f, 0.62f, 0.24f), new Color(0.48f, 0.36f, 0.22f),
            new Color(0.28f, 0.24f, 0.36f), new Color(0.42f, 0.34f, 0.26f),
            new Color(0.48f, 0.18f, 0.16f), new Color(0.55f, 0.48f, 0.30f),
        };
        Color baseColor = familyColors[Mathf.Clamp((int)family, 0, familyColors.Length - 1)];
        float offset = (Mathf.Clamp(index, 0, 7) - 3.5f) * 0.035f;
        return new Color(
            Mathf.Clamp01(baseColor.r + offset),
            Mathf.Clamp01(baseColor.g + offset),
            Mathf.Clamp01(baseColor.b + offset),
            1f);
    }

    private static Color ResolveAccentColor(int index)
    {
        Color[] colors =
        {
            new Color(0.90f, 0.72f, 0.24f), new Color(0.62f, 0.18f, 0.16f),
            new Color(0.24f, 0.72f, 0.60f), new Color(0.58f, 0.32f, 0.72f),
            new Color(0.84f, 0.42f, 0.18f), new Color(0.76f, 0.76f, 0.72f),
        };
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }
}
}
