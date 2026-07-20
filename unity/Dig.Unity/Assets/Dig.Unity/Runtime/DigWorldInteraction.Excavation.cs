using System;
using System.Collections.Generic;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
    internal enum DigExcavationDrawingMode
    {
        None = 0,
        Tunnel = 1,
        Delete = 2,
        Depth = 3,
    }

    public sealed partial class DigWorldInteraction
    {
        private readonly ExcavationStrokePlanner _excavationStrokePlanner =
            new ExcavationStrokePlanner();
        private DigExcavationDrawingMode _excavationMode;
        private ExcavationStrokeAxis _excavationAxis;
        private CellId? _excavationAnchor;
        private CellId? _lastExcavationPaintCell;
        private int _excavationPriority = 750;

        internal string ExcavationModeLabel => _caveRoomPreset.HasValue
            ? $"{_caveRoomPreset.Value} Cave"
            : _excavationMode.ToString();
        internal int ExcavationPriority => _excavationPriority;
        internal bool CanActivateExcavationDrawing =>
            _agentRenderer != null && _agentRenderer.SelectedAgentId == null;

        internal void SetExcavationDrawingMode(DigExcavationDrawingMode mode)
        {
            if (!Enum.IsDefined(typeof(DigExcavationDrawingMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (mode != DigExcavationDrawingMode.None
                && !CanActivateExcavationDrawing)
            {
                _hud!.SetStatus("Clear the dwarf selection before editing tunnels.");
                return;
            }

            DisableCaveRoomPlanning();
            _excavationMode = mode;
            _renderer!.SetTunnelDigInteractionActive(UsesTunnelCellInteraction(mode));
            ResetExcavationStroke();
            _hud!.SetStatus(mode switch
            {
                DigExcavationDrawingMode.None => "Excavation editing disabled.",
                DigExcavationDrawingMode.Tunnel =>
                    "Tunnel tool active on Z=0. Hold LMB and move horizontally or vertically.",
                DigExcavationDrawingMode.Delete =>
                    "Delete tool active. Hold LMB over marked cells.",
                DigExcavationDrawingMode.Depth =>
                    "Depth tool active. LMB an open tunnel or room cell to designate one deeper layer.",
                _ => throw new InvalidOperationException("Unknown excavation mode."),
            });
        }

        internal void AdjustExcavationPriority(int delta)
        {
            _excavationPriority = Mathf.Clamp(_excavationPriority + delta, 0, 1000);
        }

        private void DisableExcavationDrawing()
        {
            _excavationMode = DigExcavationDrawingMode.None;
            _renderer?.SetTunnelDigInteractionActive(active: false);
            ResetExcavationStroke();
            DisableCaveRoomPlanning();
        }

        private bool TryHandleExcavationStroke()
        {
            if (_excavationMode == DigExcavationDrawingMode.Depth)
            {
                return false;
            }

            if (Input.GetMouseButtonUp(0))
            {
                bool wasEditing = _excavationMode != DigExcavationDrawingMode.None;
                ResetExcavationStroke();
                return wasEditing;
            }

            if (_excavationMode == DigExcavationDrawingMode.None
                || !Input.GetMouseButton(0))
            {
                return false;
            }

            if (!CanActivateExcavationDrawing
                || _buildingPlacementMode.HasValue
                || _hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return true;
            }

            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                return true;
            }

            CellId? rawTarget = ResolveExcavationPaintTarget(hit);
            if (!rawTarget.HasValue)
            {
                return true;
            }

            CellId target = rawTarget.Value;
            bool active = _excavationMode == DigExcavationDrawingMode.Tunnel;
            if (active)
            {
                if (!_excavationAnchor.HasValue)
                {
                    _excavationAnchor = target;
                }

                ExcavationStrokeDecision decision = _excavationStrokePlanner.Resolve(
                    _excavationAnchor.Value,
                    target,
                    _excavationAxis);
                _excavationAxis = decision.Axis;
                target = decision.Cell;
            }

            if (_lastExcavationPaintCell.HasValue
                && _lastExcavationPaintCell.Value == target)
            {
                return true;
            }

            Result result = ApplyExcavationStroke(target, active);
            _hud.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(active
                    ? $"Tunnel marked through X={target.X}, Y={target.Y}, Z=0."
                    : $"Tunnel mark removed at X={target.X}, Y={target.Y}, Z=0.");
            }

            return true;
        }

        private Result ApplyExcavationStroke(CellId target, bool active)
        {
            if (!active
                || !_lastExcavationPaintCell.HasValue
                || _excavationAxis == ExcavationStrokeAxis.None)
            {
                Result single = ApplyExcavationCell(target, active);
                if (single.IsSuccess)
                {
                    _lastExcavationPaintCell = target;
                }

                return single;
            }

            CellId current = _lastExcavationPaintCell.Value;
            int xStep = Math.Sign(target.X - current.X);
            int yStep = Math.Sign(target.Y - current.Y);
            while (current != target)
            {
                current = new CellId(current.X + xStep, current.Y + yStep);
                Result applied = ApplyExcavationCell(current, active: true);
                if (applied.IsFailure)
                {
                    return applied;
                }

                _lastExcavationPaintCell = current;
            }

            return Result.Success();
        }

        private Result ApplyExcavationCell(CellId target, bool active)
        {
            if (active && _session!.IsProtected(target))
            {
                _renderer!.HighlightRejected(target);
                _hud!.SetStatus(
                    "Protected rock cannot be excavated. Borders and the first upper rock row must remain intact.");
                return Result.Failure(DigWorldSession.ProtectedRock);
            }

            Result result = _simulation!.ApplyExcavationDesignation(
                target,
                active,
                _excavationPriority);
            if (result.IsFailure)
            {
                return result;
            }

            bool vertical = active && _excavationAxis == ExcavationStrokeAxis.Vertical;
            if (vertical && _excavationAnchor.HasValue)
            {
                _session!.SetTunnelPlan(
                    _excavationAnchor.Value,
                    active: true,
                    vertical: true);
            }

            _session!.SetTunnelPlan(target, active, vertical);
            return Result.Success();
        }

        private bool TryAssignSelectedResidentToExcavation(
            RaycastHit hit,
            bool leftButton)
        {
            IReadOnlyList<string> residentIds = _agentRenderer!.SelectedAgentIds;
            if (!leftButton || residentIds.Count == 0)
            {
                return false;
            }

            DigSelectedResidentTarget target = ResolveSelectedResidentTarget();
            if (target.Kind != DigSelectedResidentTargetKind.Excavation)
            {
                return false;
            }

            IReadOnlyList<AgentViewModel> selected = _agentRenderer.GetSelectedModels();
            for (int index = 0; index < selected.Count; index++)
            {
                AgentViewModel resident = selected[index];
                if (resident.CellZ == 0)
                {
                    continue;
                }

                Result moved = _simulation!.MoveResidentThroughTunnel(
                    resident.Id,
                    new SpatialCellId(resident.CellX, resident.CellY, 0),
                    _tunnelRenderer!);
                if (moved.IsFailure)
                {
                    _hud!.SetCommandResult(moved);
                    return true;
                }
            }

            Result result = _simulation!.AssignExcavationCluster(
                target.ExcavationCell,
                residentIds);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    $"{residentIds.Count} selected dwarf(s) assigned across connected excavation cells within radius 4.");
            }

            return true;
        }

        private CellId? ResolveExcavationTarget(RaycastHit hit)
        {
            if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
                && job.Model.TargetX.HasValue
                && job.Model.TargetY.HasValue
                && (!job.Model.TargetZ.HasValue || job.Model.TargetZ.Value == 0))
            {
                return new CellId(job.Model.TargetX.Value, job.Model.TargetY.Value);
            }

            if (_renderer!.TryGetCell(hit, out DigCellVisual cell)
                && cell.Model.IsDesignated)
            {
                return new CellId(cell.Model.X, cell.Model.Y);
            }

            return null;
        }

        private CellId? ResolveExcavationPaintTarget(RaycastHit hit)
        {
            if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
                && job.Model.TargetX.HasValue
                && job.Model.TargetY.HasValue
                && (!job.Model.TargetZ.HasValue || job.Model.TargetZ.Value == 0))
            {
                return new CellId(job.Model.TargetX.Value, job.Model.TargetY.Value);
            }

            if (_renderer!.TryGetCell(hit, out DigCellVisual cell))
            {
                return new CellId(cell.Model.X, cell.Model.Y);
            }

            return null;
        }

        private static bool UsesTunnelCellInteraction(DigExcavationDrawingMode mode)
        {
            return mode == DigExcavationDrawingMode.Tunnel
                || mode == DigExcavationDrawingMode.Delete
                || mode == DigExcavationDrawingMode.Depth;
        }

        private void ResetExcavationStroke()
        {
            _excavationAxis = ExcavationStrokeAxis.None;
            _excavationAnchor = null;
            _lastExcavationPaintCell = null;
        }
    }
}