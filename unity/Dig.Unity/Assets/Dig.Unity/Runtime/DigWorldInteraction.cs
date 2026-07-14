using Dig.Domain.Core;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldInteraction : MonoBehaviour
    {
        private Camera? _camera;
        private DigWorldSession? _session;
        private DigWorldRenderer? _renderer;
        private DigAgentRenderer? _agentRenderer;
        private DigHudOverlay? _hud;

        internal void Initialize(
            Camera targetCamera,
            DigWorldSession session,
            DigWorldRenderer renderer,
            DigAgentRenderer agentRenderer,
            DigHudOverlay hud)
        {
            _camera = targetCamera;
            _session = session;
            _renderer = renderer;
            _agentRenderer = agentRenderer;
            _hud = hud;
        }

        private void Update()
        {
            if (_camera == null
                || _session == null
                || _renderer == null
                || _agentRenderer == null
                || _hud == null)
            {
                return;
            }

            bool select = Input.GetMouseButtonDown(0);
            bool updateCell = Input.GetMouseButtonDown(1);
            if (!select && !updateCell)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                return;
            }

            if (_agentRenderer.TryGetAgent(hit, out DigAgentVisual agent))
            {
                if (select)
                {
                    _renderer.Select(null);
                    _agentRenderer.Select(agent);
                    _hud.SetAgentSelection(agent.Model);
                }

                return;
            }

            if (!_renderer.TryGetCell(hit, out DigCellVisual cell))
            {
                return;
            }

            _agentRenderer.Select(null);
            _renderer.Select(cell);
            _hud.SetSelection(cell.Model);
            if (updateCell)
            {
                ToggleDesignation(cell.Model);
            }
        }

        private void ToggleDesignation(WorldCellViewModel selected)
        {
            if (_session == null || _renderer == null || _hud == null)
            {
                return;
            }

            Result result = _session.ToggleDesignation(selected);
            _hud.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            WorldViewModel world = _session.LoadView();
            _renderer.Render(world);
            DigCellVisual? refreshed = _renderer.SelectAt(selected.X, selected.Y);
            _hud.SetWorld(world);
            _hud.SetSelection(refreshed == null ? null : refreshed.Model);
        }
    }
}