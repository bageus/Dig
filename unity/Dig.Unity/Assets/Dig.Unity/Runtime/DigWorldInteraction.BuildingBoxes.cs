using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigWorldItemRenderer? _itemRenderer;
        private DigBuildingBoxGhostRenderer? _buildingBoxGhostRenderer;
        private BuildingBoxPlacementModeState? _buildingPlacementMode;
        private BuildingBoxGhostViewModel? _buildingPlacementPreview;
        private bool _buildingPlacementOwnsCursor;
        private bool _buildingPlacementPreviousCursorVisible;

        internal string? ActiveBuildingPlacementStackId =>
            _buildingPlacementMode?.SourceStackId.ToString();

        private void RefreshBuildingBoxRelocationPlans()
        {
            if (_terrainSession != null && _buildingBoxGhostRenderer != null)
            {
                _buildingBoxGhostRenderer.RenderPlans(
                    _terrainSession.LoadBuildingBoxRelocationPlans());
            }
        }

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
            ExitBuildingPlacement(clearGhost: true);
            _hud?.SetStatus("Building placement cancelled.");
        }

        private void ExitBuildingPlacement(bool clearGhost)
        {
            _buildingPlacementMode = null;
            _buildingPlacementPreview = null;
            if (clearGhost)
            {
                _buildingBoxGhostRenderer?.Clear();
            }

            _hud?.ClearBuildingPlacement();
            RestoreBuildingPlacementCursor();
        }

        private bool TryHandleBuildingPlacementClick()
        {
            if (!_buildingPlacementMode.HasValue
                || !Input.GetMouseButtonDown(0))
            {
                return false;
            }

            if (_hud == null || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                return true;
            }

            BuildingBoxGhostViewModel? visiblePreview = _buildingPlacementPreview;
            if (visiblePreview == null)
            {
                _hud.SetStatus("input.building_placement.missing_preview");
                return true;
            }

            ContextInputDecision decision = _inputRouter.Route(
                new ContextPointerEvent(
                    PointerInputSurface.World,
                    PointerButtonKind.Left,
                    altPressed: IsAltPressed()),
                BuildState(PointerButtonKind.Left),
                new ContextPointerTarget(
                    ContextWorldTargetKind.Ground,
                    cell: visiblePreview.Origin,
                    reachable: visiblePreview.IsValid));
            ApplyDecision(decision);
            return true;
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

            CellId origin;
            if (!TryResolveBuildingPlacementOrigin(GetPointerHits(), out origin))
            {
                int currentLayer = _buildingPlacementPreview?.Origin.Z ?? 0;
                CellId? projected = ProjectPointerToLayer(currentLayer);
                if (!projected.HasValue)
                {
                    return;
                }

                origin = projected.Value;
            }

            if (_buildingPlacementPreview != null
                && _buildingPlacementPreview.Origin == origin)
            {
                return;
            }

            UpdateBuildingPlacement(_buildingPlacementMode.Value, origin);
        }

        private void UpdateBuildingPlacement(
            BuildingBoxPlacementModeState mode,
            CellId origin)
        {
            BuildingBoxGhostViewModel preview = _terrainSession!
                .PreviewBuildingBoxPlacement(
                    mode,
                    origin,
                    _agentSession!.LoadView());
            _buildingPlacementMode = mode;
            _buildingPlacementPreview = preview;
            _buildingBoxGhostRenderer!.Render(preview);
            _hud!.UpdateBuildingPlacement(mode, preview);
        }

        private void StartBuildingPlacement(
            ContextInputDecision decision,
            DigWorldItemVisual? item)
        {
            if (item != null)
            {
                SelectBuildingBox(item.Model, item);
                BeginBuildingPlacement(
                    item.Model.StackId,
                    new CellId(
                        item.Model.CellX,
                        item.Model.CellY,
                        item.Model.CellZ));
                return;
            }

            string? stackId = decision.TargetEntityId?.ToString();
            if (stackId == null)
            {
                _hud!.SetStatus("input.building_box.missing_stack");
                return;
            }

            BeginBuildingPlacement(
                stackId,
                decision.TargetCell ?? new CellId(0, 0, 0));
        }

        private void BeginBuildingPlacement(string stackId, CellId origin)
        {
            Result<BuildingBoxPlacementModeState> started =
                _terrainSession!.BeginBuildingBoxPlacement(stackId);
            if (started.IsFailure)
            {
                _hud!.SetCommandResult(Result.Failure(started.Error!));
                return;
            }

            if (TryResolveBuildingPlacementOrigin(
                GetPointerHits(),
                out CellId hoveredOrigin))
            {
                origin = hoveredOrigin;
            }
            else
            {
                CellId? projected = ProjectPointerToLayer(origin.Z);
                if (projected.HasValue)
                {
                    origin = projected.Value;
                }
            }

            BuildingBoxPlacementModeState mode = started.Value;
            BuildingBoxGhostViewModel preview =
                _terrainSession.PreviewBuildingBoxPlacement(
                    mode,
                    origin,
                    _agentSession!.LoadView());
            _buildingPlacementMode = mode;
            _buildingPlacementPreview = preview;
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            HideSystemCursorForBuildingPlacement();
            _buildingBoxGhostRenderer!.Render(preview);
            _hud!.SetBuildingPlacement(mode, preview);
            _hud.SetStatus(preview.PlacementKind == BuildingBoxPlacementKind.RelocateBox
                ? "BuildingBox relocation active on Z0."
                : "Building unpack placement active.");
        }

        private void ConfirmBuildingPlacement()
        {
            if (_buildingPlacementPreview == null || !_buildingPlacementMode.HasValue)
            {
                _hud!.SetStatus("input.building_placement.missing_preview");
                return;
            }

            _buildingPlacementPreview = _terrainSession!.PreviewBuildingBoxPlacement(
                _buildingPlacementMode.Value,
                _buildingPlacementPreview.Origin,
                _agentSession!.LoadView());
            _buildingBoxGhostRenderer!.Render(_buildingPlacementPreview);
            _hud!.UpdateBuildingPlacement(_buildingPlacementMode.Value, _buildingPlacementPreview);
            if (!_buildingPlacementPreview.IsValid)
            {
                _hud.SetStatus(
                    _buildingPlacementPreview.ReasonCode
                    ?? "input.building_placement.invalid");
                return;
            }

            BuildingBoxPlacementKind kind = _buildingPlacementPreview.PlacementKind;
            string sourceStackId = _buildingPlacementMode.Value.SourceStackId.ToString();
            Result result = _terrainSession!.ConfirmBuildingBoxPlacement(
                _buildingPlacementPreview,
                _simulation!.CurrentTick,
                _agentSession!.LoadView());
            _hud!.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            _buildingRenderer!.Render(_terrainSession.LoadBuildings());
            RenderCurrentlyVisibleWorldItems();
            var jobs = _terrainSession.LoadJobs();
            _jobRenderer!.Render(jobs);
            _hud.SetJobs(jobs);
            WorldItemViewModel? source = _terrainSession.LoadAllWorldItems()
                .FirstOrDefault(value => value.IsBuildingBox
                    && string.Equals(
                        value.StackId,
                        sourceStackId,
                        StringComparison.Ordinal));
            if (source != null)
            {
                SelectBuildingBox(source);
            }
            else
            {
                ClearBuildingBoxSelection();
            }

            ExitBuildingPlacement(clearGhost: true);
            RefreshBuildingBoxRelocationPlans();
            _hud.SetStatus(kind == BuildingBoxPlacementKind.RelocateBox
                ? "BuildingBox relocation job created."
                : "BuildingBox assembly plan created.");
        }

        private void HideSystemCursorForBuildingPlacement()
        {
            if (_buildingPlacementOwnsCursor)
            {
                return;
            }

            _buildingPlacementPreviousCursorVisible = Cursor.visible;
            Cursor.visible = false;
            _buildingPlacementOwnsCursor = true;
        }

        private void RestoreBuildingPlacementCursor()
        {
            if (!_buildingPlacementOwnsCursor)
            {
                return;
            }

            Cursor.visible = _buildingPlacementPreviousCursorVisible;
            _buildingPlacementOwnsCursor = false;
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

            ClearBuildingBoxSelection();
            var jobs = _terrainSession.LoadJobs();
            _jobRenderer!.Render(jobs);
            _hud.SetJobs(jobs);
            RenderCurrentlyVisibleWorldItems();
            _hud.SetStatus("BuildingBox pickup order created.");
        }
    }
}
