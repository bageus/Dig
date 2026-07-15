using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using Dig.Presentation.Navigation;
using Dig.Presentation.Runtime;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentSimulationDriver : MonoBehaviour
    {
        private const int MaximumTicksPerFrame = 8;
        private static readonly DomainError NotInitialized = new DomainError(
            "unity.agent_simulation.not_initialized",
            "The resident simulation driver is not initialized.");

        [SerializeField]
        private float tickIntervalSeconds = 0.8f;

        private DigWorldSession? _worldSession;
        private DigWorldRenderer? _worldRenderer;
        private DigAgentSession? _agentSession;
        private DigAgentRenderer? _agentRenderer;
        private DigTerrainWorkSession? _terrainSession;
        private DigJobRenderer? _jobRenderer;
        private DigBuildingRenderer? _buildingRenderer;
        private DigWorldItemRenderer? _itemRenderer;
        private DigStockpileRenderer? _stockpileRenderer;
        private DigNavigationRouteRenderer? _routeRenderer;
        private DigHudOverlay? _hud;
        private SimulationPlaybackState? _playback;

        internal bool IsPaused => Playback.IsPaused;

        internal string PlaybackLabel => Playback.Label;

        internal long CurrentTick => _agentSession?.Tick ?? 0;

        private SimulationPlaybackState Playback =>
            _playback ??= new SimulationPlaybackState();

        internal void Initialize(
            DigWorldSession worldSession,
            DigWorldRenderer worldRenderer,
            DigAgentSession agentSession,
            DigAgentRenderer agentRenderer,
            DigTerrainWorkSession terrainSession,
            DigJobRenderer jobRenderer,
            DigBuildingRenderer buildingRenderer,
            DigWorldItemRenderer itemRenderer,
            DigStockpileRenderer stockpileRenderer,
            DigNavigationRouteRenderer routeRenderer,
            DigHudOverlay hud)
        {
            _worldSession = worldSession;
            _worldRenderer = worldRenderer;
            _agentSession = agentSession;
            _agentRenderer = agentRenderer;
            _terrainSession = terrainSession;
            _jobRenderer = jobRenderer;
            _buildingRenderer = buildingRenderer;
            _itemRenderer = itemRenderer;
            _stockpileRenderer = stockpileRenderer;
            _routeRenderer = routeRenderer;
            _hud = hud;
        }

        internal void TogglePause()
        {
            Playback.TogglePause();
        }

        internal void StepOnce()
        {
            Playback.StepOnce();
        }

        internal void SetSpeed(SimulationPlaybackSpeed speed)
        {
            Playback.SetSpeed(speed);
        }

        internal Result MoveResident(string residentId, CellId destination)
        {
            if (_agentSession == null || _agentRenderer == null || _hud == null)
            {
                return Result.Failure(NotInitialized);
            }

            Result result = _agentSession.MoveResident(residentId, destination);
            if (result.IsFailure)
            {
                return result;
            }

            IReadOnlyList<AgentViewModel> agents = _agentSession.LoadView();
            _agentRenderer.Render(agents, movementDuration: 0.25f);
            DigAgentVisual? selected = _agentRenderer.SelectById(residentId);
            _hud.SetAgents(agents, _agentSession.Tick);
            _hud.SetAgentSelection(selected?.Model);
            return Result.Success();
        }

        private void Update()
        {
            if (!IsInitialized())
            {
                return;
            }

            HandlePlaybackInput();
            int dueTicks = Playback.ConsumeDueTicks(
                Time.unscaledDeltaTime,
                tickIntervalSeconds,
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
            string? selectedAgentId = _agentRenderer!.SelectedAgentId;
            string? selectedJobId = _jobRenderer!.SelectedJobId;
            string? selectedBuildingId = _buildingRenderer!.SelectedBuildingId;
            IReadOnlyList<AgentViewModel> before = _agentSession!.LoadView();
            long nextTick = checked(_agentSession.Tick + 1);
            _terrainSession!.SynchronizeDesignations(nextTick, before);
            _terrainSession.SynchronizeHauling(nextTick, before);
            IReadOnlyDictionary<string, CellId> movement =
                _terrainSession.PlanMovement(before);
            Result result = _agentSession.Advance(movement);
            IReadOnlyList<AgentViewModel> agents = _agentSession.LoadView();
            if (result.IsSuccess)
            {
                result = _terrainSession.Advance(_agentSession.Tick, agents);
            }

            if (result.IsFailure)
            {
                _hud!.SetCommandResult(result);
                enabled = false;
                return;
            }

            IReadOnlyList<JobOverlayViewModel> jobs = _terrainSession.LoadJobs();
            IReadOnlyList<WorldItemViewModel> items = _terrainSession.LoadItems();
            IReadOnlyList<RouteViewModel> routes = _terrainSession.LoadRoutes();
            DigStorageStatus storage = _terrainSession.GetStorageStatus();
            if (_terrainSession.ConsumeWorldChanged())
            {
                WorldViewModel world = _worldSession!.LoadView();
                _worldRenderer!.Render(world);
                _hud!.SetWorld(world);
            }

            float movementDuration = tickIntervalSeconds * 0.82f;
            if (!Playback.IsPaused)
            {
                movementDuration /= Playback.SpeedMultiplier;
            }

            _agentRenderer.Render(agents, movementDuration);
            _jobRenderer.Render(jobs);
            _buildingRenderer.Render(_terrainSession.LoadBuildings());
            _itemRenderer!.Render(items);
            _stockpileRenderer!.Render(storage);
            _routeRenderer!.Render(routes);
            _hud!.SetAgents(agents, _agentSession.Tick);
            _hud.SetJobs(jobs);
            _hud.SetStorageStatus(storage);
            RestoreSelection(selectedAgentId, selectedJobId, selectedBuildingId);
        }

        private void RestoreSelection(
            string? selectedAgentId,
            string? selectedJobId,
            string? selectedBuildingId)
        {
            if (selectedAgentId != null)
            {
                DigAgentVisual? selectedAgent = _agentRenderer!.SelectById(selectedAgentId);
                _hud!.SetAgentSelection(selectedAgent?.Model);
                return;
            }

            if (selectedJobId != null)
            {
                DigJobVisual? selectedJob = _jobRenderer!.SelectById(selectedJobId);
                _hud!.SetJobSelection(selectedJob?.Model);
                return;
            }

            if (selectedBuildingId != null)
            {
                DigBuildingVisual? selectedBuilding =
                    _buildingRenderer!.SelectById(selectedBuildingId);
                _hud!.SetBuildingSelection(selectedBuilding?.Model);
            }
        }

        private bool IsInitialized()
        {
            return _worldSession != null
                && _worldRenderer != null
                && _agentSession != null
                && _agentRenderer != null
                && _terrainSession != null
                && _jobRenderer != null
                && _buildingRenderer != null
                && _itemRenderer != null
                && _stockpileRenderer != null
                && _routeRenderer != null
                && _hud != null;
        }

        private void OnValidate()
        {
            tickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds);
        }
    }
}
