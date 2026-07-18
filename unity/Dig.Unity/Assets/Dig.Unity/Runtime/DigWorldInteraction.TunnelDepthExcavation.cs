using Dig.Application.World;
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

            if (!CanActivateExcavationDrawing
                || _buildingPlacementMode.HasValue)
            {
                return true;
            }

            if (_hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return false;
            }

            SpatialCellId? source = ResolveTunnelDepthSource();
            if (!source.HasValue)
            {
                _hud.SetStatus(
                    "Depth excavation requires an open horizontal tunnel cell under the cursor.");
                return true;
            }

            TunnelDepthExcavationPlanResult result =
                _simulation!.ExcavateTunnelDepth(source.Value, _tunnelRenderer!);
            if (!result.Succeeded)
            {
                _hud.SetStatus(result.Detail);
                return true;
            }

            RefreshCompletedCaveRooms(force: true);
            SpatialCellId target = result.Plan!.Target;
            _hud.SetStatus(
                $"Tunnel depth excavated at X={target.X}, Y={target.Y}, Z={target.Z}. " +
                "The new deepest tunnel cell is selected for the next step.");
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
                    && !tunnelCell.IsVerticalTunnel
                    && tunnelCell.CanExcavateDepth
                    && (!selected.HasValue || tunnelCell.Cell.Z > selected.Value.Z))
                {
                    selected = tunnelCell.Cell;
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
