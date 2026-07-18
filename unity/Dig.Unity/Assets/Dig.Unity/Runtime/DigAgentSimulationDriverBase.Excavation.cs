using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        internal Result ApplyExcavationDesignation(
            CellId cell,
            bool active,
            int priority)
        {
            if (!IsInitialized())
            {
                return Result.Failure(new DomainError(
                    "unity.excavation.not_initialized",
                    "Excavation controls are not initialized."));
            }

            Result changed = WorldSession!.SetDesignation(cell, active);
            if (changed.IsFailure)
            {
                return changed;
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession!.LoadView();
            TerrainSession!.SynchronizeDesignations(CurrentTick, agents, priority);
            RefreshExcavationPresentation(agents);
            return Result.Success();
        }

        internal Result AssignExcavationCluster(CellId seed, string residentId)
        {
            if (!IsInitialized())
            {
                return Result.Failure(new DomainError(
                    "unity.excavation.not_initialized",
                    "Excavation controls are not initialized."));
            }

            AgentSession!.ReleaseManualTunnelOrder(residentId);
            Result result = TerrainSession!.AssignExcavationCluster(
                seed,
                residentId,
                CurrentTick);
            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            TerrainSession.SynchronizeDesignations(CurrentTick, agents);
            RefreshExcavationPresentation(agents);
            if (result.IsFailure)
            {
                return result;
            }

            DigAgentVisual? selected = AgentRenderer!.SelectById(residentId);
            Hud!.SetAgentSelection(selected?.Model);
            return Result.Success();
        }

        private void RefreshExcavationPresentation(
            IReadOnlyList<AgentViewModel> agents)
        {
            var world = WorldSession!.LoadView();
            IReadOnlyList<JobOverlayViewModel> jobs = TerrainSession!.LoadJobs();
            WorldRenderer!.Render(world);
            JobRenderer!.Render(jobs);
            Hud!.SetWorld(world);
            Hud.SetAgents(agents, AgentSession!.Tick);
            Hud.SetJobs(jobs);
        }
    }
}
