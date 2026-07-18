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

            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                _hud.SetStatus("Select an open horizontal tunnel cell.");
                return true;
            }

            SpatialCellId? source = ResolveTunnelDepthSource(hit);
            if (!source.HasValue)
            {
                _hud.SetStatus("Depth excavation can start only from a tunnel floor.");
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
                "Select the new cell to continue one layer deeper.");
            return true;
        }

        private SpatialCellId? ResolveTunnelDepthSource(RaycastHit hit)
        {
            if (_tunnelRenderer != null
                && _tunnelRenderer.TryGetCell(hit, out DigTunnelCellVisual tunnelCell))
            {
                return tunnelCell.Cell;
            }

            if (_renderer != null
                && _renderer.TryGetWalkSurface(hit, out SpatialCellId walkSurface))
            {
                return walkSurface;
            }

            return null;
        }
    }
}
