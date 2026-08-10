using System;
using System.Collections.Generic;
using Dig.Domain.WorldObjects;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class DigBarrelVisual : MonoBehaviour
{
    private const float PresentationScale = 0.70f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly List<Color> _baseColors = new List<Color>();
    private MaterialPropertyBlock? _properties;
    private BoxCollider? _collider;
    private bool _highlighted;

    internal BarrelSnapshot Model { get; private set; } = null!;
    internal float VisualHeight => 0.49f;
    internal bool IsHighlighted => _highlighted;

    internal void Configure(BarrelSnapshot model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        EnsureGeometry();
        _collider!.enabled = model.Lifecycle == BarrelLifecycle.Supported;
        name = $"Barrel {model.BarrelId}";
        ApplyHighlight();
    }

    internal void SetHighlighted(bool highlighted)
    {
        if (_highlighted == highlighted)
        {
            return;
        }

        _highlighted = highlighted;
        ApplyHighlight();
    }

    private void EnsureGeometry()
    {
        if (_collider != null)
        {
            return;
        }

        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
        _collider.center = new Vector3(0f, VisualHeight * 0.5f, 0f);
        _collider.size = Scale(new Vector3(0.62f, 0.70f, 0.54f));
        DigRenderMaterialLibrary library = GetComponentInParent<DigRenderMaterialLibrary>()
            ?? throw new InvalidOperationException("Barrel visual requires material library.");
        Material wood = library.Resolve(
            RenderMaterialSemantic.Item,
            RenderSurfaceKind.Lit,
            new Color(0.45f, 0.25f, 0.11f, 1f));
        Material metal = library.Resolve(
            RenderMaterialSemantic.Item,
            RenderSurfaceKind.Lit,
            new Color(0.25f, 0.28f, 0.31f, 1f));
        CreatePart(
            PrimitiveType.Cylinder,
            "Wood body",
            Scale(new Vector3(0f, 0.35f, 0f)),
            Scale(new Vector3(0.54f, 0.35f, 0.54f)),
            wood);
        CreatePart(
            PrimitiveType.Cylinder,
            "Upper hoop",
            Scale(new Vector3(0f, 0.55f, 0f)),
            Scale(new Vector3(0.58f, 0.04f, 0.58f)),
            metal);
        CreatePart(
            PrimitiveType.Cylinder,
            "Lower hoop",
            Scale(new Vector3(0f, 0.15f, 0f)),
            Scale(new Vector3(0.58f, 0.04f, 0.58f)),
            metal);
    }

    private void CreatePart(
        PrimitiveType type,
        string partName,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(transform, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = scale;
        Collider? collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        _renderers.Add(renderer);
        _baseColors.Add(DigMaterialColorUtility.GetColor(material, Color.white));
    }

    private static Vector3 Scale(Vector3 value)
    {
        return value * PresentationScale;
    }

    private void ApplyHighlight()
    {
        if (_renderers.Count == 0)
        {
            return;
        }

        _properties ??= new MaterialPropertyBlock();
        for (int index = 0; index < _renderers.Count; index++)
        {
            Color color = _highlighted
                ? Color.Lerp(
                    _baseColors[index],
                    new Color(1f, 0.06f, 0.04f, 1f),
                    0.72f)
                : _baseColors[index];
            _properties.Clear();
            _properties.SetColor(BaseColorId, color);
            _properties.SetColor(ColorId, color);
            _renderers[index].SetPropertyBlock(_properties);
        }
    }
}

}
