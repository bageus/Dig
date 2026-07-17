using Dig.Domain.Core;
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
            if (!leftButton
                || _tunnelRenderer == null
                || !_tunnelRenderer.TryGetCell(hit, out DigTunnelCellVisual cell))
            {
                return false;
            }

            _tunnelRenderer.Select(cell);
            string? residentId = _agentRenderer!.SelectedAgentId;
            if (residentId == null)
            {
                _hud!.SetStatus("Select a dwarf, then click an open tunnel cell.");
                return true;
            }

            Result result = _simulation!.MoveResidentThroughTunnel(
                residentId,
                cell.Cell,
                _tunnelRenderer);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    $"Moving selected dwarf to X={cell.Cell.X}, Y={cell.Cell.Y}, Z={cell.Cell.Z}.");
            }

            return true;
        }
    }
}
