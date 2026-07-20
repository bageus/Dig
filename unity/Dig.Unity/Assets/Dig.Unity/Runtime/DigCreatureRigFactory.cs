using System;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
internal static partial class DigCreatureRigFactory
{
    internal static DigCreatureRig Create(
        Transform parent,
        DigCreatureVisualResolution resolution,
        Material fallbackMaterial,
        CreatureAppearanceViewModel appearance)
    {
        int rendererBudget = Mathf.Clamp(resolution.MaximumRenderers, 3, 32);
        Material material = resolution.Asset.Material ?? fallbackMaterial;
        GameObject root;
        DigCreatureRig rig;
        if (resolution.Asset.Prefab != null)
        {
            root = UnityEngine.Object.Instantiate(resolution.Asset.Prefab);
            if (!TryConfigureAuthoredRig(root, material, rendererBudget, out rig))
            {
                UnityEngine.Object.Destroy(root);
                root = BuildRepresentative(resolution.Family, material);
                rig = root.GetComponent<DigCreatureRig>();
            }
        }
        else
        {
            root = BuildRepresentative(resolution.Family, material);
            rig = root.GetComponent<DigCreatureRig>();
        }

        root.name = "Creature Rig " + appearance.CreatureId;
        root.transform.SetParent(parent, worldPositionStays: false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        DisableChildColliders(root);
        rig.ConfigureScale(resolution.Scale);
        rig.ApplyAppearance(appearance);
        return rig;
    }

    private static bool TryConfigureAuthoredRig(
        GameObject root,
        Material material,
        int maximumRenderers,
        out DigCreatureRig rig)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers.Length < 3 || renderers.Length > maximumRenderers)
        {
            rig = null!;
            return false;
        }

        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index].sharedMaterial == null)
                renderers[index].sharedMaterial = material;
        }

        Transform primary = FindOrCreate(root.transform, "Primary Pivot");
        Transform secondary = FindOrCreate(root.transform, "Secondary Pivot");
        Transform[] markers = CreateMarkers(root.transform, material);
        Transform[] anchors = CreateAnchors(root.transform);
        rig = root.GetComponent<DigCreatureRig>() ?? root.AddComponent<DigCreatureRig>();
        rig.Initialize(renderers, markers, anchors, primary, secondary);
        return true;
    }

    private static Transform[] CreateMarkers(Transform root, Material material)
    {
        Transform markerRoot = FindOrCreate(root, "Creature Markers");
        markerRoot.localPosition = Vector3.zero;
        Transform ring = CreateMarkerShape(markerRoot, "Marker Ring");
        CreateMarkerPart(ring, "Ring Left", new Vector3(-0.42f, 0.05f, 0f),
            new Vector3(0.08f, 0.08f, 0.42f), Quaternion.identity, material);
        CreateMarkerPart(ring, "Ring Right", new Vector3(0.42f, 0.05f, 0f),
            new Vector3(0.08f, 0.08f, 0.42f), Quaternion.identity, material);
        CreateMarkerPart(ring, "Ring Front", new Vector3(0f, 0.05f, 0.34f),
            new Vector3(0.76f, 0.08f, 0.08f), Quaternion.identity, material);
        CreateMarkerPart(ring, "Ring Back", new Vector3(0f, 0.05f, -0.34f),
            new Vector3(0.76f, 0.08f, 0.08f), Quaternion.identity, material);

        Transform shield = CreateMarkerShape(markerRoot, "Marker Shield");
        CreateMarkerPart(shield, "Shield Diamond", new Vector3(0f, 1.25f, 0f),
            new Vector3(0.28f, 0.28f, 0.06f),
            Quaternion.Euler(0f, 0f, 45f), material);

        Transform spikes = CreateMarkerShape(markerRoot, "Marker Spikes");
        for (int index = -1; index <= 1; index++)
        {
            CreateMarkerPart(spikes, "Hostile Spike " + index,
                new Vector3(index * 0.24f, 1.30f, 0f),
                new Vector3(0.10f, 0.30f, 0.08f),
                Quaternion.Euler(0f, 0f, index * 18f), material);
        }

        return new[] { ring, shield, spikes };
    }

    private static Transform CreateMarkerShape(Transform parent, string name)
    {
        Transform shape = FindOrCreate(parent, name);
        shape.localPosition = Vector3.zero;
        shape.localRotation = Quaternion.identity;
        shape.localScale = Vector3.one;
        return shape;
    }

    private static void CreateMarkerPart(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.layer = 2;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.Destroy(collider);
    }

    private static Transform[] CreateAnchors(Transform root)
    {
        return new[]
        {
            CreateAnchor(root, "Anchor Equipment", new Vector3(0.45f, 0.72f, 0f)),
            CreateAnchor(root, "Anchor Drop", new Vector3(0f, 0.10f, 0.45f)),
            CreateAnchor(root, "Anchor Inside Creature", new Vector3(0f, 0.65f, 0f)),
            CreateAnchor(root, "Anchor VFX", new Vector3(0f, 1.20f, 0f)),
        };
    }

    private static Transform CreateAnchor(Transform parent, string name, Vector3 position)
    {
        Transform anchor = FindOrCreate(parent, name);
        anchor.localPosition = position;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;
        return anchor;
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;
        Transform created = new GameObject(name).transform;
        created.SetParent(parent, worldPositionStays: false);
        return created;
    }

    private static void DisableChildColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
        for (int index = 0; index < colliders.Length; index++)
            colliders[index].enabled = false;
    }
}
}