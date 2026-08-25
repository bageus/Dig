using System;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private DigRoomInfrastructureRenderer? _roomInfrastructureRenderer;
    private bool _roomUpgradeMode;

    internal bool RoomUpgradeMode => _roomUpgradeMode;

    internal bool IsRoomUpgradeModeUnlocked =>
        _terrainSession?.IsRoomUpgradeModeUnlocked == true;

    internal RoomInfrastructureViewModel? SelectedRoomInfrastructure =>
        _roomInfrastructureRenderer?.SelectedModel;

    internal bool IsRoomPlanningOverlayVisible =>
        _roomUpgradeMode
        &&
        _agentRenderer != null
        && _buildingRenderer != null
        && _jobRenderer != null
        && _agentRenderer.SelectedCount == 0
        && _buildingRenderer.SelectedBuildingId == null
        && _jobRenderer.SelectedJobId == null
        && !_buildingPlacementMode.HasValue
        && SelectedBuildingBox == null;

    internal void SetRoomInfrastructureRenderer(
        DigRoomInfrastructureRenderer renderer)
    {
        _roomInfrastructureRenderer = renderer
            ?? throw new ArgumentNullException(nameof(renderer));
        _roomInfrastructureRenderer.SetPlanningOverlayVisibility(
            IsRoomPlanningOverlayVisible);
    }

    internal void SetRoomUpgradeMode(bool enabled)
    {
        if (enabled && !IsRoomUpgradeModeUnlocked)
        {
            enabled = false;
        }

        _roomUpgradeMode = enabled;
        if (!enabled)
        {
            ClearRoomInfrastructureSelection();
        }

        _roomInfrastructureRenderer?.SetPlanningOverlayVisibility(
            IsRoomPlanningOverlayVisible);
    }

    internal void OpenExcavationMenuInDigMode()
    {
        if (_roomUpgradeMode)
        {
            SetRoomUpgradeMode(false);
        }
    }

    private bool TryHandleRoomInfrastructureMarker(
        RaycastHit[] hits,
        bool leftButton)
    {
        if (!leftButton
            || !IsRoomPlanningOverlayVisible
            || _roomInfrastructureRenderer == null)
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
