using System;
using Dig.Domain.Farming;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigFarmVisualDecoration : MonoBehaviour
{
    private const float FarmWidth = 2f;
    private const float FarmDepth = 1.5f;
    private const float FenceHeight = 0.5f;
    private const float RailThickness = 0.08f;
    private const float DirtThickness = 0.08f;
    private readonly GameObject[] _mushrooms = new GameObject[3];
    private readonly GameObject[] _hamsters = new GameObject[8];
    private readonly GameObject[] _grubs = new GameObject[8];
    private readonly GameObject[] _feedCaps = new GameObject[2];
    private bool _contentsBuilt;

    internal static void Ensure(GameObject buildingRoot)
    {
        if (buildingRoot.GetComponent<DigFarmVisualDecoration>() == null)
        {
            buildingRoot.AddComponent<DigFarmVisualDecoration>().Build();
        }
    }

    internal void SetState(FarmSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        EnsureContents();
        int mushrooms = Mathf.Min(
            _mushrooms.Length,
            snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms);
        int hamsters = Mathf.Min(
            _hamsters.Length,
            snapshot.HamsterCount + snapshot.EscapingHamsterCount);
        int grubs = Mathf.Min(
            _grubs.Length,
            snapshot.GrubCount + snapshot.EscapingGrubCount);
        SetVisible(_mushrooms, mushrooms);
        SetVisible(_hamsters, hamsters);
        SetVisible(_grubs, grubs);
        SetVisible(_feedCaps, Mathf.Min(_feedCaps.Length, snapshot.FeedCount));
    }

    private void Build()
    {
        Transform visualRoot = new GameObject("Farm Decoration").transform;
        visualRoot.SetParent(transform, worldPositionStays: false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;

        CreatePart(
            "Dirt",
            visualRoot,
            new Vector3(0f, DirtThickness * 0.5f, 0f),
            new Vector3(FarmWidth, DirtThickness, FarmDepth),
            new Color(0.33f, 0.20f, 0.11f, 1f));

        float railY = FenceHeight * 0.72f;
        float halfWidth = FarmWidth * 0.5f;
        float halfDepth = FarmDepth * 0.5f;
        Color wood = new Color(0.39f, 0.24f, 0.12f, 1f);

        CreatePart(
            "Fence Front",
            visualRoot,
            new Vector3(0f, railY, -halfDepth),
            new Vector3(FarmWidth, RailThickness, RailThickness),
            wood);
        CreatePart(
            "Fence Back",
            visualRoot,
            new Vector3(0f, railY, halfDepth),
            new Vector3(FarmWidth, RailThickness, RailThickness),
            wood);
        CreatePart(
            "Fence Left",
            visualRoot,
            new Vector3(-halfWidth, railY, 0f),
            new Vector3(RailThickness, RailThickness, FarmDepth),
            wood);
        CreatePart(
            "Fence Right",
            visualRoot,
            new Vector3(halfWidth, railY, 0f),
            new Vector3(RailThickness, RailThickness, FarmDepth),
            wood);

        CreatePost("Post FL", visualRoot, -halfWidth, -halfDepth, wood);
        CreatePost("Post FR", visualRoot, halfWidth, -halfDepth, wood);
        CreatePost("Post BL", visualRoot, -halfWidth, halfDepth, wood);
        CreatePost("Post BR", visualRoot, halfWidth, halfDepth, wood);
        EnsureContents();
    }

    private void EnsureContents()
    {
        if (_contentsBuilt) return;
        _contentsBuilt = true;
        Transform contents = new GameObject("Farm Contents").transform;
        contents.SetParent(transform, worldPositionStays: false);
        contents.localPosition = Vector3.zero;
        contents.localRotation = Quaternion.identity;

        Vector3[] mushroomPositions =
        {
            new Vector3(-0.58f, 0.10f, -0.22f),
            new Vector3(0f, 0.10f, 0.24f),
            new Vector3(0.58f, 0.10f, -0.18f),
        };
        for (int index = 0; index < _mushrooms.Length; index++)
        {
            _mushrooms[index] = CreateMushroom(
                contents,
                index,
                mushroomPositions[index]);
        }

        for (int index = 0; index < _hamsters.Length; index++)
        {
            _hamsters[index] = CreateAnimal(
                contents,
                "Hamster " + index,
                ResolveAnimalPosition(index),
                new Vector3(0.22f, 0.16f, 0.16f),
                new Color(0.64f, 0.38f, 0.18f, 1f));
        }

        for (int index = 0; index < _grubs.Length; index++)
        {
            Vector3 position = ResolveAnimalPosition(index);
            position.z += 0.06f;
            _grubs[index] = CreateAnimal(
                contents,
                "Grub " + index,
                position,
                new Vector3(0.25f, 0.10f, 0.12f),
                new Color(0.68f, 0.82f, 0.35f, 1f));
        }

        for (int index = 0; index < _feedCaps.Length; index++)
        {
            _feedCaps[index] = CreateFeedCap(contents, index);
        }

        SetVisible(_mushrooms, 0);
        SetVisible(_hamsters, 0);
        SetVisible(_grubs, 0);
        SetVisible(_feedCaps, 0);
    }

    private static GameObject CreateMushroom(
        Transform parent,
        int index,
        Vector3 position)
    {
        GameObject root = new GameObject("Mushroom " + index);
        root.transform.SetParent(parent, worldPositionStays: false);
        root.transform.localPosition = position;
        CreatePart(
            "Stem",
            root.transform,
            new Vector3(0f, 0.12f, 0f),
            new Vector3(0.10f, 0.24f, 0.10f),
            new Color(0.78f, 0.68f, 0.49f, 1f),
            PrimitiveType.Cylinder);
        CreatePart(
            "Cap",
            root.transform,
            new Vector3(0f, 0.27f, 0f),
            new Vector3(0.30f, 0.13f, 0.30f),
            new Color(0.70f, 0.22f, 0.16f, 1f),
            PrimitiveType.Sphere);
        return root;
    }

    private static GameObject CreateAnimal(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Color tint)
    {
        GameObject animal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        animal.name = name;
        animal.transform.SetParent(parent, worldPositionStays: false);
        animal.transform.localPosition = position;
        animal.transform.localRotation = Quaternion.identity;
        animal.transform.localScale = scale;
        RemoveColliderAndTint(animal, tint);
        return animal;
    }

    private static GameObject CreateFeedCap(Transform parent, int index)
    {
        GameObject root = new GameObject("Feed Cap " + index);
        root.transform.SetParent(parent, worldPositionStays: false);
        root.transform.localPosition = new Vector3(
            -0.09f + (index * 0.18f),
            0.12f + (index * 0.035f),
            0f);
        root.transform.localRotation = Quaternion.Euler(0f, index * 32f, 0f);
        CreatePart(
            "Cap",
            root.transform,
            Vector3.zero,
            new Vector3(0.24f, 0.08f, 0.20f),
            new Color(0.70f, 0.22f, 0.16f, 1f),
            PrimitiveType.Sphere);
        return root;
    }

    private static Vector3 ResolveAnimalPosition(int index)
    {
        int column = index % 4;
        int row = index / 4;
        return new Vector3(
            -0.66f + (column * 0.44f),
            0.13f,
            -0.30f + (row * 0.55f));
    }

    private static void SetVisible(GameObject[] values, int count)
    {
        for (int index = 0; index < values.Length; index++)
        {
            values[index].SetActive(index < count);
        }
    }

    private static void CreatePost(
        string name,
        Transform parent,
        float x,
        float z,
        Color tint)
    {
        CreatePart(
            name,
            parent,
            new Vector3(x, FenceHeight * 0.5f, z),
            new Vector3(RailThickness, FenceHeight, RailThickness),
            tint);
    }

    private static void CreatePart(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color tint,
        PrimitiveType primitive = PrimitiveType.Cube)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        RemoveColliderAndTint(part, tint);
    }

    private static void RemoveColliderAndTint(GameObject part, Color tint)
    {
        Collider? collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer? renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = tint;
        }
    }
}

}
