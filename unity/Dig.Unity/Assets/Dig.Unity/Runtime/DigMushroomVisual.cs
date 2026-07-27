using System;
using Dig.Domain.Ecology;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class DigMushroomVisual : MonoBehaviour
{
    private Transform? _stem;
    private Transform? _cap;
    private BoxCollider? _collider;

    internal MushroomSiteSnapshot Model { get; private set; } = null!;

    internal void Configure(MushroomSiteSnapshot model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        EnsureGeometry();
        (float height, float width) = ResolveSize(model.Stage);
        _stem!.localPosition = new Vector3(0f, height * 0.42f, 0f);
        _stem.localScale = new Vector3(width * 0.34f, height * 0.84f, width * 0.34f);
        _cap!.localPosition = new Vector3(0f, height * 0.88f, 0f);
        _cap.localScale = new Vector3(width, height * 0.34f, width * 0.82f);
        _collider!.center = new Vector3(0f, height * 0.5f, 0f);
        _collider.size = new Vector3(
            Mathf.Max(0.35f, width),
            Mathf.Max(0.35f, height),
            Mathf.Max(0.28f, width * 0.72f));
        name = $"Mushroom {model.Stage} {model.SiteId}";
    }

    private void EnsureGeometry()
    {
        if (_collider != null)
        {
            return;
        }

        _collider = GetComponent<BoxCollider>();
        _stem = CreatePrimitive(
            PrimitiveType.Cylinder,
            "Stem",
            new Color(0.78f, 0.67f, 0.49f, 1f));
        _cap = CreatePrimitive(
            PrimitiveType.Sphere,
            "Cap",
            new Color(0.76f, 0.18f, 0.20f, 1f));
    }

    private Transform CreatePrimitive(PrimitiveType type, string childName, Color color)
    {
        GameObject child = GameObject.CreatePrimitive(type);
        child.name = childName;
        child.transform.SetParent(transform, worldPositionStays: false);
        Collider? collider = child.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = child.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"))
        {
            color = color,
        };
        return child.transform;
    }

    private static (float Height, float Width) ResolveSize(MushroomStage stage)
    {
        return stage switch
        {
            MushroomStage.Tiny => (0.34f, 0.30f),
            MushroomStage.Small => (0.58f, 0.48f),
            MushroomStage.Medium => (0.88f, 0.68f),
            MushroomStage.Large => (1.34f, 0.92f),
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };
    }
}
}
