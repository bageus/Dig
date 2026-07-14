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
    [DisallowMultipleComponent]
    public sealed class DigAgentSimulationDriver : MonoBehaviour
    {
        [SerializeField]
        private float tickIntervalSeconds = 0.8f;

        private DigWorldSession? _worldSession;
        private DigWorldRenderer? _worldRenderer;
        private DigAgentSession? _agentSession;
        private DigAgentRenderer? _agentRenderer;
        private DigTerrainWorkSession? _terrainSession;
        private DigJobRenderer? _jobRenderer;
        private DigWorldItemRenderer? _itemRenderer;
        private DigNavigationRouteRenderer? _routeRenderer;
        private DigHudOverlay? _hud;
        private float _elapsed;

        internal void Initialize(
            DigWorldSession worldSession,
            DigWorldRenderer worldRenderer,
            DigAgentSession agentSession,
            DigAgentRenderer agentRenderer,
            DigTerrainWorkSession terrainSession,
            DigJobRenderer jobRenderer,
            DigWorldItemRenderer itemRenderer,
            DigNavigationRouteRenderer routeRenderer,
            DigHudOverlay hud)
        {
            _worldSession = worldSession;
            _worldRenderer = worldRenderer;
            _agentSession = agentSession;
            _agentRenderer = agentRenderer;
            _terrainSession = terrainSession;
            _jobRenderer = jobRenderer;
            _itemRenderer = itemRenderer;
            _routeRenderer = routeRenderer;
            _hud = hud;
        }

        private void Update()
        {
            if (!IsInitialized())
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < tickIntervalSeconds)
            {
                return;
            }

            _elapsed -= tickIntervalSeconds;
            AdvanceOneTick();
        }

        private void AdvanceOneTick()
        {
            string? selectedAgentId = _agentRenderer!.SelectedAgentId;
            string? selectedJobId = _jobRenderer!.SelectedJobId;
            IReadOnlyList<AgentViewModel> before = _agentSession!.LoadView();
            IReadOnlyDictionary<string, CellId> movement =
                _terrainSession!.PlanMovement(before);
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
            if (_terrainSession.ConsumeWorldChanged())
            {
                WorldViewModel world = _worldSession!.LoadView();
                _worldRenderer!.Render(world);
                _hud!.SetWorld(world);
            }

            _agentRenderer.Render(agents, tickIntervalSeconds * 0.82f);
            _jobRenderer.Render(jobs);
            _itemRenderer!.Render(items);
            _routeRenderer!.Render(routes);
            _hud!.SetAgents(agents, _agentSession.Tick);
            _hud.SetJobs(jobs);
            RestoreSelection(selectedAgentId, selectedJobId);
        }

        private void RestoreSelection(string? selectedAgentId, string? selectedJobId)
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
                && _itemRenderer != null
                && _routeRenderer != null
                && _hud != null;
        }

        private void OnValidate()
        {
            tickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds);
        }
    }
}
