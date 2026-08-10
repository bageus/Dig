using System;
using Dig.Domain.Content;
using UnityEngine;

namespace Dig.Unity
{

internal static partial class DigBasketVisualPolicy
{
    private const string EquipmentProceduralPrefix = "procedural.equipment:";
    private static readonly Color SheathTint = new Color(0.25f, 0.11f, 0.045f, 1f);
    private static readonly Color HarnessTint = new Color(0.18f, 0.075f, 0.03f, 1f);
    private static readonly Color ClubTint = new Color(0.44f, 0.24f, 0.075f, 1f);

    internal static bool IsEquipment(string itemId)
    {
        return IsSheath(itemId) || IsHarness(itemId) || IsClub(itemId);
    }

    private static DigItemVisualResolution ResolveEquipment(
        string itemId,
        DigItemVisualResolution resolution)
    {
        if (!IsEquipment(itemId))
        {
            return resolution;
        }

        DigVisualAsset asset = resolution.Asset.IsFallback
            ? DigVisualAsset.CreateRuntimeFallback(
                EquipmentProceduralPrefix + itemId,
                ResolveEquipmentTint(itemId))
            : resolution.Asset;
        return new DigItemVisualResolution(
            asset,
            resolution.Icon,
            IsClub(itemId)
                ? DigItemCarrySocketPolicy.Weapon
                : DigItemCarrySocketPolicy.Back,
            ResolveEquipmentWorldScale(itemId),
            ResolveEquipmentCarryScale(itemId),
            DigItemRotationPolicy.Fixed,
            DigItemColliderPolicy.InteractiveOnly,
            maxVisibleInstances: 1,
            hasProfile: true);
    }

    private static GameObject CreateEquipmentInstance(
        string itemId,
        DigItemVisualResolution resolution,
        Transform parent,
        string instanceName)
    {
        if (resolution.Asset.Prefab != null)
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
        if (IsSheath(itemId))
        {
            CreateSheath(root.transform);
        }
        else if (IsHarness(itemId))
        {
            CreateHarness(root.transform);
        }
        else
        {
            CreateClub(root.transform);
        }

        DigVisualTintTarget tint = root.AddComponent<DigVisualTintTarget>();
        tint.Configure(resolution.Asset.Material, resolution.Asset.Tint);
        return root;
    }

    private static bool IsSheath(string itemId)
    {
        return string.Equals(
            itemId,
            ResidentInventoryExpansionContent.SheathItemId.ToString(),
            StringComparison.Ordinal);
    }

    private static bool IsHarness(string itemId)
    {
        return string.Equals(
            itemId,
            ResidentInventoryExpansionContent.WeaponHarnessItemId.ToString(),
            StringComparison.Ordinal);
    }

    private static bool IsClub(string itemId)
    {
        return string.Equals(
            itemId,
            CombatEquipmentContent.ClubItemId.ToString(),
            StringComparison.Ordinal);
    }

    private static Color ResolveEquipmentTint(string itemId)
    {
        return IsSheath(itemId)
            ? SheathTint
            : IsHarness(itemId) ? HarnessTint : ClubTint;
    }

    private static Vector3 ResolveEquipmentWorldScale(string itemId)
    {
        return IsSheath(itemId)
            ? new Vector3(0.34f, 0.58f, 0.24f)
            : IsHarness(itemId)
                ? new Vector3(0.52f, 0.46f, 0.20f)
                : new Vector3(0.30f, 0.68f, 0.30f);
    }

    private static Vector3 ResolveEquipmentCarryScale(string itemId)
    {
        return IsSheath(itemId)
            ? new Vector3(0.42f, 0.72f, 0.24f)
            : IsHarness(itemId)
                ? new Vector3(0.72f, 0.62f, 0.22f)
                : new Vector3(0.27f, 0.76f, 0.27f);
    }

    private static void CreateSheath(Transform root)
    {
        CreateEquipmentPart(root, "Sheath Body", PrimitiveType.Cube,
            new Vector3(0f, 0.48f, 0f),
            new Vector3(0.24f, 0.96f, 0.18f),
            new Vector3(0f, 0f, -12f));
        CreateEquipmentPart(root, "Sheath Mouth", PrimitiveType.Cube,
            new Vector3(-0.10f, 0.92f, 0f),
            new Vector3(0.38f, 0.12f, 0.24f),
            new Vector3(0f, 0f, -12f));
        CreateEquipmentPart(root, "Sheath Strap", PrimitiveType.Cube,
            new Vector3(0.16f, 0.63f, 0f),
            new Vector3(0.12f, 0.60f, 0.12f),
            new Vector3(0f, 0f, 25f));
    }

    private static void CreateHarness(Transform root)
    {
        CreateEquipmentPart(root, "Weapon Harness Belt", PrimitiveType.Cube,
            new Vector3(0f, 0.18f, 0f),
            new Vector3(0.96f, 0.16f, 0.18f),
            Vector3.zero);
        CreateEquipmentPart(root, "Weapon Harness Left Strap", PrimitiveType.Cube,
            new Vector3(-0.18f, 0.55f, 0f),
            new Vector3(0.14f, 0.86f, 0.16f),
            new Vector3(0f, 0f, -27f));
        CreateEquipmentPart(root, "Weapon Harness Right Strap", PrimitiveType.Cube,
            new Vector3(0.18f, 0.55f, 0f),
            new Vector3(0.14f, 0.86f, 0.16f),
            new Vector3(0f, 0f, 27f));
        CreateEquipmentPart(root, "Weapon Harness Buckle", PrimitiveType.Cube,
            new Vector3(0f, 0.50f, -0.08f),
            new Vector3(0.24f, 0.20f, 0.10f),
            Vector3.zero);
    }

    private static void CreateClub(Transform root)
    {
        CreateEquipmentPart(root, "Club Handle", PrimitiveType.Cylinder,
            new Vector3(0f, 0.38f, 0f),
            new Vector3(0.15f, 0.48f, 0.15f),
            new Vector3(0f, 0f, -8f));
        CreateEquipmentPart(root, "Club Head", PrimitiveType.Sphere,
            new Vector3(-0.10f, 0.92f, 0f),
            new Vector3(0.52f, 0.62f, 0.46f),
            new Vector3(0f, 0f, -8f));
    }

    private static void CreateEquipmentPart(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 scale,
        Vector3 rotation)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = Quaternion.Euler(rotation);
    }
}

}
