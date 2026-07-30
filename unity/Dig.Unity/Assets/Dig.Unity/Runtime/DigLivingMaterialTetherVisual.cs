using System;
using Dig.Presentation.Buildings;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigLivingMaterialTetherVisual : MonoBehaviour
{
    private Transform? _post;
    private Transform? _hamster;
    private LineRenderer? _rope;
    private string _creatureId = string.Empty;

    internal string CreatureId => _creatureId;

    internal void Apply(
        LivingMaterialCampfireTetherViewModel model,
        BuildingWorldViewModel building,
        Material material)
    {
        if (model == null || building == null || material == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        EnsureGeometry(material);
        _creatureId = model.CreatureId;
        Vector3 campfire = DigTunnelProjection.ResidentWorldPosition(
            building.OriginX,
            building.OriginY,
            building.OriginZ) + (Vector3.up * DigTunnelProjection.ResidentFootSink);
        float side = model.SlotIndex == 0 ? -1f : 1f;
        Vector3 postPosition = campfire + new Vector3(
            side * 0.46f,
            0.13f,
            0.16f + (model.SlotIndex * 0.025f));
        Vector3 hamsterPosition = campfire + new Vector3(
            side * 0.72f,
            0.11f,
            0.13f + (model.SlotIndex * 0.025f));
        _post!.position = postPosition;
        _hamster!.position = hamsterPosition;
        _hamster.rotation = Quaternion.Euler(0f, side < 0f ? 90f : -90f, 0f);
        _rope!.SetPosition(0, postPosition + (Vector3.up * 0.10f));
        _rope.SetPosition(1, hamsterPosition + (Vector3.up * 0.07f));
        name = "Campfire tether hamster " + model.CreatureId;
    }

    private void EnsureGeometry(Material material)
    {
        if (_post != null)
        {
            return;
        }

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Tether post";
        post.transform.SetParent(transform, worldPositionStays: true);
        post.transform.localScale = new Vector3(0.055f, 0.13f, 0.055f);
        post.GetComponent<Renderer>().sharedMaterial = material;
        DisableCollider(post);
        _post = post.transform;

        GameObject hamster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hamster.name = "Tethered hamster";
        hamster.transform.SetParent(transform, worldPositionStays: true);
        hamster.transform.localScale = new Vector3(0.25f, 0.18f, 0.20f);
        hamster.GetComponent<Renderer>().sharedMaterial = material;
        DisableCollider(hamster);
        _hamster = hamster.transform;

        GameObject rope = new GameObject("Tether rope");
        rope.transform.SetParent(transform, worldPositionStays: true);
        _rope = rope.AddComponent<LineRenderer>();
        _rope.sharedMaterial = material;
        _rope.positionCount = 2;
        _rope.startWidth = 0.018f;
        _rope.endWidth = 0.014f;
        _rope.useWorldSpace = true;
        _rope.numCapVertices = 2;
    }

    private static void DisableCollider(GameObject value)
    {
        Collider collider = value.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
    }
}

}
