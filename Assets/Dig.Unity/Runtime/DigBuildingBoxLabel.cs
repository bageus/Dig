using System;
using Dig.Presentation.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigBuildingBoxLabel : MonoBehaviour
{
    private const float IconDepth = 0.035f;
    private const float IconScale = 0.13f;
    private static readonly Color IconMaterialTint = new Color(1f, 0.86f, 0.35f, 1f);

    private DigWorldItemVisual? _visual;
    private GameObject? _iconRoot;
    private string _itemId = string.Empty;
    private Camera? _camera;
    private Material? _iconMaterial;

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
        PositionIconOnCameraFacingFace();
    }

    private void PositionIconOnCameraFacingFace()
    {
        _camera ??= Camera.main;
        if (_camera == null || _iconRoot == null)
        {
            return;
        }

        Vector3 towardCamera = _camera.transform.position - transform.position;
        towardCamera.y = 0f;
        if (towardCamera.sqrMagnitude <= 0.0001f)
        {
            towardCamera = Vector3.forward;
        }

        towardCamera.Normalize();
        Vector3 faceNormal = Mathf.Abs(towardCamera.x) >= Mathf.Abs(towardCamera.z)
            ? new Vector3(Mathf.Sign(towardCamera.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(towardCamera.z));
        Bounds bounds = ResolveBoxBounds();
        Vector3 center = new Vector3(
            bounds.center.x,
            bounds.min.y + (bounds.size.y * 0.58f),
            bounds.center.z);
        float faceDistance = faceNormal.x != 0f
            ? bounds.extents.x
            : bounds.extents.z;
        _iconRoot.transform.position = center
            + (faceNormal * (faceDistance + IconDepth));
        _iconRoot.transform.rotation = Quaternion.LookRotation(
            faceNormal,
            Vector3.up);
    }

    private Bounds ResolveBoxBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.3f);
        bool found = false;
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer renderer = renderers[index];
            if (!renderer.enabled
                || (_iconRoot != null
                    && renderer.transform.IsChildOf(_iconRoot.transform)))
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private void EnsureIcon(string itemId)
    {
        if (_iconRoot != null && string.Equals(_itemId, itemId, StringComparison.Ordinal))
        {
            return;
        }

        if (_iconRoot != null)
        {
            Object.Destroy(_iconRoot);
        }

        _itemId = itemId;
        _iconRoot = new GameObject("Building box contents icon");
        _iconRoot.layer = 2;
        _iconRoot.transform.SetParent(transform, worldPositionStays: true);
        _iconRoot.transform.localScale = Vector3.one * IconScale;
        CreateIcon(itemId, _iconRoot.transform);
    }

    private void OnDestroy()
    {
        if (_iconMaterial != null)
        {
            Object.Destroy(_iconMaterial);
        }
    }

    private void CreateIcon(string itemId, Transform parent)
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

    private void CreateFlame(Transform parent)
    {
        CreateFlatPart(parent, "Fire bowl", new Vector2(0f, -0.24f),
            new Vector2(1.5f, 0.28f), new Color(0.30f, 0.15f, 0.06f));
        CreateFlatPart(parent, "Fire", new Vector2(0f, 0.35f),
            new Vector2(0.78f, 1.10f), new Color(1f, 0.55f, 0.08f));
    }

    private void CreateStoneAndHammer(Transform parent)
    {
        CreateFlatPart(parent, "Stone", new Vector2(-0.27f, 0f),
            new Vector2(0.82f, 0.68f), new Color(0.68f, 0.70f, 0.72f));
        CreateFlatPart(parent, "Hammer head", new Vector2(0.30f, 0.26f),
            new Vector2(0.72f, 0.25f), new Color(0.52f, 0.54f, 0.58f));
        CreateFlatPart(parent, "Hammer handle", new Vector2(0.30f, -0.12f),
            new Vector2(0.13f, 0.68f), new Color(0.48f, 0.27f, 0.12f));
    }

    private void CreateHammerAndSaw(Transform parent)
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

    private void CreateFood(Transform parent)
    {
        CreateFlatPart(parent, "Food", Vector2.zero,
            new Vector2(0.92f, 0.72f), new Color(0.92f, 0.45f, 0.14f));
        CreateFlatPart(parent, "Food highlight", new Vector2(-0.12f, 0.18f),
            new Vector2(0.22f, 0.18f), new Color(1f, 0.82f, 0.36f));
    }

    private void CreateBuildingSymbol(Transform parent)
    {
        CreateFlatPart(parent, "Building symbol", Vector2.zero,
            new Vector2(0.72f, 0.72f), new Color(0.86f, 0.74f, 0.42f));
    }

    private void CreateFlatPart(
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
            Object.Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = ResolveIconMaterial();
        DigMaterialColorUtility.SetColor(renderer.material, color);
    }

    private Material ResolveIconMaterial()
    {
        if (_iconMaterial != null)
        {
            return _iconMaterial;
        }

        DigRenderMaterialLibrary library = GetComponentInParent<DigRenderMaterialLibrary>()
            ?? throw new InvalidOperationException("Building box icon requires material library.");
        _iconMaterial = library.Resolve(
            RenderMaterialSemantic.Item,
            RenderSurfaceKind.Unlit,
            IconMaterialTint);
        return _iconMaterial;
    }
}

[RequireComponent(typeof(DigBuildingBoxLabel))]
public sealed partial class DigWorldItemVisual
{
}

}
