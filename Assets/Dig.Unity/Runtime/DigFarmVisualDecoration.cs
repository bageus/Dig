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

    internal static void Ensure(GameObject buildingRoot)
    {
        if (buildingRoot.GetComponent<DigFarmVisualDecoration>() == null)
        {
            buildingRoot.AddComponent<DigFarmVisualDecoration>().Build();
        }
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
        Color tint)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

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
