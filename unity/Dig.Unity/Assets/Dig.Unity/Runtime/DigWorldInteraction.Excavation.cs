using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    internal enum DigExcavationDrawingMode
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
    }

    public sealed partial class DigWorldInteraction
    {
        private DigExcavationDrawingMode _excavationMode;
        private CellId? _excavationAnchor;
        private int _excavationPriority = 750;

        internal string ExcavationModeLabel => _excavationMode.ToString();
        internal int ExcavationPriority => _excavationPriority;
        internal bool CanActivateExcavationDrawing =>
            _agentRenderer != null && _agentRenderer.SelectedAgentId == null;

        internal void SetExcavationDrawingMode(DigExcavationDrawingMode mode)
        {
            if (mode != DigExcavationDrawingMode.None
                && !CanActivateExcavationDrawing)
            {
                _hud!.SetStatus("Clear the dwarf selection before drawing tunnels.");
                return;
            }

            _excavationMode = mode;
            _excavationAnchor = null;
            _hud!.SetStatus(mode == DigExcavationDrawingMode.None
                ? "Excavation drawing disabled."
                : $"{mode} excavation drawing active on Z=0.");
        }

        internal void AdjustExcavationPriority(int delta)
        {
            _excavationPriority = Mathf.Clamp(_excavationPriority + delta, 0, 1000);
        }

        private void DisableExcavationDrawing()
        {
            _excavationMode = DigExcavationDrawingMode.None;
            _excavationAnchor = null;
        }

        private bool TryHandleExcavationInput(
            RaycastHit hit,
            bool leftButton,
            bool rightButton)
        {
            if (TryAssignSelectedResidentToExcavation(hit, leftButton))
            {
                return true;
            }

            if (_excavationMode == DigExcavationDrawingMode.None
                || (!leftButton && !rightButton)
                || !CanActivateExcavationDrawing
                || !_renderer!.TryGetCell(hit, out DigCellVisual cell))
            {
                return false;
            }

            CellId target = ResolveDrawingCell(new CellId(cell.Model.X, cell.Model.Y));
            Result result = _simulation!.ApplyExcavationDesignation(
                target,
                active: leftButton,
                _excavationPriority);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                if (leftButton && !_excavationAnchor.HasValue)
                {
                    _excavationAnchor = target;
                }

                _hud.SetStatus(leftButton
                    ? $"Dig Job created at X={target.X}, Y={target.Y}, Z=0."
                    : $"Excavation designation removed at X={target.X}, Y={target.Y}, Z=0.");
            }

            return true;
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

        private CellId ResolveDrawingCell(CellId hit)
        {
            if (!_excavationAnchor.HasValue)
            {
                return hit;
            }

            if (_excavationMode == DigExcavationDrawingMode.Horizontal)
            {
                return new CellId(hit.X, _excavationAnchor.Value.Y);
            }

            if (_excavationMode == DigExcavationDrawingMode.Vertical)
            {
                return new CellId(_excavationAnchor.Value.X, hit.Y);
            }

            return hit;
        }
    }
}
