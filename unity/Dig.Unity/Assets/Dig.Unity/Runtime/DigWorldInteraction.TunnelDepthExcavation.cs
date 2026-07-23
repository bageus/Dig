using System;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private bool TryHandleTunnelDepthExcavation()
        {
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

        private Result DesignateTunnelDepthCell(CellId source)
        {
            Result result = _simulation!.DesignateTunnelDepth(source);
            if (result.IsFailure)
            {
                return result;
            }

            _lastExcavationPaintCell = source;
            CellId target = new CellId(source.X, source.Y, source.Z + 1);
            _hud!.SetStatus(
                $"Depth excavation marked through X={target.X}, Y={target.Y}, Z={target.Z}.");
            return Result.Success();
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
