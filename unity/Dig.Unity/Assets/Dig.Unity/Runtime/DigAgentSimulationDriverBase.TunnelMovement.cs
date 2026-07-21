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
            float destinationOffsetX,
            DigTunnelDemoRenderer tunnelRenderer)
        {
            if (AgentSession == null
                || AgentRenderer == null
                || TerrainSession == null
                || Hud == null)
            {
                return NotInitializedResult();
            }

            SynchronizeTunnelInteractionTargets(tunnelRenderer);
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

            AgentRenderer.SetFreeformDestination(
                residentId,
                destination,
                destinationOffsetX);
            tunnelRenderer.ShowRoute(report.Path, destinationOffsetX);
            RefreshTunnelMovementPresentation(AgentSession.LoadView());
            return Result.Success();
        }

        internal Result MoveResidentsThroughTunnel(
            IReadOnlyList<string> residentIds,
            SpatialCellId destination,
            float destinationOffsetX,
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
                    destinationOffsetX,
                    tunnelRenderer);
            }

            SynchronizeTunnelInteractionTargets(tunnelRenderer);
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

            for (int index = 0; index < report.Entries.Count; index++)
            {
                PlannedAgentTunnelRoute entry = report.Entries[index];
                SpatialCellId assigned = entry.Path.Cells[entry.Path.Cells.Count - 1];
                float assignedOffset = ResolveGroupDestinationOffset(
                    destinationOffsetX,
                    index,
                    report.Entries.Count);
                AgentRenderer.SetFreeformDestination(
                    entry.AgentId.ToString(),
                    assigned,
                    assignedOffset);
            }

            float routeOffset = report.Entries.Count == 0
                ? destinationOffsetX
                : ResolveGroupDestinationOffset(
                    destinationOffsetX,
                    index: 0,
                    report.Entries.Count);
            tunnelRenderer.ShowRoute(
                report.Entries.Count == 0 ? null : report.Entries[0].Path,
                routeOffset);
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

        private static float ResolveGroupDestinationOffset(
            float requestedOffset,
            int index,
            int count)
        {
            const float spacing = 0.18f;
            const float limit = 0.44f;
            float centeredIndex = index - ((count - 1) * 0.5f);
            return Math.Max(
                -limit,
                Math.Min(limit, requestedOffset + (centeredIndex * spacing)));
        }
    }
}
