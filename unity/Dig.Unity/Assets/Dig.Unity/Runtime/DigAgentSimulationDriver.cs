using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentSimulationDriver : MonoBehaviour
    {
        [SerializeField]
        private float tickIntervalSeconds = 0.8f;

        private DigAgentSession? _agentSession;
        private DigAgentRenderer? _agentRenderer;
        private DigJobSession? _jobSession;
        private DigJobRenderer? _jobRenderer;
        private DigHudOverlay? _hud;
        private float _elapsed;

        internal void Initialize(
            DigAgentSession agentSession,
            DigAgentRenderer agentRenderer,
            DigJobSession jobSession,
            DigJobRenderer jobRenderer,
            DigHudOverlay hud)
        {
            _agentSession = agentSession;
            _agentRenderer = agentRenderer;
            _jobSession = jobSession;
            _jobRenderer = jobRenderer;
            _hud = hud;
        }

        private void Update()
        {
            if (_agentSession == null
                || _agentRenderer == null
                || _jobSession == null
                || _jobRenderer == null
                || _hud == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < tickIntervalSeconds)
            {
                return;
            }

            _elapsed -= tickIntervalSeconds;
            string? selectedAgentId = _agentRenderer.SelectedAgentId;
            string? selectedJobId = _jobRenderer.SelectedJobId;
            Result result = _agentSession.Advance();
            if (result.IsSuccess)
            {
                result = _jobSession.Advance(_agentSession.Tick);
            }

            if (result.IsFailure)
            {
                _hud.SetCommandResult(result);
                enabled = false;
                return;
            }

            IReadOnlyList<AgentViewModel> agents = _agentSession.LoadView();
            IReadOnlyList<JobOverlayViewModel> jobs = _jobSession.LoadView();
            _agentRenderer.Render(agents, tickIntervalSeconds * 0.82f);
            _jobRenderer.Render(jobs);
            _hud.SetAgents(agents, _agentSession.Tick);
            _hud.SetJobs(jobs);
            if (selectedAgentId != null)
            {
                DigAgentVisual? selectedAgent = _agentRenderer.SelectById(selectedAgentId);
                _hud.SetAgentSelection(selectedAgent?.Model);
            }
            else if (selectedJobId != null)
            {
                DigJobVisual? selectedJob = _jobRenderer.SelectById(selectedJobId);
                _hud.SetJobSelection(selectedJob?.Model);
            }
        }

        private void OnValidate()
        {
            tickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds);
        }
    }
}