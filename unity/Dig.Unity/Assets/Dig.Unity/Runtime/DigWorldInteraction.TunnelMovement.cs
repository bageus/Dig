using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigTunnelDemoRenderer? _tunnelRenderer;

        internal void SetTunnelMovement(DigTunnelDemoRenderer tunnelRenderer)
        {
            _tunnelRenderer = tunnelRenderer;
        }

        private bool TryClearResidentSelection(bool rightButton)
        {
            if (!rightButton
                || _buildingPlacementMode.HasValue
                || _agentRenderer == null
                || _agentRenderer.SelectedAgentId == null)
            {
                return false;
            }

            _agentRenderer.Select(null);
            _hud!.SetAgentSelection(null);
            _tunnelRenderer?.Select(null);
            _tunnelRenderer?.ShowRoute(null);
            _hud.SetStatus("Resident selection cleared.");
            return true;
        }

        private bool TryApplyTunnelMove(RaycastHit hit, bool leftButton)
        {
            if (!leftButton || _tunnelRenderer == null)
            {
                return false;
            }

            SpatialCellId destination;
            if (_tunnelRenderer.TryGetCell(hit, out DigTunnelCellVisual tunnelCell))
            {
                destination = tunnelCell.Cell;
                _tunnelRenderer.Select(tunnelCell);
            }
            else if (_renderer != null
                && _renderer.TryGetWalkSurface(hit, out SpatialCellId walkSurface))
            {
                destination = walkSurface;
                _tunnelRenderer.Select(null);
            }
            else
            {
                return false;
            }

            string? residentId = _agentRenderer!.SelectedAgentId;
            if (residentId == null)
            {
                _hud!.SetStatus("Select a dwarf, then click a walkable destination.");
                return true;
            }

            Result result = _simulation!.MoveResidentThroughTunnel(
                residentId,
                destination,
                _tunnelRenderer);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    $"Moving to X={destination.X}, Y={destination.Y}, Z={destination.Z}.");
            }

            return true;
        }
    }
}
