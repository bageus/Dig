using System;
using System.Collections.Generic;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
internal sealed class DigRoomPurposeMarkerRenderer : MonoBehaviour
{
    private readonly Dictionary<string, DigRoomPurposeMarkerVisual> _markers =
        new Dictionary<string, DigRoomPurposeMarkerVisual>(StringComparer.Ordinal);
    private Transform? _root;
    private Material? _material;

    internal void Render(IReadOnlyList<RoomPurposeViewModel> rooms)
    {
        if (rooms == null)
        {
            throw new ArgumentNullException(nameof(rooms));
        }

        EnsureResources();
        HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < rooms.Count; index++)
        {
            RoomPurposeViewModel room = rooms[index];
            visible.Add(room.RoomId);
            if (!_markers.TryGetValue(room.RoomId, out DigRoomPurposeMarkerVisual? marker))
            {
                GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instance.name = $"Room purpose {room.RoomId}";
                instance.transform.SetParent(_root, worldPositionStays: false);
                instance.transform.localScale = Vector3.one * 0.24f;
                instance.GetComponent<MeshRenderer>().sharedMaterial = _material;
                marker = instance.AddComponent<DigRoomPurposeMarkerVisual>();
                _markers.Add(room.RoomId, marker);
            }

            marker.Apply(room);
        }

        foreach (string roomId in new List<string>(_markers.Keys))
        {
            if (visible.Contains(roomId))
            {
                continue;
            }

            Destroy(_markers[roomId].gameObject);
            _markers.Remove(roomId);
        }
    }

    internal bool TryGetRoom(RaycastHit hit, out RoomPurposeViewModel room)
    {
        DigRoomPurposeMarkerVisual? marker = hit.collider == null
            ? null
            : hit.collider.GetComponent<DigRoomPurposeMarkerVisual>();
        if (marker != null)
        {
            room = marker.Model;
            return true;
        }

        room = null!;
        return false;
    }

    internal void Select(string? roomId)
    {
        foreach (KeyValuePair<string, DigRoomPurposeMarkerVisual> pair in _markers)
        {
            pair.Value.SetSelected(string.Equals(
                pair.Key,
                roomId,
                StringComparison.Ordinal));
        }
    }

    private void EnsureResources()
    {
        if (_root == null)
        {
            _root = new GameObject("Room Purpose Markers").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }

        if (_material == null)
        {
            Shader shader = Shader.Find("Dig/Stylized Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard")
                ?? throw new InvalidOperationException("Room marker shader unavailable.");
            _material = new Material(shader)
            {
                name = "Room purpose marker material",
                color = new Color(0.95f, 0.72f, 0.22f, 1f),
            };
        }
    }
}

internal sealed class DigRoomPurposeMarkerVisual : MonoBehaviour
{
    private Vector3 _baseScale;

    internal RoomPurposeViewModel Model { get; private set; } = null!;

    internal void Apply(RoomPurposeViewModel model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        transform.position = new Vector3(
            model.CenterX,
            DigTunnelProjection.WalkSurfaceY(model.TopY) + 1.1f,
            DigTunnelProjection.DepthOrigin
                + (model.FrontZ * DigTunnelProjection.DepthSpacing));
        _baseScale = Vector3.one * 0.24f;
        transform.localScale = _baseScale;
    }

    internal void SetSelected(bool selected)
    {
        transform.localScale = selected ? _baseScale * 1.35f : _baseScale;
    }
}
}
