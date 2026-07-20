using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;

namespace Dig.Unity
{

public abstract partial class DigAgentSimulationDriverBase
{
    internal bool TryAssignSpatialExcavation(
        SpatialCellId workCell,
        IReadOnlyList<string> residentIds,
        out Result result)
    {
        if (AgentSession == null
            || TerrainSession == null
            || AgentRenderer == null
            || JobRenderer == null
            || Hud == null)
        {
            result = Result.Failure(new DomainError(
                "unity.spatial_excavation.not_initialized",
                "Spatial excavation controls are not initialized."));
            return true;
        }

        bool found = TerrainSession.TryAssignSpatialExcavation(
            workCell,
            residentIds,
            CurrentTick,
            out result);
        if (!found || result.IsFailure)
        {
            return found;
        }

        for (int index = 0; index < residentIds.Count; index++)
        {
            AgentSession.ReleaseManualTunnelOrder(residentIds[index]);
            TerrainSession.ReleaseDirectMovementControl(residentIds[index]);
        }

        IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
        IReadOnlyList<JobOverlayViewModel> jobs = TerrainSession.LoadJobs();
        JobRenderer.Render(jobs);
        Hud.SetAgents(agents, AgentSession.Tick);
        Hud.SetJobs(jobs);
        Hud.SetAgentSelection(AgentRenderer.SelectedModel, AgentRenderer.SelectedCount);
        return true;
    }
}

}