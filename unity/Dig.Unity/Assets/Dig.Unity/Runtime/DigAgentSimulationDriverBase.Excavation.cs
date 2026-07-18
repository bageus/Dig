using System;
using System.Collections.Generic;
using Dig.Application.World;
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

        internal Result ApplyCaveRoomPlan(CaveRoomPlan plan, int priority)
        {
            if (!IsInitialized())
            {
                return Result.Failure(new DomainError(
                    "unity.excavation.not_initialized",
                    "Excavation controls are not initialized."));
            }

            Result changed = WorldSession!.ApplyCaveRoomPlan(plan);
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
            return AssignExcavationCluster(seed, new[] { residentId });
        }

        internal Result AssignExcavationCluster(
            CellId seed,
            IReadOnlyList<string> residentIds)
        {
            if (!IsInitialized())
            {
                return Result.Failure(new DomainError(
                    "unity.excavation.not_initialized",
                    "Excavation controls are not initialized."));
            }

            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            for (int index = 0; index < residentIds.Count; index++)
            {
                AgentSession!.ReleaseManualTunnelOrder(residentIds[index]);
                TerrainSession!.ReleaseDirectMovementControl(residentIds[index]);
            }

            Result result = TerrainSession!.AssignExcavationClusterToResidents(
                seed,
                residentIds,
                CurrentTick);
            IReadOnlyList<AgentViewModel> agents = AgentSession!.LoadView();
            TerrainSession.SynchronizeDesignations(CurrentTick, agents);
            RefreshExcavationPresentation(agents);
            Hud!.SetAgentSelection(
                AgentRenderer!.SelectedModel,
                AgentRenderer.SelectedCount);
            return result;
        }

        private void RefreshExcavationPresentation(
            IReadOnlyList<AgentViewModel> agents)
        {
            SynchronizeExcavatedTunnelNavigation();
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
