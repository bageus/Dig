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
            _selection = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _selection.name = "Selection Overlay";
            _selection.transform.SetParent(transform, worldPositionStays: false);
            _selection.transform.localScale = new Vector3(1.04f, 1.04f, 1.2f);
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
        MaterialPropertyBlock properties;
        properties = new MaterialPropertyBlock();
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

        // The overlay already matches the authoritative room bounds. Selection is
        // projected by the child overlay and must not resize the room hit target.
    }
}

}
