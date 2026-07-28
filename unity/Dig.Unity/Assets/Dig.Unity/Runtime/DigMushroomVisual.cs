using System;
using Dig.Domain.Ecology;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class DigMushroomVisual : MonoBehaviour
{
    private const float HoverBlend = 0.48f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Transform? _stem;
    private Transform? _cap;
    private BoxCollider? _collider;
    private Material? _stemMaterial;
    private Material? _capMaterial;
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private Color[] _baseColors = Array.Empty<Color>();
    private MaterialPropertyBlock? _properties;
    private bool _hovered;

    internal MushroomSiteSnapshot Model { get; private set; } = null!;

    internal void Configure(MushroomSiteSnapshot model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        EnsureGeometry();
        (float height, float width) = ResolveSize(model.Stage);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        _stem!.localPosition = new Vector3(0f, height * 0.41f, 0f);
        _stem.localRotation = Quaternion.identity;
        _stem.localScale = new Vector3(width * 0.30f, height * 0.82f, width * 0.30f);
        _cap!.localPosition = new Vector3(0f, height * 0.84f, 0f);
        _cap.localRotation = Quaternion.identity;
        _cap.localScale = new Vector3(width, height * 0.30f, width * 0.82f);
        _collider!.center = new Vector3(0f, height * 0.5f, 0f);
        _collider.size = new Vector3(
            Mathf.Max(0.12f, width),
            height,
            Mathf.Max(0.10f, width * 0.72f));
        name = $"Mushroom {model.Stage} {model.SiteId}";
        if (_hovered)
        {
            ApplyHover();
        }
    }

    internal void SetHovered(bool hovered)
    {
        if (_hovered == hovered)
        {
            return;
        }

        _hovered = hovered;
        if (_hovered)
        {
            ApplyHover();
        }
        else
        {
            RestoreColors();
        }
    }

    private void EnsureGeometry()
    {
        if (_collider != null)
        {
            return;
        }

        _collider = GetComponent<BoxCollider>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? throw new InvalidOperationException("The URP/Lit mushroom shader was not found.");
        _stemMaterial = CreateMaterial(
            shader,
            "Dig Mushroom Stem",
            new Color(0.78f, 0.67f, 0.49f, 1f));
        _capMaterial = CreateMaterial(
            shader,
            "Dig Mushroom Cap",
            new Color(0.76f, 0.18f, 0.20f, 1f));
        _stem = CreatePrimitive(PrimitiveType.Cylinder, "Stem", _stemMaterial);
        _cap = CreatePrimitive(PrimitiveType.Sphere, "Cap", _capMaterial);
        _renderers = new[]
        {
            _stem.GetComponent<Renderer>(),
            _cap.GetComponent<Renderer>(),
        };
        _baseColors = new[] { _stemMaterial.color, _capMaterial.color };
    }

    private Transform CreatePrimitive(PrimitiveType type, string childName, Material material)
    {
        GameObject child = GameObject.CreatePrimitive(type);
        child.name = childName;
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, worldPositionStays: false);
        Collider? generated = child.GetComponent<Collider>();
        if (generated != null)
        {
            generated.enabled = false;
            Destroy(generated);
        }

        child.GetComponent<Renderer>().sharedMaterial = material;
        return child.transform;
    }

    private static Material CreateMaterial(Shader shader, string name, Color color)
    {
        return new Material(shader)
        {
            name = name,
            color = color,
            enableInstancing = true,
        };
    }

    private void ApplyHover()
    {
        MaterialPropertyBlock properties = ResolveProperties();
        for (int index = 0; index < _renderers.Length; index++)
        {
            Renderer renderer = _renderers[index];
            if (renderer == null)
            {
                continue;
            }

            Color highlighted = Color.Lerp(_baseColors[index], Color.white, HoverBlend);
            highlighted.a = _baseColors[index].a;
            properties.Clear();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, highlighted);
            properties.SetColor(ColorId, highlighted);
            renderer.SetPropertyBlock(properties);
        }
    }

    private void RestoreColors()
    {
        MaterialPropertyBlock properties = ResolveProperties();
        for (int index = 0; index < _renderers.Length; index++)
        {
            Renderer renderer = _renderers[index];
            if (renderer == null)
            {
                continue;
            }

            properties.Clear();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, _baseColors[index]);
            properties.SetColor(ColorId, _baseColors[index]);
            renderer.SetPropertyBlock(properties);
        }
    }

    private MaterialPropertyBlock ResolveProperties()
    {
        return _properties ??= new MaterialPropertyBlock();
    }

    private static (float Height, float Width) ResolveSize(MushroomStage stage)
    {
        return stage switch
        {
            MushroomStage.Tiny => (0.18f, 0.16f),
            MushroomStage.Small => (0.34f, 0.28f),
            MushroomStage.Medium => (0.56f, 0.44f),
            MushroomStage.Large => (0.84f, 0.62f),
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };
    }

    private void OnDestroy()
    {
        if (_stemMaterial != null)
        {
            Destroy(_stemMaterial);
        }

        if (_capMaterial != null)
        {
            Destroy(_capMaterial);
        }
    }
}
}
