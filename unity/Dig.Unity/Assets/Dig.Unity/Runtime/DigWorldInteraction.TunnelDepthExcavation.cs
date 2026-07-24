using System;
using System.Collections.Generic;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private bool TryHandleTunnelDepthExcavation()
        {
            if (_excavationMode == DigExcavationDrawingMode.Delete)
            {
                return TryHandleDepthExcavationErase();
            }

            if (_excavationMode != DigExcavationDrawingMode.Depth)
            {
                return false;
            }

            if (Input.GetMouseButtonUp(0))
            {
                ResetExcavationStroke();
                return true;
            }

            if (!Input.GetMouseButton(0))
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

            CellId? rawSource = ResolveTunnelDepthSource();
            if (!rawSource.HasValue)
            {
                _hud.SetStatus(
                    "Depth excavation requires an open tunnel or room cell under the cursor.");
                return true;
            }

            CellId source = rawSource.Value;
            if (!_excavationAnchor.HasValue)
            {
                _excavationAnchor = source;
            }

            if (source.Z != _excavationAnchor.Value.Z)
            {
                _hud.SetStatus("Finish the current depth stroke before selecting another layer.");
                return true;
            }

            ExcavationStrokeDecision decision = _excavationStrokePlanner.Resolve(
                _excavationAnchor.Value,
                source,
                _excavationAxis);
            _excavationAxis = decision.Axis;
            source = decision.Cell;

            if (_lastExcavationPaintCell.HasValue
                && _lastExcavationPaintCell.Value == source)
            {
                return true;
            }

            Result result = ApplyTunnelDepthStroke(source);
            _hud.SetCommandResult(result);
            return true;
        }

        private bool TryHandleDepthExcavationErase()
        {
            if (Input.GetMouseButtonUp(0))
            {
                ResetExcavationStroke();
                return true;
            }

            if (!Input.GetMouseButton(0)
                || _buildingPlacementMode.HasValue
                || _hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return false;
            }

            CellId? target = ResolveExcavationEraseTarget();
            if (!target.HasValue)
            {
                return true;
            }

            if (_lastExcavationPaintCell.HasValue
                && _lastExcavationPaintCell.Value == target.Value)
            {
                return true;
            }

            Result result = ApplyImmediateExcavationErase(target.Value);
            _hud.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _lastExcavationPaintCell = target.Value;
            }

            return true;
        }

        private CellId? ResolveExcavationEraseTarget()
        {
            RaycastHit[] hits = GetPointerHits();
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (_renderer!.TryGetDepthDesignation(hit, out CellId depthTarget))
                {
                    return depthTarget;
                }

                if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
                    && !IsTerminalJobStatus(job.Model.Status)
                    && job.Model.TargetX.HasValue
                    && job.Model.TargetY.HasValue
                    && job.Model.TargetZ.HasValue)
                {
                    return new CellId(
                        job.Model.TargetX.Value,
                        job.Model.TargetY.Value,
                        job.Model.TargetZ.Value);
                }

                if (_renderer.TryGetCell(hit, out DigCellVisual cell)
                    && cell.Model.IsDesignated)
                {
                    return new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
                }

                if (_renderer.TryGetWalkSurface(hit, out CellId walkSurface)
                    && _session!.IsPlannedHorizontalTunnel(walkSurface))
                {
                    return walkSurface;
                }
            }

            return null;
        }

        private Result ApplyImmediateExcavationErase(CellId target)
        {
            IReadOnlyList<CellId> expanded =
                _session!.ExpandExcavationEraseCells(new[] { target });
            Result<EraseExcavationBatchReport> erased =
                _simulation!.ApplyExcavationEraseBatch(expanded);
            if (erased.IsFailure)
            {
                return Result.Failure(erased.Error!);
            }

            for (int index = 0; index < expanded.Count; index++)
            {
                if (expanded[index].Z > 0)
                {
                    _renderer!.RemoveDepthDesignationTint(expanded[index]);
                }
            }

            _hud!.SetStatus(
                $"Removed {erased.Value.DesignationCount} designation(s) and cancelled "
                + $"{erased.Value.CancelledJobIds.Count} job(s).");
            return Result.Success();
        }

        private Result ApplyTunnelDepthStroke(CellId source)
        {
            if (!_lastExcavationPaintCell.HasValue
                || _excavationAxis == ExcavationStrokeAxis.None)
            {
                return DesignateTunnelDepthCell(source);
            }

            CellId current = _lastExcavationPaintCell.Value;
            int xStep = Math.Sign(source.X - current.X);
            int yStep = Math.Sign(source.Y - current.Y);
            while (current != source)
            {
                current = new CellId(current.X + xStep, current.Y + yStep, current.Z);
                Result applied = DesignateTunnelDepthCell(current);
                if (applied.IsFailure)
                {
                    return applied;
                }
            }

            return Result.Success();
        }

        private Result DesignateTunnelDepthCell(CellId? source)
        {
            if (!source.HasValue)
            {
                return Result.Failure(new DomainError(
                    "unity.excavation.depth_source_missing",
                    "Depth excavation source is missing."));
            }

            Result result = DesignateTunnelDepth(source.Value);
            if (result.IsFailure)
            {
                return result;
            }

            CellId sourceCell = source.Value;
            _lastExcavationPaintCell = sourceCell;
            CellId target = new CellId(sourceCell.X, sourceCell.Y, sourceCell.Z + 1);
            _renderer!.SetDepthDesignationTint(target);
            _hud!.SetStatus(
                $"Depth excavation designated. Depth excavation marked through "
                + $"X={target.X}, Y={target.Y}, Z={target.Z}.");
            return Result.Success();
        }

        private Result DesignateTunnelDepth(CellId source)
        {
            return _simulation!.DesignateTunnelDepth(source);
        }

        private CellId? ResolveTunnelDepthSource()
        {
            RaycastHit[] hits = GetPointerHits();
            CellId? selected = null;
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (_tunnelRenderer!.TryGetCell(hit, out DigTunnelCellVisual tunnelCell)
                    && (!selected.HasValue || tunnelCell.Cell.Z > selected.Value.Z))
                {
                    selected = tunnelCell.Cell;
                    continue;
                }

                if (_caveRoomFloorRenderer != null
                    && _caveRoomFloorRenderer.TryGetCell(
                        hit,
                        out DigTunnelCellVisual roomCell)
                    && (!selected.HasValue || roomCell.Cell.Z > selected.Value.Z))
                {
                    selected = roomCell.Cell;
                    continue;
                }

                if (_renderer!.TryGetWalkSurface(hit, out CellId walkSurface)
                    && walkSurface.Z == 0
                    && _session!.IsPlannedHorizontalTunnel(walkSurface)
                    && !selected.HasValue)
                {
                    selected = walkSurface;
                }
            }

            return selected;
        }
    }
}
