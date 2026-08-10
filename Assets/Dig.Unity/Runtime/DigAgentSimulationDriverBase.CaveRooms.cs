using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;
using Dig.Presentation.World;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private const int DefaultSpatialExcavationPriority = 750;
        private readonly HashSet<string> _activatedCaveRooms =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly CaveTemplateTrimPresenter _caveTemplateTrimPresenter =
            new CaveTemplateTrimPresenter();

        internal Result DesignateTunnelDepth(CellId source)
        {
            if (AgentSession == null || TerrainSession == null || WorldSession == null)
            {
                return Result.Failure(new DomainError(
                    "unity.depth.not_initialized",
                    "Spatial excavation is not initialized."));
            }

            TunnelDepthExcavationPlanResult planned =
                AgentSession.PlanTunnelDepthExcavation(source);
            if (!planned.Succeeded)
            {
                return Result.Failure(new DomainError(
                    $"unity.depth.{planned.FailureReason.ToString().ToLowerInvariant()}",
                    planned.Detail));
            }

            TunnelDepthExcavationPlan plan = planned.Plan!;
            Result worldDesignation = WorldSession.SetDesignation(
                plan.Target,
                active: true);
            if (worldDesignation.IsFailure)
            {
                return worldDesignation;
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            Result designated = TerrainSession.DesignateSpatialExcavation(
                plan,
                agents,
                DefaultSpatialExcavationPriority,
                CurrentTick);
            if (designated.IsFailure)
            {
                Result rollback = WorldSession.SetDesignation(
                    plan.Target,
                    active: false);
                return rollback.IsFailure ? rollback : designated;
            }

            IReadOnlyList<JobOverlayViewModel> jobs = TerrainSession.LoadJobs();
            JobRenderer!.Render(jobs);
            Hud!.SetJobs(jobs);
            return Result.Success();
        }

        private Result CompleteSpatialExcavation(SpatialExcavationCommit commit)
        {
            Result world = WorldSession!.ExcavateSpatialCell(commit.Target);
            if (world.IsFailure)
            {
                return world;
            }

            TerrainSession!.MarkAuthoritativeWorldChanged();
            Result terrain = AgentSession!.CompleteTunnelDepthExcavation(
                commit.Target,
                WorldSession.LoadSnapshot(),
                WorldSession.PlannedTunnelCells,
                WorldSession.PlannedVerticalTunnelCells);
            if (terrain.IsFailure)
            {
                return terrain;
            }

            Result job = TerrainSession!.CompleteSpatialExcavationJob(
                commit.JobId,
                CurrentTick);
            if (job.IsFailure)
            {
                return job;
            }

            WorldRenderer!.RemoveDepthDesignationTint(commit.Target);
            WorldRenderer!.Render(WorldSession.LoadView());
            PresentSpatialExcavationEffect(commit.Target, CurrentTick);
            DigTunnelDemoRenderer renderer = GetComponent<DigTunnelDemoRenderer>();
            renderer.Initialize(AgentSession.TunnelVolume);
            renderer.SetDepthExcavationSources(AgentSession.TunnelDepthExcavations);
            if (renderer.TryGetCell(commit.Target, out DigTunnelCellVisual visual))
            {
                renderer.Select(visual);
            }

            return Result.Success();
        }

        internal void RefreshCaveRoomRuntime(
            IReadOnlyList<CaveRoomPlan> completedPlans,
            DigCaveRoomFloorRenderer floorRenderer)
        {
            if (completedPlans == null)
            {
                throw new ArgumentNullException(nameof(completedPlans));
            }

            if (floorRenderer == null)
            {
                throw new ArgumentNullException(nameof(floorRenderer));
            }

            if (AgentSession == null || WorldRenderer == null)
            {
                return;
            }

            for (int index = 0; index < completedPlans.Count; index++)
            {
                CaveRoomPlan plan = completedPlans[index];
                string key = CreateCaveRoomKey(plan);
                if (!_activatedCaveRooms.Add(key))
                {
                    continue;
                }

                AgentSession.SynchronizeNavigation(
                    WorldSession!.LoadSnapshot(),
                    WorldSession.PlannedTunnelCells,
                    WorldSession.PlannedVerticalTunnelCells);
                floorRenderer.AddRoomFloor(plan);
            }

            WorldRenderer.SetCaveTemplateTrims(
                _caveTemplateTrimPresenter.Present(completedPlans));
            WorldRenderer.Render(WorldSession!.LoadView());
        }

        internal static CellId[] CreateFloorCells(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            CaveRoomRowProfile baseProfile = CaveRoomPlanner.ResolveRowProfile(
                plan.Preset,
                plan.Entrance.X,
                level: 0);
            CellId[] cells = new CellId[
                baseProfile.RequiredQuartersByX.Count * plan.Preset.Depth];
            int index = 0;
            for (int z = 0; z < plan.Preset.Depth; z++)
            {
                foreach (int x in baseProfile.RequiredQuartersByX.Keys.OrderBy(value => value))
                {
                    cells[index++] = new CellId(
                        x,
                        plan.Entrance.Y,
                        z);
                }
            }

            return cells;
        }

        private static string CreateCaveRoomKey(CaveRoomPlan plan)
        {
            return $"{plan.Entrance.X}:{plan.Entrance.Y}:{plan.Preset.Kind}";
        }
    }
}
