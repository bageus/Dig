using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        internal Result MoveResidentThroughTunnel(
            string residentId,
            SpatialCellId destination,
            DigTunnelDemoRenderer tunnelRenderer)
        {
            if (AgentSession == null
                || AgentRenderer == null
                || TerrainSession == null
                || Hud == null)
            {
                return NotInitializedResult();
            }

            SynchronizeExcavatedTunnelNavigation();
            Result interrupted = TerrainSession.InterruptForManualMovement(
                new[] { residentId },
                CurrentTick);
            if (interrupted.IsFailure)
            {
                return interrupted;
            }

            PlanAgentTunnelRouteReport report =
                AgentSession.MoveResidentThroughTunnel(residentId, destination);
            if (report.Result.IsFailure)
            {
                return report.Result;
            }

            tunnelRenderer.ShowRoute(report.Path);
            RefreshTunnelMovementPresentation(AgentSession.LoadView());
            return Result.Success();
        }

        internal Result MoveResidentsThroughTunnel(
            IReadOnlyList<string> residentIds,
            SpatialCellId destination,
            DigTunnelDemoRenderer tunnelRenderer)
        {
            if (AgentSession == null
                || AgentRenderer == null
                || TerrainSession == null
                || Hud == null)
            {
                return NotInitializedResult();
            }

            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            if (tunnelRenderer == null)
            {
                throw new ArgumentNullException(nameof(tunnelRenderer));
            }

            if (residentIds.Count == 1)
            {
                return MoveResidentThroughTunnel(
                    residentIds[0],
                    destination,
                    tunnelRenderer);
            }

            SynchronizeExcavatedTunnelNavigation();
            Result interrupted = TerrainSession.InterruptForManualMovement(
                residentIds,
                CurrentTick);
            if (interrupted.IsFailure)
            {
                return interrupted;
            }

            PlanAgentsTunnelRoutesReport report =
                AgentSession.MoveResidentsThroughTunnel(residentIds, destination);
            if (report.Result.IsFailure)
            {
                return report.Result;
            }

            tunnelRenderer.ShowRoute(
                report.Entries.Count == 0 ? null : report.Entries[0].Path);
            RefreshTunnelMovementPresentation(AgentSession.LoadView());
            return Result.Success();
        }

        private void RefreshTunnelMovementPresentation(
            IReadOnlyList<AgentViewModel> agents)
        {
            RefreshEquipmentVisuals();
            Hud!.SetAgents(agents, AgentSession!.Tick);
            Hud.SetAgentSelection(
                AgentRenderer!.SelectedModel,
                AgentRenderer.SelectedCount);
        }

        private static Result NotInitializedResult()
        {
            return Result.Failure(new DomainError(
                "unity.tunnel.not_initialized",
                "The layered tunnel movement runtime is not initialized."));
        }
    }
}
