using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private void UpdateResidentWorldHover()
        {
            if (_agentRenderer == null
                || _camera == null
                || _hud == null
                || _hud.ContainsScreenPoint(Input.mousePosition))
            {
                _agentRenderer?.SetHovered(null);
                return;
            }

            RaycastHit[] hits = GetPointerHits();
            _agentRenderer.SetHovered(
                TryResolveAgentHit(hits, out DigAgentVisual agent) ? agent : null);
        }
    }
}
