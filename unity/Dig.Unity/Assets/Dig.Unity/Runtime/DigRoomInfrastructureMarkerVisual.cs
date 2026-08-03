using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{

internal sealed class DigRoomInfrastructureMarkerVisual : MonoBehaviour
{
    private Renderer? _marker;
    private GameObject? _selection;

    internal RoomInfrastructureViewModel Model { get; private set; } = null!;

    internal void Initialize(
        RoomInfrastructureViewModel model,
        Material markerMaterial,
        Color markerTint,
        Material selectionMaterial)
    {
        Model = model;
        _marker = GetComponent<Renderer>();
        _marker.sharedMaterial = markerMaterial;
        ApplyTint(_marker, markerTint);
        if (_selection == null)
        {
            _selection = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _selection.name = "Selection Halo";
            _selection.transform.SetParent(transform, worldPositionStays: false);
            _selection.transform.localScale = Vector3.one * 1.55f;
            Renderer renderer = _selection.GetComponent<Renderer>();
            renderer.sharedMaterial = selectionMaterial;
            Collider collider = _selection.GetComponent<Collider>();
            collider.enabled = false;
            _selection.SetActive(false);
        }
    }

    internal void SetModel(
        RoomInfrastructureViewModel model,
        Material markerMaterial,
        Color markerTint)
    {
        Model = model;
        if (_marker != null)
        {
            _marker.sharedMaterial = markerMaterial;
            ApplyTint(_marker, markerTint);
        }
    }

    private static void ApplyTint(Renderer renderer, Color tint)
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", tint);
        properties.SetColor("_Color", tint);
        renderer.SetPropertyBlock(properties);
    }

    internal void SetSelected(bool selected)
    {
        if (_selection != null)
        {
            _selection.SetActive(selected);
        }

        transform.localScale = selected
            ? Vector3.one * 0.31f
            : Vector3.one * 0.25f;
    }
}

}
