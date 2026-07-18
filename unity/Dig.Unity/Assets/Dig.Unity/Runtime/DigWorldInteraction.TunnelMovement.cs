using System.Collections.Generic;
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

            IReadOnlyList<string> residentIds = _agentRenderer!.SelectedAgentIds;
            if (residentIds.Count == 0)
            {
                _hud!.SetStatus("Select one or more dwarfs, then click a walkable destination.");
                return true;
            }

            Result result = residentIds.Count == 1
                ? _simulation!.MoveResidentThroughTunnel(
                    residentIds[0],
                    destination,
                    _tunnelRenderer)
                : _simulation!.MoveResidentsThroughTunnel(
                    residentIds,
                    destination,
                    _tunnelRenderer);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    $"Moving {residentIds.Count} dwarf(s) to " +
                    $"X={destination.X}, Y={destination.Y}, Z={destination.Z}.");
            }

            return true;
        }
    }
}
