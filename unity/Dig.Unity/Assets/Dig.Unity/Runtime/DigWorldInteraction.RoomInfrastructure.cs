using System;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private DigRoomInfrastructureRenderer? _roomInfrastructureRenderer;

    internal RoomInfrastructureViewModel? SelectedRoomInfrastructure =>
        _roomInfrastructureRenderer?.SelectedModel;

    internal void SetRoomInfrastructureRenderer(
        DigRoomInfrastructureRenderer renderer)
    {
        _roomInfrastructureRenderer = renderer
            ?? throw new ArgumentNullException(nameof(renderer));
    }

    private bool TryHandleRoomInfrastructureMarker(
        RaycastHit[] hits,
        bool leftButton)
    {
        if (!leftButton || _roomInfrastructureRenderer == null)
        {
            return false;
        }

        for (int index = 0; index < hits.Length; index++)
        {
            if (!_roomInfrastructureRenderer.TryGetMarker(
                    hits[index],
                    out DigRoomInfrastructureMarkerVisual marker))
            {
                continue;
            }

            CancelInventoryItemPlacement();
            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ClearBuildingBoxSelection();
            ClearSelectedInventoryStack();
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.ClearSelection();
            _creatureRenderer!.ClearSelection();
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _tunnelRenderer?.Select(null);
            _roomInfrastructureRenderer.Select(marker);
            _hud!.SetAgentSelection(null, 0);
            _hud.SetBuildingSelection(null);
            _hud.SetJobSelection(null);
            _hud.SetStatus(
                $"Selected {marker.Model.TemplateKind} room · {marker.Model.Status}.");
            return true;
        }

        return false;
    }

    private void ClearRoomInfrastructureSelection()
    {
        _roomInfrastructureRenderer?.Select(null);
    }
}

}
