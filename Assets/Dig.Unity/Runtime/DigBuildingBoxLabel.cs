using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigBuildingBoxLabel : MonoBehaviour
{
    private static readonly Vector3 IconOffset = new Vector3(0f, 0.12f, -0.24f);
    private const float IconScale = 0.13f;

    private DigWorldItemVisual? _visual;
    private GameObject? _iconRoot;
    private string _itemId = string.Empty;

    private void Awake()
    {
        _visual = GetComponent<DigWorldItemVisual>();
    }

    private void LateUpdate()
    {
        if (_visual == null)
        {
            _visual = GetComponent<DigWorldItemVisual>();
        }

        string itemId = _visual?.Model?.ItemId ?? string.Empty;
        if (!DigWorldItemVisualPolicy.IsBuildingBox(itemId)
            && !string.Equals(itemId, "package.food", StringComparison.Ordinal))
        {
            if (_iconRoot != null) _iconRoot.SetActive(false);
            _itemId = string.Empty;
            return;
        }

        EnsureIcon(itemId);
        _iconRoot!.SetActive(true);
        _iconRoot.transform.localPosition = IconOffset;
        _iconRoot.transform.localRotation = Quaternion.identity;
    }

    private void EnsureIcon(string itemId)
    {
        if (_iconRoot != null && string.Equals(_itemId, itemId, StringComparison.Ordinal))
        {
            return;
        }

        if (_iconRoot != null)
        {
            Destroy(_iconRoot);
        }

        _itemId = itemId;
        _iconRoot = new GameObject("Building box contents icon");
        _iconRoot.layer = 2;
        _iconRoot.transform.SetParent(transform, worldPositionStays: false);
        _iconRoot.transform.localPosition = IconOffset;
        _iconRoot.transform.localRotation = Quaternion.identity;
        _iconRoot.transform.localScale = Vector3.one * IconScale;
        CreateIcon(itemId, _iconRoot.transform);
    }

    private static void CreateIcon(string itemId, Transform parent)
    {
        if (itemId == "building_box.campfire")
        {
            CreateFlame(parent);
            return;
        }

        if (itemId == "building_box.stone_mason")
        {
            CreateStoneAndHammer(parent);
            return;
        }

        if (itemId == "building_box.wood_workshop")
        {
            CreateHammerAndSaw(parent);
            return;
        }

        if (itemId == "package.food")
        {
            CreateFood(parent);
            return;
        }

        CreateBuildingSymbol(parent);
    }

    private static void CreateFlame(Transform parent)
    {
        CreatePart(parent, "Fire bowl", PrimitiveType.Cube, new Vector3(0f, -0.24f, 0f),
            new Vector3(1.5f, 0.28f, 0.55f), new Color(0.30f, 0.15f, 0.06f));
        CreatePart(parent, "Fire", PrimitiveType.Sphere, new Vector3(0f, 0.35f, -0.02f),
            new Vector3(0.78f, 1.10f, 0.34f), new Color(1f, 0.55f, 0.08f));
    }

    private static void CreateStoneAndHammer(Transform parent)
    {
        CreatePart(parent, "Stone", PrimitiveType.Sphere, new Vector3(-0.27f, 0f, 0f),
            new Vector3(0.82f, 0.68f, 0.40f), new Color(0.68f, 0.70f, 0.72f));
        CreatePart(parent, "Hammer head", PrimitiveType.Cube, new Vector3(0.30f, 0.26f, 0f),
            new Vector3(0.72f, 0.25f, 0.22f), new Color(0.52f, 0.54f, 0.58f));
        CreatePart(parent, "Hammer handle", PrimitiveType.Cube, new Vector3(0.30f, -0.12f, 0f),
            new Vector3(0.13f, 0.68f, 0.13f), new Color(0.48f, 0.27f, 0.12f));
    }

    private static void CreateHammerAndSaw(Transform parent)
    {
        CreatePart(parent, "Hammer head", PrimitiveType.Cube, new Vector3(-0.28f, 0.27f, 0f),
            new Vector3(0.72f, 0.25f, 0.22f), new Color(0.52f, 0.54f, 0.58f));
        CreatePart(parent, "Hammer handle", PrimitiveType.Cube, new Vector3(-0.28f, -0.12f, 0f),
            new Vector3(0.13f, 0.68f, 0.13f), new Color(0.48f, 0.27f, 0.12f));
        CreatePart(parent, "Saw blade", PrimitiveType.Cube, new Vector3(0.28f, 0.02f, 0f),
            new Vector3(0.78f, 0.10f, 0.16f), new Color(0.72f, 0.74f, 0.78f));
        CreatePart(parent, "Saw handle", PrimitiveType.Cube, new Vector3(0.06f, -0.20f, 0f),
            new Vector3(0.24f, 0.32f, 0.18f), new Color(0.60f, 0.30f, 0.10f));
    }

    private static void CreateFood(Transform parent)
    {
        CreatePart(parent, "Food", PrimitiveType.Sphere, Vector3.zero,
            new Vector3(0.92f, 0.72f, 0.40f), new Color(0.92f, 0.45f, 0.14f));
        CreatePart(parent, "Food highlight", PrimitiveType.Sphere, new Vector3(-0.12f, 0.18f, -0.16f),
            new Vector3(0.22f, 0.18f, 0.08f), new Color(1f, 0.82f, 0.36f));
    }

    private static void CreateBuildingSymbol(Transform parent)
    {
        CreatePart(parent, "Building symbol", PrimitiveType.Cube, Vector3.zero,
            new Vector3(0.72f, 0.72f, 0.20f), new Color(0.86f, 0.74f, 0.42f));
    }

    private static void CreatePart(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 scale,
        Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.layer = 2;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.material.color = color;
    }
}

[RequireComponent(typeof(DigBuildingBoxLabel))]
public sealed partial class DigWorldItemVisual
{
}

}
