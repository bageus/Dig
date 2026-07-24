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
            _tunnelRenderer = tunnelRenderer
                ?? throw new ArgumentNullException(nameof(tunnelRenderer));
            _tunnelRenderer.SetDigInteractionActive(
                UsesTunnelCellInteraction(_excavationMode));
        }

        private void SetTunnelDigInteractionActive(bool active)
        {
            _renderer?.SetTunnelDigInteractionActive(active);
            _tunnelRenderer?.SetDigInteractionActive(active);
            _caveRoomFloorRenderer?.SetDigInteractionActive(active);
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

            if (TryAssignExplicitExcavation(hits, residentIds))
            {
                return true;
            }

            DigSelectedResidentTarget target = ResolveSelectedResidentTarget(hits);
            if (target.Kind != DigSelectedResidentTargetKind.Movement)
            {
                return false;
            }

            Result prepared = _terrainSession!.PrepareResidentsForDirectCommand(
                residentIds,
                _simulation!.CurrentTick);
            if (prepared.IsFailure)
            {
                _hud!.SetCommandResult(prepared);
                return true;
            }

            CellId destination = target.MovementCell;
            Result result = residentIds.Count == 1
                ? _simulation!.MoveResidentThroughTunnel(
                    residentIds[0],
                    destination,
                    target.MovementOffsetX,
                    _tunnelRenderer)
                : _simulation!.MoveResidentsThroughTunnel(
                    residentIds,
                    destination,
                    target.MovementOffsetX,
                    _tunnelRenderer);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                RefreshDirectCommandPresentation();
                _hud!.SetStatus(
                    $"Moving {residentIds.Count} dwarf(s) to the selected point "
                    + $"through hidden cell X={destination.X}, Y={destination.Y}, Z={destination.Z}.");
            }

            return true;
        }

        private void RefreshDirectCommandPresentation()
        {
            var jobs = _terrainSession!.LoadJobs();
            _jobRenderer!.Render(jobs);
            _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
            _hud!.SetJobs(jobs);
        }

        private bool TryAssignExplicitExcavation(
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
                    // A resident or item may overlap the highlighted excavation in
                    // screen space. Keep scanning the ordered hit stack instead of
                    // cancelling the explicit work command at the first blocker.
                    continue;
                }

                CellId? surfaceTarget = ResolveExcavationTarget(hit);
                if (surfaceTarget.HasValue)
                {
                    if (!PrepareExplicitExcavationResidents(residentIds))
                    {
                        return true;
                    }

                    bool assigned = AssignSurfaceExcavation(
                        surfaceTarget.Value,
                        residentIds);
                    RefreshDirectCommandPresentation();
                    return assigned;
                }

                if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job))
                {
                    if (IsTerminalJobStatus(job.Model.Status)
                        || !job.Model.TargetX.HasValue
                        || !job.Model.TargetY.HasValue
                        || !job.Model.TargetZ.HasValue)
                    {
                        continue;
                    }

                    if (!PrepareExplicitExcavationResidents(residentIds))
                    {
                        return true;
                    }

                    CellId workCell = new CellId(
                        job.Model.TargetX.Value,
                        job.Model.TargetY.Value,
                        job.Model.TargetZ.Value - 1);
                    bool found = _simulation!.TryAssignSpatialExcavation(
                        workCell,
                        residentIds,
                        out Result assignment);
                    if (!found)
                    {
                        continue;
                    }

                    _hud!.SetCommandResult(assignment);
                    if (assignment.IsSuccess)
                    {
                        RefreshDirectCommandPresentation();
                        _hud.SetStatus(
                            $"Assigned {residentIds.Count} selected dwarf(s) to spatial excavation "
                            + $"near X={workCell.X}, Y={workCell.Y}, Z={workCell.Z}.");
                    }

                    return true;
                }

                if (TryResolveTunnelDestination(hit, out _, out _))
                {
                    continue;
                }
            }

            return false;
        }

        private bool PrepareExplicitExcavationResidents(
            IReadOnlyList<string> residentIds)
        {
            Result prepared = _terrainSession!.PrepareResidentsForDirectCommand(
                residentIds,
                _simulation!.CurrentTick);
            if (prepared.IsSuccess)
            {
                return true;
            }

            _hud!.SetCommandResult(prepared);
            return false;
        }

        private bool AssignSurfaceExcavation(
            CellId target,
            IReadOnlyList<string> residentIds)
        {
            var selected = _agentRenderer!.GetSelectedModels();
            for (int index = 0; index < selected.Count; index++)
            {
                if (selected[index].CellZ != 0)
                {
                    _hud!.SetStatus(
                        "Move every selected dwarf to an open Z=0 tunnel cell before assigning front-layer excavation.");
                    return true;
                }
            }

            Result result = _simulation!.AssignExcavationCluster(target, residentIds);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    $"{residentIds.Count} selected dwarf(s) assigned across connected excavation cells within radius 4.");
            }

            return true;
        }

        private bool TryResolveTunnelDestination(
            RaycastHit hit,
            out CellId destination,
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
