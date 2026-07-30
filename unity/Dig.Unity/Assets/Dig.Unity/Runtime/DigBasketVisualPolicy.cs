using System;
using Dig.Domain.Content;
using UnityEngine;

namespace Dig.Unity
{
internal static partial class DigBasketVisualPolicy
{
    private const string ProceduralPrefix = "procedural.basket:";
    private static readonly Color BasketTint = new Color(0.56f, 0.31f, 0.12f, 1f);
    private static readonly Color LargeBasketTint = new Color(0.46f, 0.24f, 0.09f, 1f);

    internal static bool IsBasket(string itemId)
    {
        return string.Equals(
                itemId,
                ResidentInventoryExpansionContent.BasketItemId.ToString(),
                StringComparison.Ordinal)
            || IsLargeBasket(itemId);
    }

    internal static bool IsLargeBasket(string itemId)
    {
        return string.Equals(
            itemId,
            ResidentInventoryExpansionContent.LargeBasketItemId.ToString(),
            StringComparison.Ordinal);
    }

    internal static DigItemVisualResolution Resolve(
        string itemId,
        DigItemVisualResolution resolution)
    {
        if (!IsBasket(itemId))
        {
            return ResolveEquipment(itemId, resolution);
        }

        bool large = IsLargeBasket(itemId);
        DigVisualAsset asset = resolution.Asset.IsFallback
            ? DigVisualAsset.CreateRuntimeFallback(
                ProceduralPrefix + itemId,
                large ? LargeBasketTint : BasketTint)
            : resolution.Asset;
        return new DigItemVisualResolution(
            asset,
            resolution.Icon,
            DigItemCarrySocketPolicy.Cargo,
            large
                ? new Vector3(0.62f, 0.48f, 0.42f)
                : new Vector3(0.49f, 0.39f, 0.34f),
            large
                ? new Vector3(0.70f, 0.62f, 0.50f)
                : new Vector3(0.56f, 0.50f, 0.42f),
            DigItemRotationPolicy.Fixed,
            DigItemColliderPolicy.InteractiveOnly,
            maxVisibleInstances: 1,
            hasProfile: true);
    }

    internal static GameObject CreateInstance(
        string itemId,
        DigItemVisualResolution resolution,
        Transform parent,
        string instanceName)
    {
        if (IsEquipment(itemId))
        {
            return CreateEquipmentInstance(
                itemId,
                resolution,
                parent,
                instanceName);
        }

        if (!IsBasket(itemId) || resolution.Asset.Prefab != null)
        {
            return DigVisualPrefabFactory.Create(
                resolution.Asset,
                parent,
                instanceName,
                PrimitiveType.Cube);
        }

        GameObject root = new GameObject(instanceName);
        root.transform.SetParent(parent, worldPositionStays: false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.AddComponent<DigVisualPrefabRoot>();
        CreateBasketParts(root.transform, IsLargeBasket(itemId));
        DigVisualTintTarget tint = root.AddComponent<DigVisualTintTarget>();
        tint.Configure(resolution.Asset.Material, resolution.Asset.Tint);
        return root;
    }

    private static void CreateBasketParts(Transform root, bool large)
    {
        float width = large ? 1.10f : 0.96f;
        float depth = large ? 0.82f : 0.72f;
        float height = large ? 0.78f : 0.68f;
        float wall = 0.12f;
        float bottom = 0.13f;
        CreatePart(root, "Basket Bottom", new Vector3(0f, bottom * 0.5f, 0f),
            new Vector3(width, bottom, depth));
        CreatePart(root, "Basket Front", new Vector3(0f, height * 0.48f, -depth * 0.44f),
            new Vector3(width, height, wall));
        CreatePart(root, "Basket Back", new Vector3(0f, height * 0.48f, depth * 0.44f),
            new Vector3(width, height, wall));
        CreatePart(root, "Basket Left", new Vector3(-width * 0.44f, height * 0.48f, 0f),
            new Vector3(wall, height, depth * 0.78f));
        CreatePart(root, "Basket Right", new Vector3(width * 0.44f, height * 0.48f, 0f),
            new Vector3(wall, height, depth * 0.78f));
        float handleY = height + 0.25f;
        CreatePart(root, "Basket Handle Left", new Vector3(-width * 0.32f, height + 0.02f, 0f),
            new Vector3(wall * 0.72f, 0.46f, wall * 0.72f));
        CreatePart(root, "Basket Handle Right", new Vector3(width * 0.32f, height + 0.02f, 0f),
            new Vector3(wall * 0.72f, 0.46f, wall * 0.72f));
        CreatePart(root, "Basket Handle Top", new Vector3(0f, handleY, 0f),
            new Vector3(width * 0.70f, wall * 0.72f, wall * 0.72f));
    }

    private static void CreatePart(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
    }
}
}
