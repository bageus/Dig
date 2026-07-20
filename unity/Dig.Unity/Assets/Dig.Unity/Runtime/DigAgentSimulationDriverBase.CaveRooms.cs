using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private readonly HashSet<string> _activatedCaveRooms =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<SpatialCellId> _terrainExcavatedVolume =
            new HashSet<SpatialCellId>();
        private readonly CaveTemplateTrimPresenter _caveTemplateTrimPresenter =
            new CaveTemplateTrimPresenter();

        internal TunnelDepthExcavationPlanResult ExcavateTunnelDepth(
            SpatialCellId source,
            DigTunnelDemoRenderer tunnelRenderer)
        {
            if (AgentSession == null)
            {
                throw new InvalidOperationException(
                    "Tunnel depth excavation is not initialized.");
            }

            if (tunnelRenderer == null)
            {
                throw new ArgumentNullException(nameof(tunnelRenderer));
            }

            TunnelDepthExcavationPlanResult result =
                AgentSession.ExcavateTunnelDepth(source);
            if (result.Succeeded)
            {
                SpatialCellId target = result.Plan!.Target;
                _terrainExcavatedVolume.Add(target);
                tunnelRenderer.Initialize(AgentSession.TunnelVolume);
                tunnelRenderer.SetDepthExcavationSources(
                    AgentSession.TunnelDepthExcavations);
                RefreshTerrainDepthVolume();
                if (tunnelRenderer.TryGetCell(target, out DigTunnelCellVisual visual))
                {
                    tunnelRenderer.Select(visual);
                }
            }

            return result;
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
