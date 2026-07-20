using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using Dig.Presentation.Navigation;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        protected virtual void Update()
        {
            if (!IsInitialized())
            {
                return;
            }

            HandlePlaybackInput();
            int dueTicks = Playback.ConsumeDueTicks(
                Time.unscaledDeltaTime,
                TickIntervalSeconds,
                MaximumTicksPerFrame);
            for (int index = 0; index < dueTicks && enabled; index++)
            {
                AdvanceOneTick();
            }
        }

        private void HandlePlaybackInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }

            if (Input.GetKeyDown(KeyCode.Period)
                || Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                StepOnce();
            }

            if (Input.GetKeyDown(KeyCode.Minus)
                || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                Playback.SlowDown();
            }

            if (Input.GetKeyDown(KeyCode.Equals)
                || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                Playback.SpeedUp();
            }
        }

        private void AdvanceOneTick()
        {
            IReadOnlyList<string> selectedAgentIds = AgentRenderer!.SelectedAgentIds;
            string? primarySelectedAgentId = AgentRenderer.SelectedAgentId;
            string? selectedJobId = JobRenderer!.SelectedJobId;
            string? selectedBuildingId = BuildingRenderer!.SelectedBuildingId;
            IReadOnlyList<AgentViewModel> before = AgentSession!.LoadView();
            long nextTick = checked(AgentSession.Tick + 1);
            Result result = TerrainSession!.EnforceDirectMovementOwnership(nextTick);
            if (result.IsSuccess)
            {
                TerrainSession.SynchronizeDesignations(nextTick, before);
                TerrainSession.SynchronizeBuildingBoxAssembly(nextTick, before);
                TerrainSession.SynchronizeBuildingPacking(nextTick, before);
                IReadOnlyDictionary<string, CellId> movement =
                    TerrainSession.PlanMovement(before);
                IReadOnlyDictionary<string, SpatialCellId> spatialMovement =
                    TerrainSession.PlanSpatialExcavationMovement(before);
                AgentSession.SetSpatialWorkMovementTargets(spatialMovement);
                result = AgentSession.Advance(movement);
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            if (result.IsSuccess)
            {
                result = TerrainSession.AdvanceSpatialExcavationWork(
                    AgentSession.Tick,
                    agents);
            }

            if (result.IsSuccess)
            {
                IReadOnlyList<SpatialExcavationCommit> commits =
                    TerrainSession.LoadSpatialExcavationsToFinalize();
                for (int index = 0; index < commits.Count && result.IsSuccess; index++)
                {
                    result = CompleteSpatialExcavation(commits[index]);
                }
            }

            if (result.IsSuccess)
            {
                result = AdvanceTerrainForAgents(AgentSession.Tick, agents);
            }

            if (result.IsSuccess)
            {
                result = TerrainSession.AdvanceBuildingBoxPickup(AgentSession.Tick, agents);
            }

            if (result.IsSuccess)
            {
                result = TerrainSession.AdvanceWorldItemPickup(AgentSession.Tick, agents);
            }

            if (result.IsSuccess)
            {
                result = TerrainSession.AdvanceBuildingBoxAssembly(AgentSession.Tick, agents);
            }

            if (result.IsSuccess)
            {
                result = TerrainSession.AdvanceBuildingPacking(AgentSession.Tick, agents);
            }

            if (result.IsFailure)
            {
                Hud!.SetCommandResult(result);
                enabled = false;
                return;
            }

            IReadOnlyList<JobOverlayViewModel> jobs = TerrainSession.LoadJobs();
            IReadOnlyList<WorldItemViewModel> items = TerrainSession.LoadAllWorldItems();
            IReadOnlyList<RouteViewModel> routes = TerrainSession.LoadRoutes();
            DigStorageStatus storage = TerrainSession.GetStorageStatus();
            if (TerrainSession.ConsumeWorldChanged())
            {
                WorldViewModel world = WorldSession!.LoadView();
                WorldRenderer!.Render(world);
                WorldRenderer.SetProtectedCells(WorldSession.ProtectedCells);
                WorldRenderer.SetTerrainDeposits(WorldSession.LoadTerrainDeposits());
                Hud!.SetWorld(world);
            }

            float movementDuration = TickIntervalSeconds * 0.82f;
            if (!Playback.IsPaused)
            {
                movementDuration /= Playback.SpeedMultiplier;
            }

            AgentRenderer.Render(agents, movementDuration);
            RefreshEquipmentVisuals();
            JobRenderer.Render(jobs);
            BuildingRenderer.Render(TerrainSession.LoadBuildings());
            ItemRenderer!.Render(items);
            StockpileRenderer!.Render(storage);
            RouteRenderer!.Render(routes);
            Hud!.SetAgents(agents, AgentSession.Tick);
            Hud.SetJobs(jobs);
            Hud.SetStorageStatus(storage);
            RestoreSelection(
                selectedAgentIds,
                primarySelectedAgentId,
                selectedJobId,
                selectedBuildingId);
        }
    }
}