using System;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigResidentInventoryAttachmentVisual : MonoBehaviour
{
    private string? _visualAttachmentId;
    private string _assetKey = string.Empty;
    private InventoryExpansionGroup _group;
    private int _tier;
    private GameObject? _instance;

    internal void InvalidateAsset()
    {
        _assetKey = string.Empty;
    }

    internal void Configure(
        ResidentInventoryAttachmentViewModel model,
        DigItemVisualResolution resolution)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        bool rebuild = _instance == null
            || !string.Equals(_assetKey, resolution.Asset.StableId, StringComparison.Ordinal)
            || !string.Equals(
                _visualAttachmentId,
                model.VisualAttachmentId,
                StringComparison.Ordinal)
            || _group != model.Group
            || _tier != model.Tier;
        _visualAttachmentId = model.VisualAttachmentId;
        _group = model.Group;
        _tier = model.Tier;
        name = "Inventory Attachment " + model.VisualAttachmentId;
        if (rebuild)
        {
            Rebuild(model, resolution);
        }
        else
        {
            ApplySocket(model, resolution);
        }
    }

    internal void Clear()
    {
        _visualAttachmentId = null;
        _assetKey = string.Empty;
        _tier = 0;
        if (_instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }
    }

    private void Rebuild(
        ResidentInventoryAttachmentViewModel model,
        DigItemVisualResolution resolution)
    {
        if (_instance != null)
        {
            Destroy(_instance);
        }

        _instance = DigVisualPrefabFactory.Create(
            resolution.Asset,
            transform,
            model.VisualAttachmentId,
            PrimitiveType.Cube);
        _assetKey = resolution.Asset.StableId;
        SetLayerRecursively(_instance, layer: 2);
        DisableColliders(_instance);
        ApplySocket(model, resolution);
    }

    private void ApplySocket(
        ResidentInventoryAttachmentViewModel model,
        DigItemVisualResolution resolution)
    {
        if (_instance == null)
        {
            return;
        }

        DigItemCarrySocketPolicy socket = ResolveSocket(model.Group, resolution.CarrySocket);
        float tierScale = model.Tier >= 2 ? 1.14f : 1f;
        transform.localPosition = socket switch
        {
            DigItemCarrySocketPolicy.Hand => new Vector3(0.38f, 0.18f, -0.04f),
            DigItemCarrySocketPolicy.Weapon => new Vector3(-0.43f, 0.03f, 0.02f),
            DigItemCarrySocketPolicy.Back => new Vector3(0f, 0.16f, -0.44f),
            _ => new Vector3(0f, 0.04f, -0.42f),
        };
        transform.localRotation = socket switch
        {
            DigItemCarrySocketPolicy.Hand => Quaternion.Euler(0f, 0f, -18f),
            DigItemCarrySocketPolicy.Weapon => Quaternion.Euler(0f, 0f, 8f),
            _ => Quaternion.identity,
        };
        transform.localScale = Vector3.one;
        _instance.transform.localPosition = Vector3.zero;
        _instance.transform.localRotation = Quaternion.identity;
        _instance.transform.localScale = resolution.CarryScale * tierScale;
    }

    private static DigItemCarrySocketPolicy ResolveSocket(
        InventoryExpansionGroup group,
        DigItemCarrySocketPolicy configured)
    {
        if (configured != DigItemCarrySocketPolicy.None)
        {
            return configured;
        }

        return group == InventoryExpansionGroup.Weapon
            ? DigItemCarrySocketPolicy.Weapon
            : DigItemCarrySocketPolicy.Cargo;
    }

    private static void DisableColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
        for (int index = 0; index < colliders.Length; index++)
        {
            colliders[index].enabled = false;
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        Transform[] values = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int index = 0; index < values.Length; index++)
        {
            values[index].gameObject.layer = layer;
        }
    }
}
}
