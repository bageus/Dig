using System;
using System.Collections.Generic;
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
        private readonly HashSet<SpatialCellId> _terrainExcavatedVolume =
            new HashSet<SpatialCellId>();
        private readonly CaveTemplateTrimPresenter _caveTemplateTrimPresenter =
            new CaveTemplateTrimPresenter();

        internal Result DesignateTunnelDepth(SpatialCellId source)
        {
            if (AgentSession == null || TerrainSession == null)
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

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            Result designated = TerrainSession.DesignateSpatialExcavation(
                planned.Plan!,
                agents,
                DefaultSpatialExcavationPriority,
                CurrentTick);
            if (designated.IsFailure)
            {
                return designated;
            }

            IReadOnlyList<JobOverlayViewModel> jobs = TerrainSession.LoadJobs();
            JobRenderer!.Render(jobs);
            Hud!.SetJobs(jobs);
            return Result.Success();
        }

        private Result CompleteSpatialExcavation(SpatialExcavationCommit commit)
        {
            Result terrain = AgentSession!.CompleteTunnelDepthExcavation(commit.Target);
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

            _terrainExcavatedVolume.Add(commit.Target);
            PresentSpatialExcavationEffect(commit.Target, CurrentTick);
            DigTunnelDemoRenderer renderer = GetComponent<DigTunnelDemoRenderer>();
            renderer.Initialize(AgentSession.TunnelVolume);
            renderer.SetDepthExcavationSources(AgentSession.TunnelDepthExcavations);
            RefreshTerrainDepthVolume();
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

            _terrainExcavatedVolume.Clear();
            _terrainExcavatedVolume.UnionWith(AgentSession.TunnelDepthExcavations);
            for (int index = 0; index < completedPlans.Count; index++)
            {
                CaveRoomPlan plan = completedPlans[index];
                for (int cellIndex = 0; cellIndex < plan.VolumeCells.Count; cellIndex++)
                {
                    SpatialCellId cell = plan.VolumeCells[cellIndex];
                    if (cell.Z > 0)
                    {
                        _terrainExcavatedVolume.Add(cell);
                    }
                }

                string key = CreateCaveRoomKey(plan);
                if (!_activatedCaveRooms.Add(key))
                {
                    continue;
                }

                SpatialCellId[] floorCells = CreateFloorCells(plan);
                AgentSession.ExpandTunnelVolume(floorCells);
                floorRenderer.AddRoomFloor(plan);
            }

            WorldRenderer.SetCaveTemplateTrims(
                _caveTemplateTrimPresenter.Present(completedPlans));
            RefreshTerrainDepthVolume();
        }

        internal static SpatialCellId[] CreateFloorCells(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            int minX = plan.Entrance.X - ((plan.Preset.BaseWidth - 1) / 2);
            SpatialCellId[] cells = new SpatialCellId[
                plan.Preset.BaseWidth * plan.Preset.Depth];
            int index = 0;
            for (int z = 0; z < plan.Preset.Depth; z++)
            {
                for (int offset = 0; offset < plan.Preset.BaseWidth; offset++)
                {
                    cells[index++] = new SpatialCellId(
                        minX + offset,
                        plan.Entrance.Y,
                        z);
                }
            }

            return cells;
        }

        private void RefreshTerrainDepthVolume()
        {
            if (AgentSession == null || WorldSession == null || WorldRenderer == null)
            {
                return;
            }

            WorldRenderer.SetTerrainDepthVolume(
                AgentSession.TunnelVolume,
                WorldSession.SolidMaterialId.ToString(),
                WorldSession.SolidHardness,
                _terrainExcavatedVolume);
        }

        private static string CreateCaveRoomKey(CaveRoomPlan plan)
        {
            return $"{plan.Entrance.X}:{plan.Entrance.Y}:{plan.Preset.Kind}";
        }
    }
}