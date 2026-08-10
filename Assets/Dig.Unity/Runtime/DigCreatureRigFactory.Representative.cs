using System.Collections.Generic;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
internal static partial class DigCreatureRigFactory
{
    private static GameObject BuildRepresentative(
        CreatureVisualFamily family,
        Material material)
    {
        GameObject root = new GameObject("Low Poly Creature Rig " + family);
        root.AddComponent<DigVisualPrefabRoot>();
        List<Renderer> renderers = new List<Renderer>();
        Transform primary = CreatePivot(root.transform, "Primary Pivot", Vector3.zero);
        Transform secondary = CreatePivot(root.transform, "Secondary Pivot", Vector3.zero);
        switch (family)
        {
            case CreatureVisualFamily.Plant:
                BuildPlant(root.transform, primary, secondary, material, renderers);
                break;
            case CreatureVisualFamily.Vuker:
                BuildVuker(root.transform, primary, secondary, material, renderers);
                break;
            case CreatureVisualFamily.Arachnid:
                BuildArachnid(root.transform, primary, secondary, material, renderers);
                break;
            case CreatureVisualFamily.Biped:
                BuildBiped(root.transform, primary, secondary, material, renderers);
                break;
            case CreatureVisualFamily.LargeDemon:
                BuildDemon(root.transform, primary, secondary, material, renderers);
                break;
            default:
                BuildSmallCreature(root.transform, primary, material, renderers);
                break;
        }

        Transform[] markers = CreateMarkers(root.transform, material);
        Transform[] anchors = CreateAnchors(root.transform);
        DigCreatureRig rig = root.AddComponent<DigCreatureRig>();
        rig.Initialize(renderers.ToArray(), markers, anchors, primary, secondary);
        return root;
    }

    private static void BuildPlant(
        Transform root,
        Transform primary,
        Transform secondary,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(root, "Stem", PrimitiveType.Cube,
            new Vector3(0f, 0.48f, 0f), new Vector3(0.20f, 0.82f, 0.20f), material, renderers);
        CreatePart(root, "Head Accent", PrimitiveType.Sphere,
            new Vector3(0f, 1.05f, 0f), new Vector3(0.48f, 0.42f, 0.38f), material, renderers);
        primary.localPosition = new Vector3(-0.18f, 0.58f, 0f);
        secondary.localPosition = new Vector3(0.18f, 0.72f, 0f);
        CreatePart(primary, "Leaf Left Accent", PrimitiveType.Cube,
            new Vector3(-0.18f, 0f, 0f), new Vector3(0.36f, 0.10f, 0.24f), material, renderers);
        CreatePart(secondary, "Leaf Right Accent", PrimitiveType.Cube,
            new Vector3(0.18f, 0f, 0f), new Vector3(0.36f, 0.10f, 0.24f), material, renderers);
    }

    private static void BuildVuker(
        Transform root,
        Transform primary,
        Transform secondary,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(root, "Body", PrimitiveType.Sphere,
            new Vector3(0f, 0.58f, 0f), new Vector3(0.62f, 0.52f, 0.46f), material, renderers);
        CreatePart(root, "Head Accent", PrimitiveType.Cube,
            new Vector3(0f, 0.98f, 0.08f), new Vector3(0.42f, 0.32f, 0.36f), material, renderers);
        primary.localPosition = new Vector3(-0.28f, 0.48f, 0f);
        secondary.localPosition = new Vector3(0.28f, 0.48f, 0f);
        CreateLegPair(primary, "Left", material, renderers);
        CreateLegPair(secondary, "Right", material, renderers);
    }

    private static void BuildArachnid(
        Transform root,
        Transform primary,
        Transform secondary,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(root, "Body", PrimitiveType.Sphere,
            new Vector3(0f, 0.48f, 0f), new Vector3(0.62f, 0.40f, 0.48f), material, renderers);
        CreatePart(root, "Head Accent", PrimitiveType.Sphere,
            new Vector3(0f, 0.78f, 0.18f), new Vector3(0.34f, 0.28f, 0.34f), material, renderers);
        primary.localPosition = new Vector3(-0.30f, 0.48f, 0f);
        secondary.localPosition = new Vector3(0.30f, 0.48f, 0f);
        for (int index = -1; index <= 1; index++)
        {
            CreatePart(primary, "Left Leg " + index, PrimitiveType.Cube,
                new Vector3(-0.22f, index * 0.10f, index * 0.12f),
                new Vector3(0.46f, 0.08f, 0.08f), material, renderers);
            CreatePart(secondary, "Right Leg " + index, PrimitiveType.Cube,
                new Vector3(0.22f, index * 0.10f, index * 0.12f),
                new Vector3(0.46f, 0.08f, 0.08f), material, renderers);
        }
    }

