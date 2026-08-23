using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigBuildingBoxLabel : MonoBehaviour
{
    private const float IconDepth = 0.015f;
    private const float IconScale = 0.13f;

    private DigWorldItemVisual? _visual;
    private GameObject? _iconRoot;
    private string _itemId = string.Empty;
    private Camera? _camera;

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
        FaceCameraOnFrontSurface();
    }

    private void FaceCameraOnFrontSurface()
    {
        _camera ??= Camera.main;
        if (_camera == null || _iconRoot == null)
        {
            return;
        }

        Vector3 towardCamera = _camera.transform.position - transform.position;
        if (towardCamera.sqrMagnitude <= 0.0001f)
        {
            towardCamera = Vector3.forward;
        }

        towardCamera.Normalize();
        _iconRoot.transform.position = transform.position
            + (towardCamera * ResolveFrontDepth())
            + (Vector3.up * 0.12f);
        _iconRoot.transform.rotation = Quaternion.LookRotation(
            towardCamera,
            _camera.transform.up);
    }

    private float ResolveFrontDepth()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        Bounds boxBounds = default;
        bool hasBoxBounds = false;
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer renderer = renderers[index];
            if (_iconRoot != null
                && renderer.transform.IsChildOf(_iconRoot.transform))
            {
                continue;
            }

            if (!renderer.enabled)
            {
                continue;
            }

            if (!hasBoxBounds)
            {
                boxBounds = renderer.bounds;
                hasBoxBounds = true;
            }
            else
            {
                boxBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBoxBounds)
        {
            return IconDepth;
        }

        Vector3 direction = ResolveCameraDirection();
        Vector3 extents = boxBounds.extents;
        float extent = Mathf.Abs(direction.x) * extents.x
            + Mathf.Abs(direction.y) * extents.y
            + Mathf.Abs(direction.z) * extents.z;
        return Mathf.Max(0.04f, extent) + IconDepth;
    }

    private Vector3 ResolveCameraDirection()
    {
        _camera ??= Camera.main;
        if (_camera == null)
        {
            return Vector3.forward;
        }

        Vector3 direction = _camera.transform.position - transform.position;
        return direction.sqrMagnitude <= 0.0001f
            ? Vector3.forward
            : direction.normalized;
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
        CreateFlatPart(parent, "Fire bowl", new Vector2(0f, -0.24f),
            new Vector2(1.5f, 0.28f), new Color(0.30f, 0.15f, 0.06f));
        CreateFlatPart(parent, "Fire", new Vector2(0f, 0.35f),
            new Vector2(0.78f, 1.10f), new Color(1f, 0.55f, 0.08f));
    }

    private static void CreateStoneAndHammer(Transform parent)
    {
        CreateFlatPart(parent, "Stone", new Vector2(-0.27f, 0f),
            new Vector2(0.82f, 0.68f), new Color(0.68f, 0.70f, 0.72f));
        CreateFlatPart(parent, "Hammer head", new Vector2(0.30f, 0.26f),
            new Vector2(0.72f, 0.25f), new Color(0.52f, 0.54f, 0.58f));
        CreateFlatPart(parent, "Hammer handle", new Vector2(0.30f, -0.12f),
            new Vector2(0.13f, 0.68f), new Color(0.48f, 0.27f, 0.12f));
    }

    private static void CreateHammerAndSaw(Transform parent)
    {
        CreateFlatPart(parent, "Hammer head", new Vector2(-0.28f, 0.27f),
            new Vector2(0.72f, 0.25f), new Color(0.52f, 0.54f, 0.58f));
        CreateFlatPart(parent, "Hammer handle", new Vector2(-0.28f, -0.12f),
            new Vector2(0.13f, 0.68f), new Color(0.48f, 0.27f, 0.12f));
        CreateFlatPart(parent, "Saw blade", new Vector2(0.28f, 0.02f),
            new Vector2(0.78f, 0.10f), new Color(0.72f, 0.74f, 0.78f));
        CreateFlatPart(parent, "Saw handle", new Vector2(0.06f, -0.20f),
            new Vector2(0.24f, 0.32f), new Color(0.60f, 0.30f, 0.10f));
    }

    private static void CreateFood(Transform parent)
    {
        CreateFlatPart(parent, "Food", Vector2.zero,
            new Vector2(0.92f, 0.72f), new Color(0.92f, 0.45f, 0.14f));
        CreateFlatPart(parent, "Food highlight", new Vector2(-0.12f, 0.18f),
            new Vector2(0.22f, 0.18f), new Color(1f, 0.82f, 0.36f));
    }

    private static void CreateBuildingSymbol(Transform parent)
    {
        CreateFlatPart(parent, "Building symbol", Vector2.zero,
            new Vector2(0.72f, 0.72f), new Color(0.86f, 0.74f, 0.42f));
    }

    private static void CreateFlatPart(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Quad);
        part.name = name;
        part.layer = 2;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = new Vector3(position.x, position.y, 0f);
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = new Vector3(size.x, size.y, 1f);
        Collider? collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.material.color = color;
    }
}

[RequireComponent(typeof(DigBuildingBoxLabel))]
public sealed partial class DigWorldItemVisual
{
}

}
