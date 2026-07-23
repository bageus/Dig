using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly HashSet<CellId> _excavationEraseBatch =
            new HashSet<CellId>();
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
            SetTunnelDigInteractionActive(UsesTunnelCellInteraction(mode));
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
            SetTunnelDigInteractionActive(active: false);
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
                if (_excavationMode == DigExcavationDrawingMode.Delete
                    && _excavationEraseBatch.Count > 0)
                {
                    ApplyExcavationEraseBatch();
                }
                ResetExcavationStroke();
                return wasEditing;
            }

            if (_excavationMode == DigExcavationDrawingMode.None
                || !Input.GetMouseButton(0))
            {
                return false;
            }

            if (!CanActivateExcavationDrawing)
            {
                ResetExcavationStroke();
                return false;
            }

            if (_buildingPlacementMode.HasValue
                || _hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return false;
            }

            RaycastHit[] hits = GetPointerHits();
            CellId? rawTarget = null;
            for (int index = 0; index < hits.Length; index++)
            {
                rawTarget = ResolveExcavationPaintTarget(hits[index]);
                if (rawTarget.HasValue)
                {
                    break;
                }
            }

            if (!rawTarget.HasValue)
            {
                return false;
            }

            CellId target = rawTarget.Value;
            if (_excavationMode == DigExcavationDrawingMode.Delete)
            {
                _excavationEraseBatch.Add(target);
                _lastExcavationPaintCell = target;
                _hud.SetStatus(
                    $"Eraser preview: {_excavationEraseBatch.Count} cell(s). Release LMB to apply.");
                return true;
            }

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

        private void ApplyExcavationEraseBatch()
        {
            CellId[] cells = _excavationEraseBatch.OrderBy(cell => cell).ToArray();
            Result<Dig.Application.World.EraseExcavationBatchReport> result =
                _simulation!.ApplyExcavationEraseBatch(cells);
            if (result.IsFailure)
            {
                _hud!.SetCommandResult(Result.Failure(result.Error!));
                return;
            }

            _hud!.SetStatus(
                $"Removed {result.Value.DesignationCount} designation(s) and cancelled "
                + $"{result.Value.CancelledJobIds.Count} job(s).");
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
                current = new CellId(current.X + xStep, current.Y + yStep, current.Z);
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
                if (selected[index].CellZ != 0)
                {
                    _hud!.SetStatus(
                        "Move every selected dwarf to an open Z=0 tunnel cell before assigning front-layer excavation.");
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
                && !IsTerminalJobStatus(job.Model.Status)
                && job.Model.TargetX.HasValue
                && job.Model.TargetY.HasValue
                && (!job.Model.TargetZ.HasValue || job.Model.TargetZ.Value == 0))
            {
                return new CellId(job.Model.TargetX.Value, job.Model.TargetY.Value, job.Model.TargetZ ?? 0);
            }

            if (_renderer!.TryGetCell(hit, out DigCellVisual cell)
                && cell.Model.IsDesignated)
            {
                return new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
            }

            return null;
        }

        private CellId? ResolveExcavationPaintTarget(RaycastHit hit)
        {
            if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
                && !IsTerminalJobStatus(job.Model.Status)
                && job.Model.TargetX.HasValue
                && job.Model.TargetY.HasValue
                && (!job.Model.TargetZ.HasValue || job.Model.TargetZ.Value == 0))
            {
                return new CellId(job.Model.TargetX.Value, job.Model.TargetY.Value, job.Model.TargetZ ?? 0);
            }

            if (_renderer!.TryGetCell(hit, out DigCellVisual cell))
            {
                return new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
            }

            return null;
        }

        private void ResetExcavationStroke()
        {
            _excavationAxis = ExcavationStrokeAxis.None;
            _excavationAnchor = null;
            _lastExcavationPaintCell = null;
            _excavationEraseBatch.Clear();
        }
    }
}