    private static void BuildBiped(
        Transform root,
        Transform primary,
        Transform secondary,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(root, "Body", PrimitiveType.Cube,
            new Vector3(0f, 0.72f, 0f), new Vector3(0.48f, 0.62f, 0.34f), material, renderers);
        CreatePart(root, "Head Accent", PrimitiveType.Sphere,
            new Vector3(0f, 1.18f, 0f), new Vector3(0.36f, 0.34f, 0.34f), material, renderers);
        primary.localPosition = new Vector3(-0.28f, 0.82f, 0f);
        secondary.localPosition = new Vector3(0.28f, 0.82f, 0f);
        CreatePart(primary, "Left Arm", PrimitiveType.Cube,
            new Vector3(0f, -0.20f, 0f), new Vector3(0.14f, 0.48f, 0.14f), material, renderers);
        CreatePart(secondary, "Right Arm", PrimitiveType.Cube,
            new Vector3(0f, -0.20f, 0f), new Vector3(0.14f, 0.48f, 0.14f), material, renderers);
        CreatePart(root, "Left Leg", PrimitiveType.Cube,
            new Vector3(-0.13f, 0.28f, 0f), new Vector3(0.17f, 0.48f, 0.18f), material, renderers);
        CreatePart(root, "Right Leg", PrimitiveType.Cube,
            new Vector3(0.13f, 0.28f, 0f), new Vector3(0.17f, 0.48f, 0.18f), material, renderers);
    }

    private static void BuildDemon(
        Transform root,
        Transform primary,
        Transform secondary,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(root, "Body", PrimitiveType.Sphere,
            new Vector3(0f, 0.78f, 0f), new Vector3(0.88f, 0.88f, 0.62f), material, renderers);
        CreatePart(root, "Belly Accent", PrimitiveType.Sphere,
            new Vector3(0f, 0.72f, 0.34f), new Vector3(0.54f, 0.54f, 0.18f), material, renderers);
        CreatePart(root, "Head Accent", PrimitiveType.Cube,
            new Vector3(0f, 1.42f, 0f), new Vector3(0.52f, 0.42f, 0.42f), material, renderers);
        primary.localPosition = new Vector3(-0.48f, 0.90f, 0f);
        secondary.localPosition = new Vector3(0.48f, 0.90f, 0f);
        CreatePart(primary, "Left Arm", PrimitiveType.Cube,
            new Vector3(0f, -0.26f, 0f), new Vector3(0.20f, 0.62f, 0.20f), material, renderers);
        CreatePart(secondary, "Right Arm", PrimitiveType.Cube,
            new Vector3(0f, -0.26f, 0f), new Vector3(0.20f, 0.62f, 0.20f), material, renderers);
        CreatePart(root, "Left Leg", PrimitiveType.Cube,
            new Vector3(-0.24f, 0.24f, 0f), new Vector3(0.26f, 0.50f, 0.26f), material, renderers);
        CreatePart(root, "Right Leg", PrimitiveType.Cube,
            new Vector3(0.24f, 0.24f, 0f), new Vector3(0.26f, 0.50f, 0.26f), material, renderers);
    }

    private static void BuildSmallCreature(
        Transform root,
        Transform primary,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(root, "Body", PrimitiveType.Sphere,
            new Vector3(0f, 0.38f, 0f), new Vector3(0.52f, 0.34f, 0.40f), material, renderers);
        CreatePart(root, "Head Accent", PrimitiveType.Sphere,
            new Vector3(0.30f, 0.52f, 0f), new Vector3(0.30f, 0.28f, 0.28f), material, renderers);
        primary.localPosition = new Vector3(-0.30f, 0.40f, 0f);
        CreatePart(primary, "Tail Accent", PrimitiveType.Cube,
            new Vector3(-0.20f, 0f, 0f), new Vector3(0.38f, 0.08f, 0.08f), material, renderers);
    }

    private static void CreateLegPair(
        Transform pivot,
        string prefix,
        Material material,
        ICollection<Renderer> renderers)
    {
        CreatePart(pivot, prefix + " Front Leg", PrimitiveType.Cube,
            new Vector3(0f, -0.26f, 0.14f), new Vector3(0.13f, 0.52f, 0.13f), material, renderers);
        CreatePart(pivot, prefix + " Back Leg", PrimitiveType.Cube,
            new Vector3(0f, -0.26f, -0.14f), new Vector3(0.13f, 0.52f, 0.13f), material, renderers);
    }

    private static Transform CreatePivot(Transform parent, string name, Vector3 position)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, worldPositionStays: false);
        pivot.localPosition = position;
        return pivot;
    }

    private static void CreatePart(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 scale,
        Material material,
        ICollection<Renderer> renderers)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.layer = 2;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderers.Add(renderer);
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.Destroy(collider);
    }
}
}