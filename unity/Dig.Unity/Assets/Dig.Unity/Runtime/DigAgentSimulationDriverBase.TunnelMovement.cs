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
            if (AgentSession == null || AgentRenderer == null || Hud == null)
            {
                return Result.Failure(new DomainError(
                    "unity.tunnel.not_initialized",
                    "The layered tunnel movement runtime is not initialized."));
            }

            if (tunnelRenderer == null)
            {
                throw new ArgumentNullException(nameof(tunnelRenderer));
            }

            MoveAgentThroughTunnelReport report =
                AgentSession.MoveResidentThroughTunnel(residentId, destination);
            if (report.Result.IsFailure)
            {
                return report.Result;
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            AgentRenderer.Render(agents, movementDuration: 0.01f);
            AgentRenderer.AnimateRoute(
                residentId,
                report.Path!.Cells,
                stepDuration: 0.12f);
            tunnelRenderer.ShowRoute(report.Path);
            RefreshEquipmentVisuals();
            DigAgentVisual? selected = AgentRenderer.SelectById(residentId);
            Hud.SetAgents(agents, AgentSession.Tick);
            Hud.SetAgentSelection(selected?.Model);
            return Result.Success();
        }
    }
}
