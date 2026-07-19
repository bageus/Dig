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
    private InventoryExpansionGroup _group;
    private int _tier;

    internal void Configure(
        ResidentInventoryAttachmentViewModel model,
        Material material)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (material == null)
        {
            throw new ArgumentNullException(nameof(material));
        }

        if (string.Equals(
                _visualAttachmentId,
                model.VisualAttachmentId,
                StringComparison.Ordinal)
            && _group == model.Group
            && _tier == model.Tier
            && transform.childCount > 0)
        {
            return;
        }

        Clear();
        _visualAttachmentId = model.VisualAttachmentId;
        _group = model.Group;
        _tier = model.Tier;
        name = "Inventory Attachment " + model.VisualAttachmentId;
        if (model.Group == InventoryExpansionGroup.Cargo)
        {
            BuildCargo(model.Tier, material);
        }
        else
        {
            BuildWeapon(model.Tier, material);
        }
    }

    internal void Clear()
    {
        _visualAttachmentId = null;
        _tier = 0;
        for (int index = transform.childCount - 1; index >= 0; index--)
        {
            Destroy(transform.GetChild(index).gameObject);
        }
    }

    private void BuildCargo(int tier, Material material)
    {
        float width = tier >= 2 ? 0.72f : 0.56f;
        float height = tier >= 2 ? 0.52f : 0.42f;
        transform.localPosition = new Vector3(0f, 0.04f, -0.42f);
        transform.localRotation = Quaternion.identity;
        CreatePart(
            "Basket Back",
            new Vector3(0f, 0f, 0f),
            new Vector3(width, height, 0.12f),
            material);
        CreatePart(
            "Basket Rim",
            new Vector3(0f, height * 0.5f, -0.06f),
            new Vector3(width + 0.08f, 0.08f, 0.22f),
            material);
        CreatePart(
            "Basket Left Strap",
            new Vector3(-width * 0.32f, height * 0.38f, 0.12f),
            new Vector3(0.07f, 0.62f, 0.07f),
            material);
        CreatePart(
            "Basket Right Strap",
            new Vector3(width * 0.32f, height * 0.38f, 0.12f),
            new Vector3(0.07f, 0.62f, 0.07f),
            material);
    }

    private void BuildWeapon(int tier, Material material)
    {
        transform.localPosition = new Vector3(-0.43f, 0.03f, 0.02f);
        transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        CreatePart(
            "Harness Strap",
            Vector3.zero,
            new Vector3(0.10f, 0.84f, 0.10f),
            material);
        int pockets = tier >= 2 ? 2 : 1;
        for (int index = 0; index < pockets; index++)
        {
            CreatePart(
                "Weapon Pocket " + index,
                new Vector3(0.11f, -0.16f + (index * 0.30f), 0f),
                new Vector3(0.20f, 0.28f, 0.16f),
                material);
        }
    }

    private void CreatePart(
        string partName,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.layer = 2;
        part.transform.SetParent(transform, worldPositionStays: false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }
}

}
