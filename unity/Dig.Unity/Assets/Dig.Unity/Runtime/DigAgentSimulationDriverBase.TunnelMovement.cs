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
            return MoveResidentsThroughTunnel(
                new[] { residentId },
                destination,
                tunnelRenderer);
        }

        internal Result MoveResidentsThroughTunnel(
            IReadOnlyList<string> residentIds,
            SpatialCellId destination,
            DigTunnelDemoRenderer tunnelRenderer)
        {
            if (AgentSession == null || AgentRenderer == null || Hud == null)
            {
                return Result.Failure(new DomainError(
                    "unity.tunnel.not_initialized",
                    "The layered tunnel movement runtime is not initialized."));
            }

            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            if (tunnelRenderer == null)
            {
                throw new ArgumentNullException(nameof(tunnelRenderer));
            }

            MoveAgentsThroughTunnelReport report =
                AgentSession.MoveResidentsThroughTunnel(residentIds, destination);
            if (report.Result.IsFailure)
            {
                return report.Result;
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            AgentRenderer.Render(agents, movementDuration: 0.01f);
            for (int index = 0; index < report.Entries.Count; index++)
            {
                MoveAgentThroughTunnelEntry entry = report.Entries[index];
                AgentRenderer.AnimateRoute(
                    entry.AgentId.ToString(),
                    entry.Path.Cells,
                    stepDuration: 0.12f);
            }

            tunnelRenderer.ShowRoute(
                report.Entries.Count == 0 ? null : report.Entries[0].Path);
            RefreshEquipmentVisuals();
            Hud.SetAgents(agents, AgentSession.Tick);
            Hud.SetAgentSelection(
                AgentRenderer.SelectedModel,
                AgentRenderer.SelectedCount);
            return Result.Success();
        }
    }
}
