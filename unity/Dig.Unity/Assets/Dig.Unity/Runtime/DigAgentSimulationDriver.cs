using Dig.Domain.Core;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentSimulationDriver : MonoBehaviour
    {
        [SerializeField]
        private float tickIntervalSeconds = 0.8f;

        private DigAgentSession? _session;
        private DigAgentRenderer? _renderer;
        private DigHudOverlay? _hud;
        private float _elapsed;

        internal void Initialize(
            DigAgentSession session,
            DigAgentRenderer renderer,
            DigHudOverlay hud)
        {
            _session = session;
            _renderer = renderer;
            _hud = hud;
        }

        private void Update()
        {
            if (_session == null || _renderer == null || _hud == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < tickIntervalSeconds)
            {
                return;
            }

            _elapsed -= tickIntervalSeconds;
            string? selectedId = _renderer.SelectedAgentId;
            Result result = _session.Advance();
            if (result.IsFailure)
            {
                _hud.SetCommandResult(result);
                enabled = false;
                return;
            }

            var agents = _session.LoadView();
            _renderer.Render(agents, tickIntervalSeconds * 0.82f);
            _hud.SetAgents(agents, _session.Tick);
            if (selectedId != null)
            {
                DigAgentVisual? selected = _renderer.SelectById(selectedId);
                _hud.SetAgentSelection(selected?.Model);
            }
        }

        private void OnValidate()
        {
            tickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds);
        }
    }
}