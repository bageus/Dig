using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigBuildingInternalStockBayVisual : MonoBehaviour
{
    internal void Initialize(Material material)
    {
        if (material == null)
        {
            throw new ArgumentNullException(nameof(material));
        }

        if (transform.childCount != 0)
        {
            return;
        }

        CreatePart(
            "Storage tray",
            new Vector3(0f, 0.025f, 0f),
            new Vector3(0.82f, 0.05f, 0.38f),
            material);
    }

    internal void SetPosition(Vector3 position)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
    }

    private void CreatePart(
        string partName,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(transform, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
    }
}

}
