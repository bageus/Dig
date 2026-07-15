using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private void ApplyDecision(
            ContextInputDecision decision,
            DigAgentVisual? agent = null,
            DigCellVisual? cell = null,
            DigBuildingVisual? building = null)
        {
            ApplyEffects(decision, agent, cell, building);
            if (!decision.HasApplicationCommand)
            {
                return;
            }

            switch (decision.CommandKind)
            {
                case ApplicationInputCommandKind.MoveResident:
                    ApplyMove(decision);
                    break;
                case ApplicationInputCommandKind.ApplyExcavation:
                    ApplyExcavation(decision, cell);
                    break;
                default:
                    _hud!.SetStatus(
                        $"Input command '{decision.CommandKind}' is not wired in this demo slice.");
                    break;
            }
        }

        private void ApplyEffects(
            ContextInputDecision decision,
            DigAgentVisual? agent,
            DigCellVisual? cell,
            DigBuildingVisual? building)
        {
            if (decision.Effects.HasFlag(PresentationInputEffect.DeselectResident))
            {
                _agentRenderer!.Select(null);
                _hud!.SetAgentSelection(null);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.SelectResident))
            {
                DigAgentVisual? selected = agent;
                if (selected == null && decision.TargetEntityId.HasValue)
                {
                    selected = _agentRenderer!.SelectById(
                        decision.TargetEntityId.Value.ToString());
                }
                else
                {
                    _agentRenderer!.Select(selected);
                }

                _selectedCell = null;
                _renderer!.Select(null);
                _jobRenderer!.Select(null);
                _buildingRenderer!.Select(null);
                _hud!.SetAgentSelection(selected?.Model);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.SelectBuilding))
            {
                DigBuildingVisual? selected = building;
                if (selected == null && decision.TargetEntityId.HasValue)
                {
                    selected = _buildingRenderer!.SelectById(
                        decision.TargetEntityId.Value.ToString());
                }
                else
                {
                    _buildingRenderer!.Select(selected);
                }

                _selectedCell = null;
                _renderer!.Select(null);
                _agentRenderer!.Select(null);
                _jobRenderer!.Select(null);
                _hud!.SetBuildingSelection(selected?.Model);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.SelectGround)
                && cell != null)
            {
                SelectCell(cell);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.FocusResident)
                && decision.TargetEntityId.HasValue
                && _agentRenderer!.TryGetWorldPosition(
                    decision.TargetEntityId.Value.ToString(),
                    out Vector3 position))
            {
                _cameraController!.Focus(position);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.ShowReason)
                && decision.ReasonCode != null)
            {
                _hud!.SetStatus(decision.ReasonCode);
            }
        }

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
                decision.TargetCell.Value.Y);
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
            _selectedCell = cell;
            _agentRenderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _renderer!.Select(cell);
            _hud!.SetSelection(cell.Model);
        }

        private void SelectJob(DigJobVisual job)
        {
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _jobRenderer!.Select(job);
            _hud!.SetJobSelection(job.Model);
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
                new CellId(selected.X, selected.Y),
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
            DigCellVisual? refreshed = _renderer.SelectAt(selected.X, selected.Y);
            _selectedCell = refreshed;
            _hud.SetWorld(world);
            _hud.SetSelection(refreshed == null ? null : refreshed.Model);
        }
    }
}
