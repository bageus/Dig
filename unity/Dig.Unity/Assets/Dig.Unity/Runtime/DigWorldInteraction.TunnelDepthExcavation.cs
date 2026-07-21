using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private bool TryHandleTunnelDepthExcavation()
        {
            if (_excavationMode != DigExcavationDrawingMode.Depth
                || !Input.GetMouseButtonDown(0))
            {
                return false;
            }

            if (!CanActivateExcavationDrawing)
            {
                return false;
            }

            if (_buildingPlacementMode.HasValue
                || _hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return false;
            }

            SpatialCellId? source = ResolveTunnelDepthSource();
            if (!source.HasValue)
            {
                _hud.SetStatus(
                    "Depth excavation requires an open tunnel or room cell under the cursor.");
                return true;
            }

            Result result = _simulation!.DesignateTunnelDepth(source.Value);
            if (result.IsFailure)
            {
                _hud.SetCommandResult(result);
                return true;
            }

            SpatialCellId target = new SpatialCellId(
                source.Value.X,
                source.Value.Y,
                source.Value.Z + 1);
            _hud.SetStatus(
                $"Depth excavation designated at X={target.X}, Y={target.Y}, Z={target.Z}. "
                + "A worker must reach the open face and finish the Dig job.");
            return true;
        }

        private SpatialCellId? ResolveTunnelDepthSource()
        {
            RaycastHit[] hits = GetPointerHits();
            SpatialCellId? selected = null;
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

                if (_renderer!.TryGetWalkSurface(hit, out SpatialCellId walkSurface)
                    && walkSurface.Z == 0
                    && _session!.IsPlannedHorizontalTunnel(walkSurface.Projection)
                    && !selected.HasValue)
                {
                    selected = walkSurface;
                }
            }

            return selected;
        }
    }
}
