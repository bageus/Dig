using System;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigWorldInteraction
{
    private DigRoomPurposeMarkerRenderer? _roomPurposeMarkerRenderer;
    private string? _selectedRoomPurposeId;

    internal RoomPurposeViewModel? SelectedRoomPurpose =>
        _selectedRoomPurposeId == null
            ? null
            : _terrainSession?.LoadRoomPurpose(_selectedRoomPurposeId);

    internal Result ChangeSelectedRoomPurpose(RoomPurposeKind purpose)
    {
        if (_selectedRoomPurposeId == null || _terrainSession == null || _simulation == null)
        {
            return Result.Failure(RoomPurposeErrors.RoomNotFound);
        }

        Result result = _terrainSession.ChangeRoomPurpose(
            _selectedRoomPurposeId,
            purpose,
            _simulation.CurrentTick);
        if (result.IsSuccess)
        {
            RefreshRoomPurposeMarkers();
            _hud?.InvalidateContext();
        }

        return result;
    }

    private bool TryHandleRoomPurposeMarker(
        RaycastHit[] hits,
        bool leftButton)
    {
        if (!leftButton || _roomPurposeMarkerRenderer == null)
        {
            return false;
        }

        for (int index = 0; index < hits.Length; index++)
        {
            if (!_roomPurposeMarkerRenderer.TryGetRoom(hits[index], out RoomPurposeViewModel room))
            {
                continue;
            }

            CancelInventoryItemPlacement();
            CancelTunnelReinforcementPlacement();
            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ClearBuildingBoxSelection();
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.ClearSelection();
            _creatureRenderer!.ClearSelection();
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _selectedRoomPurposeId = room.RoomId;
            _roomPurposeMarkerRenderer.Select(room.RoomId);
            _hud!.InvalidateContext();
            _hud.SetStatus($"Room {room.TemplateId} selected.");
            return true;
        }

        return false;
    }

    private void RefreshRoomPurposeMarkers()
    {
        if (_terrainSession == null || _roomPurposeMarkerRenderer == null)
        {
            return;
        }

        _roomPurposeMarkerRenderer.Render(_terrainSession.LoadRoomPurposes());
        _roomPurposeMarkerRenderer.Select(_selectedRoomPurposeId);
    }

    private void ClearRoomPurposeSelection()
    {
        _selectedRoomPurposeId = null;
        _roomPurposeMarkerRenderer?.Select(null);
    }
}
}
