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
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly List<Color> _baseColors = new List<Color>();
    private MaterialPropertyBlock? _properties;
    private BoxCollider? _collider;
    private bool _highlighted;

    internal BarrelSnapshot Model { get; private set; } = null!;
    internal float VisualHeight => 1.05f;
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
        _collider.size = new Vector3(0.82f, VisualHeight, 0.66f);
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
            new Vector3(0f, 0.52f, 0f),
            new Vector3(0.72f, 0.52f, 0.72f),
            wood);
        CreatePart(
            PrimitiveType.Cylinder,
            "Upper hoop",
            new Vector3(0f, 0.82f, 0f),
            new Vector3(0.77f, 0.055f, 0.77f),
            metal);
        CreatePart(
            PrimitiveType.Cylinder,
            "Lower hoop",
            new Vector3(0f, 0.23f, 0f),
            new Vector3(0.77f, 0.055f, 0.77f),
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
        part.transform.localScale = scale;
        Collider? collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        _renderers.Add(renderer);
        _baseColors.Add(material.color);
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