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

            IReadOnlyList<string> residentIds = _agentRenderer!.SelectedAgentIds;
            if (residentIds.Count == 0)
            {
                _hud!.SetStatus("Select one or more dwarfs, then click a walkable destination.");
                return false;
            }

            if (TryAssignExplicitSpatialExcavation(hits, residentIds))
            {
                return true;
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

        private bool TryAssignExplicitSpatialExcavation(
            RaycastHit[] hits,
            IReadOnlyList<string> residentIds)
        {
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (_agentRenderer!.TryGetAgent(hit, out _)
                    || (_buildingRenderer != null
                        && _buildingRenderer.TryGetBuilding(hit, out _))
                    || (_itemRenderer != null
                        && _itemRenderer.TryGetItem(hit, out _)))
                {
                    return false;
                }

                if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job))
                {
                    if (IsTerminalJobStatus(job.Model.Status)
                        || !job.Model.TargetX.HasValue
                        || !job.Model.TargetY.HasValue
                        || !job.Model.TargetZ.HasValue
                        || job.Model.TargetZ.Value <= 0)
                    {
                        return false;
                    }

                    SpatialCellId workCell = new SpatialCellId(
                        job.Model.TargetX.Value,
                        job.Model.TargetY.Value,
                        job.Model.TargetZ.Value - 1);
                    bool found = _simulation!.TryAssignSpatialExcavation(
                        workCell,
                        residentIds,
                        out Result assignment);
                    if (!found)
                    {
                        return false;
                    }

                    _hud!.SetCommandResult(assignment);
                    if (assignment.IsSuccess)
                    {
                        _hud.SetStatus(
                            $"Assigned {residentIds.Count} selected dwarf(s) to spatial excavation "
                            + $"near X={workCell.X}, Y={workCell.Y}, Z={workCell.Z}.");
                    }

                    return true;
                }

                if (TryResolveTunnelDestination(hit, out _, out _))
                {
                    return false;
                }
            }

            return false;
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

        private static bool IsTerminalJobStatus(string status)
        {
            return string.Equals(status, "Completed", StringComparison.Ordinal)
                || string.Equals(status, "Cancelled", StringComparison.Ordinal)
                || string.Equals(status, "Failed", StringComparison.Ordinal);
        }
    }
}
