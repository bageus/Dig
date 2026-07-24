using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private void ApplyMove(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue || !decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.move.missing_target");
                return;
            }

            Result result = _simulation!.MoveResident(
                decision.ActorId.Value.ToString(),
                decision.TargetCell.Value);
            _hud!.SetCommandResult(result);
        }

        private void ApplyExcavation(
            ContextInputDecision decision,
            DigCellVisual? cell)
        {
            if (!decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.excavation.missing_target");
                return;
            }

            DigCellVisual? target = cell ?? _renderer!.SelectAt(
                decision.TargetCell.Value.X,
                decision.TargetCell.Value.Y,
                decision.TargetCell.Value.Z);
            if (target == null)
            {
                _hud!.SetStatus("input.excavation.stale_target");
                return;
            }

            SelectCell(target);
            ToggleDesignation(target.Model);
        }

        private void SelectCell(DigCellVisual cell)
        {
            _selectedBuildingBox = null;
            _selectedCell = cell;
            _agentRenderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _renderer!.Select(cell);
            _hud!.SetSelection(cell.Model);
        }

        private void SelectJob(DigJobVisual job)
        {
            _selectedBuildingBox = null;
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _jobRenderer!.Select(job);
            _hud!.SetJobSelection(job.Model);
        }

        private static bool IsAdditiveResidentSelectionPressed()
        {
            return Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.RightShift);
        }

        private void ToggleResidentSelection(DigAgentVisual agent)
        {
            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            _selectedCell = null;
            _renderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _tunnelRenderer?.Select(null);
            DigAgentVisual? primary = _agentRenderer!.ToggleSelection(agent);
            _hud!.SetAgentSelection(primary?.Model, _agentRenderer.SelectedCount);
            _hud.SetStatus(_agentRenderer.SelectedCount == 0
                ? "Resident selection cleared."
                : $"{_agentRenderer.SelectedCount} resident(s) selected. " +
                    "Shift+LMB toggles the group; LMB on a destination moves everyone.");
        }

        private void CancelCurrentInteraction()
        {
            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            _selectedBuildingBox = null;
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.ClearSelection();
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _tunnelRenderer?.Select(null);
            _tunnelRenderer?.ShowRoute(null);
            _buildingBoxGhostRenderer?.Clear();
            _hud!.SetSelection(null);
            _hud.SetStatus("Current mode, action, route, and selection cancelled.");
        }

        private void HandleStoragePlacement()
        {
            bool requested = Input.GetKeyDown(KeyCode.Alpha5)
                || Input.GetKeyDown(KeyCode.Keypad5);
            if (!requested)
            {
                return;
            }

            if (_selectedCell == null)
            {
                _hud!.SetStatus("Select an open cell before placing the stockpile.");
                return;
            }

            WorldCellViewModel selected = _selectedCell.Model;
            Result result = _terrainSession!.MoveStorageZone(
                new CellId(selected.X, selected.Y, selected.Z),
                _simulation!.CurrentTick);
            if (result.IsFailure)
            {
                _hud!.SetCommandResult(result);
                return;
            }

            DigStorageStatus storage = _terrainSession.GetStorageStatus();
            _stockpileRenderer!.Render(storage);
            _hud!.SetStorageStatus(storage);
            _hud.SetStatus($"Stockpile moved to {storage.Cell.X},{storage.Cell.Y}.");
        }

        private void ToggleDesignation(WorldCellViewModel selected)
        {
            Result result = _session!.ToggleDesignation(selected);
            _hud!.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            WorldViewModel world = _session.LoadView();
            _renderer!.Render(world);
            DigCellVisual? refreshed = _renderer.SelectAt(selected.X, selected.Y, selected.Z);
            _selectedCell = refreshed;
            _hud.SetWorld(world);
            _hud.SetSelection(refreshed == null ? null : refreshed.Model);
        }
    }
}
