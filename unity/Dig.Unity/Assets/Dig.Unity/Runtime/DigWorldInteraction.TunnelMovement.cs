using System;
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

        private void SynchronizeTunnelInteractionTargets()
        {
            if (_simulation != null && _tunnelRenderer != null)
            {
                _simulation.SynchronizeTunnelInteractionTargets(_tunnelRenderer);
            }
        }

        private bool TryApplyTunnelMove(RaycastHit hit, bool leftButton)
        {
            return TryApplyTunnelMove(GetPointerHits(), leftButton);
        }

        private bool TryApplyTunnelMove(RaycastHit[] hits, bool leftButton)
        {
            if (!leftButton || _tunnelRenderer == null)
            {
                return false;
            }

            if (hits == null)
            {
                throw new ArgumentNullException(nameof(hits));
            }

            DigSelectedResidentTarget target = ResolveSelectedResidentTarget(hits);
            if (target.Kind != DigSelectedResidentTargetKind.Movement)
            {
                return false;
            }

            if (target.Visual != null)
            {
                _tunnelRenderer.Select(target.Visual);
            }
            else
            {
                _tunnelRenderer.Select(null);
            }

            IReadOnlyList<string> residentIds = _agentRenderer!.SelectedAgentIds;
            if (residentIds.Count == 0)
            {
                _hud!.SetStatus("Select one or more dwarfs, then click a walkable destination.");
                return true;
            }

            SpatialCellId destination = target.MovementCell;
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
                    $"Moving {residentIds.Count} dwarf(s) to "
                    + $"X={destination.X}, Y={destination.Y}, Z={destination.Z}.");
            }

            return true;
        }

        private bool TryResolveTunnelDestination(
            RaycastHit hit,
            out SpatialCellId destination,
            out DigTunnelCellVisual? visual)
        {
            if (_tunnelRenderer != null
                && _tunnelRenderer.TryGetCell(hit, out DigTunnelCellVisual tunnelCell))
            {
                destination = tunnelCell.Cell;
                visual = tunnelCell;
                return true;
            }

            if (_caveRoomFloorRenderer != null
                && _caveRoomFloorRenderer.TryGetCell(hit, out DigTunnelCellVisual roomCell))
            {
                destination = roomCell.Cell;
                visual = null;
                return true;
            }

            destination = default;
            visual = null;
            return false;
        }
    }
}
