using System;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    internal enum DigExcavationDrawingMode
    {
        None = 0,
        Tunnel = 1,
        Delete = 2,
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

        internal string ExcavationModeLabel => _excavationMode.ToString();
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

            _excavationMode = mode;
            ResetExcavationStroke();
            _hud!.SetStatus(mode switch
            {
                DigExcavationDrawingMode.None => "Excavation editing disabled.",
                DigExcavationDrawingMode.Tunnel =>
                    "Tunnel tool active on Z=0. Hold LMB and move horizontally or vertically.",
                DigExcavationDrawingMode.Delete =>
                    "Delete tool active. Hold LMB over marked cells.",
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
            ResetExcavationStroke();
        }

        private bool TryHandleExcavationStroke()
        {
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
                Result single = _simulation!.ApplyExcavationDesignation(
                    target,
                    active,
                    _excavationPriority);
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
                Result applied = _simulation!.ApplyExcavationDesignation(
                    current,
                    active: true,
                    _excavationPriority);
                if (applied.IsFailure)
                {
                    return applied;
                }

                _lastExcavationPaintCell = current;
            }

            return Result.Success();
        }

        private bool TryAssignSelectedResidentToExcavation(
            RaycastHit hit,
            bool leftButton)
        {
            string? residentId = _agentRenderer!.SelectedAgentId;
            if (!leftButton || residentId == null)
            {
                return false;
            }

            CellId? target = ResolveExcavationTarget(hit);
            if (!target.HasValue)
            {
                return false;
            }

            var selected = _agentRenderer.SelectedModel!;
            if (selected.CellZ != 0)
            {
                Result moved = _simulation!.MoveResidentThroughTunnel(
                    residentId,
                    new SpatialCellId(selected.CellX, selected.CellY, 0),
                    _tunnelRenderer!);
                if (moved.IsFailure)
                {
                    _hud!.SetCommandResult(moved);
                    return true;
                }
            }

            Result result = _simulation!.AssignExcavationCluster(target.Value, residentId);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    "Selected dwarf assigned to connected excavation cells within radius 4.");
            }

            return true;
        }

        private CellId? ResolveExcavationTarget(RaycastHit hit)
        {
            if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
                && job.Model.TargetX.HasValue
                && job.Model.TargetY.HasValue)
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
                && job.Model.TargetY.HasValue)
            {
                return new CellId(job.Model.TargetX.Value, job.Model.TargetY.Value);
            }

            if (_renderer!.TryGetCell(hit, out DigCellVisual cell))
            {
                return new CellId(cell.Model.X, cell.Model.Y);
            }

            return null;
        }

        private void ResetExcavationStroke()
        {
            _excavationAxis = ExcavationStrokeAxis.None;
            _excavationAnchor = null;
            _lastExcavationPaintCell = null;
        }
    }
}
