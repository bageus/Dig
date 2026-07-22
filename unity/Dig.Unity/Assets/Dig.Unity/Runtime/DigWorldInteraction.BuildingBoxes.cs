using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.Input;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigWorldItemRenderer? _itemRenderer;
        private DigBuildingBoxGhostRenderer? _buildingBoxGhostRenderer;
        private BuildingBoxPlacementModeState? _buildingPlacementMode;
        private BuildingBoxGhostViewModel? _buildingPlacementPreview;

        internal void RotateBuildingPlacement(bool clockwise)
        {
            if (!_buildingPlacementMode.HasValue || _buildingPlacementPreview == null)
            {
                return;
            }

            BuildingBoxPlacementModeState mode = clockwise
                ? _buildingPlacementMode.Value.RotateClockwise()
                : _buildingPlacementMode.Value.RotateCounterClockwise();
            UpdateBuildingPlacement(mode, _buildingPlacementPreview.Origin);
        }

        internal void CancelBuildingPlacement()
        {
            _buildingPlacementMode = null;
            _buildingPlacementPreview = null;
            _buildingBoxGhostRenderer?.Clear();
            _hud?.ClearBuildingPlacement();
            _hud?.SetStatus("Building placement cancelled.");
        }

        private void UpdateBuildingPlacementHover()
        {
            if (!_buildingPlacementMode.HasValue
                || _camera == null
                || _renderer == null
                || _hud == null
                || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)
                || !_renderer.TryGetCell(hit, out DigCellVisual cell))
            {
                return;
            }

            UpdateBuildingPlacement(
                _buildingPlacementMode.Value,
                new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z));
        }

        private void UpdateBuildingPlacement(
            BuildingBoxPlacementModeState mode,
            CellId origin)
        {
            BuildingBoxGhostViewModel preview = _terrainSession!.PreviewBuildingBoxPlacement(
                mode,
                origin);
            _buildingPlacementMode = mode;
            _buildingPlacementPreview = preview;
            _buildingBoxGhostRenderer!.Render(preview);
            _hud!.UpdateBuildingPlacement(mode, preview);
        }

        private void StartBuildingPlacement(
            ContextInputDecision decision,
            DigWorldItemVisual? item)
        {
            string? stackId = decision.TargetEntityId?.ToString() ?? item?.Model.StackId;
            if (stackId == null)
            {
                _hud!.SetStatus("input.building_box.missing_stack");
                return;
            }

            Result<BuildingBoxPlacementModeState> started =
                _terrainSession!.BeginBuildingBoxPlacement(stackId);
            if (started.IsFailure)
            {
                _hud!.SetCommandResult(Result.Failure(started.Error!));
                return;
            }

            CellId origin = decision.TargetCell
                ?? (item == null
                    ? new CellId(0, 0, 0)
                    : new CellId(item.Model.CellX, item.Model.CellY, item.Model.CellZ));
            BuildingBoxPlacementModeState mode = started.Value;
            BuildingBoxGhostViewModel preview =
                _terrainSession.PreviewBuildingBoxPlacement(mode, origin);
            _buildingPlacementMode = mode;
            _buildingPlacementPreview = preview;
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _buildingBoxGhostRenderer!.Render(preview);
            _hud!.SetBuildingPlacement(mode, preview);
            _hud!.SetStatus("Building placement active.");
        }

        private void ConfirmBuildingPlacement()
        {
            if (_buildingPlacementPreview == null)
            {
                _hud!.SetStatus("input.building_placement.missing_preview");
                return;
            }

            Result result = _terrainSession!.ConfirmBuildingBoxPlacement(
                _buildingPlacementPreview,
                _simulation!.CurrentTick);
            _hud!.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            _buildingRenderer!.Render(_terrainSession.LoadBuildings());
            _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
            var jobs = _terrainSession.LoadJobs();
            _jobRenderer!.Render(jobs);
            _hud!.SetJobs(jobs);
            CancelBuildingPlacement();
            _hud!.SetStatus("BuildingBox plan created.");
        }

        private void CreateBuildingBoxPickup(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue
                || !decision.TargetEntityId.HasValue
                || !decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.building_box.pickup_missing_target");
                return;
            }

            Result result = _terrainSession!.CreateBuildingBoxPickup(
                decision.TargetEntityId.Value.ToString(),
                decision.ActorId.Value.ToString(),
                decision.TargetCell.Value,
                _simulation!.CurrentTick);
            _hud!.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            var jobs = _terrainSession.LoadJobs();
            _jobRenderer!.Render(jobs);
            _hud!.SetJobs(jobs);
            _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
            _hud!.SetStatus("BuildingBox pickup order created.");
        }
    }
}
