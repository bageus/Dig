using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed partial class DigRoomInfrastructureRenderer : MonoBehaviour
{
    private readonly Dictionary<string, DigRoomInfrastructureMarkerVisual> _markers =
        new Dictionary<string, DigRoomInfrastructureMarkerVisual>(StringComparer.Ordinal);
    private readonly Dictionary<string, GameObject> _progress =
        new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private Transform? _root;
    private DigRoomInfrastructureMarkerVisual? _selected;
    private DigRenderMaterialLibrary? _materials;
    private bool _planningOverlaysVisible = true;

    internal string? SelectedRoomId => _selected?.Model.Id;
    internal RoomInfrastructureViewModel? SelectedModel => _selected?.Model;
    internal int MarkerCount => _markers.Count;
    internal int ProgressPieceCount => _progress.Count;
    internal bool PlanningOverlaysVisible => _planningOverlaysVisible;

    internal void Render(IReadOnlyList<RoomInfrastructureViewModel> rooms)
    {
        if (rooms == null)
        {
            throw new ArgumentNullException(nameof(rooms));
        }

        EnsureRoot();
        HashSet<string> visibleRooms = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> visibleProgress = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < rooms.Count; index++)
        {
            RoomInfrastructureViewModel room = rooms[index];
            visibleRooms.Add(room.Id);
            RenderMarker(room);
            RenderProgress(room, visibleProgress);
        }

        RemoveMissingMarkers(visibleRooms);
        RemoveMissingProgress(visibleProgress);
    }

    internal void SetPlanningOverlayVisibility(bool visible)
    {
        _planningOverlaysVisible = visible;
        foreach (DigRoomInfrastructureMarkerVisual marker in _markers.Values)
        {
            marker.gameObject.SetActive(visible);
        }
    }

    internal bool TryGetMarker(
        RaycastHit hit,
        out DigRoomInfrastructureMarkerVisual marker)
    {
        marker = !_planningOverlaysVisible || hit.collider == null
            ? null!
            : hit.collider.GetComponentInParent<DigRoomInfrastructureMarkerVisual>();
        return marker != null;
    }

    internal DigRoomInfrastructureMarkerVisual? Select(
        DigRoomInfrastructureMarkerVisual? marker)
    {
        if (_selected != null)
        {
            _selected.SetSelected(false);
        }

        _selected = marker;
        if (_selected != null)
        {
            _selected.SetSelected(true);
        }

        return _selected;
    }

    internal DigRoomInfrastructureMarkerVisual? SelectById(string id)
    {
        return !string.IsNullOrWhiteSpace(id)
            && _markers.TryGetValue(id, out DigRoomInfrastructureMarkerVisual? marker)
                ? Select(marker)
                : Select(null);
    }

    private void RenderMarker(RoomInfrastructureViewModel room)
    {
        Material markerMaterial = ResolveMarkerBaseMaterial();
        Color markerTint = ResolveMarkerTint(room);
        if (_markers.TryGetValue(
                room.Id,
                out DigRoomInfrastructureMarkerVisual? marker))
        {
            marker.SetModel(room, markerMaterial, markerTint);
            marker.transform.position = MarkerPosition(room);
            marker.gameObject.SetActive(_planningOverlaysVisible);
            return;
        }

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Room Upgrade Overlay " + room.Id;
        root.transform.SetParent(_root, worldPositionStays: true);
        root.transform.position = MarkerPosition(room);
        root.transform.localScale = new Vector3(
            room.MaxX - room.MinX + 0.9f,
            room.MaxY - room.MinY + 0.9f,
            0.08f);
        DigRoomInfrastructureMarkerVisual visual =
            root.AddComponent<DigRoomInfrastructureMarkerVisual>();
        visual.Initialize(
            room,
            markerMaterial,
            markerTint,
            ResolveSelectionMaterial());
        visual.SetSelected(false);
        root.SetActive(_planningOverlaysVisible);
        _markers.Add(room.Id, visual);
    }

    private void RemoveMissingMarkers(HashSet<string> visible)
    {
        string[] removed = new List<string>(_markers.Keys)
            .FindAll(id => !visible.Contains(id))
            .ToArray();
        for (int index = 0; index < removed.Length; index++)
        {
            DigRoomInfrastructureMarkerVisual marker = _markers[removed[index]];
            if (ReferenceEquals(marker, _selected))
            {
                _selected = null;
            }

            _markers.Remove(removed[index]);
            Destroy(marker.gameObject);
        }
    }

    private void RemoveMissingProgress(HashSet<string> visible)
    {
        string[] removed = new List<string>(_progress.Keys)
            .FindAll(id => !visible.Contains(id))
            .ToArray();
        for (int index = 0; index < removed.Length; index++)
        {
            GameObject visual = _progress[removed[index]];
            _progress.Remove(removed[index]);
            Destroy(visual);
        }
    }

    private void EnsureRoot()
    {
        if (_root != null)
        {
            return;
        }

        GameObject root = new GameObject("Room Infrastructure Visuals");
        root.transform.SetParent(transform, worldPositionStays: false);
        _root = root.transform;
        _materials = GetComponent<DigRenderMaterialLibrary>()
            ?? gameObject.AddComponent<DigRenderMaterialLibrary>();
    }

    private static Vector3 MarkerPosition(RoomInfrastructureViewModel room)
    {
        float depth = DigTunnelProjection.CellWorldPosition(
            new Dig.Domain.World.CellId(0, room.MarkerY, room.MarkerZ)).z;
        return new Vector3(
            room.MarkerX,
            -((room.MinY + room.MaxY) * 0.5f),
            depth - 0.08f);
    }
}

}
