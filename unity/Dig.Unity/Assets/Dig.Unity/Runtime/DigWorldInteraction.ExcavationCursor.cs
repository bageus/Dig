using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigExcavationCursorRenderer? _excavationCursorRenderer;

        private void LateUpdate()
        {
            SynchronizeExcavationDesignations();
            UpdateExcavationCursorPreview();
            UpdateSelectedResidentCommandCursor();
        }

        private void SynchronizeExcavationDesignations()
        {
            if (!IsInitialized())
            {
                return;
            }

            EnsureExcavationCursorRenderer();
            _excavationCursorRenderer!.SynchronizeTunnelDesignations(
                _session!.LoadView());
        }

        private void UpdateExcavationCursorPreview()
        {
            if (!IsInitialized())
            {
                _renderer?.SetDepthDesignationInteractionActive(active: false);
                _excavationCursorRenderer?.Hide();
                return;
            }

            bool erase = _excavationMode == DigExcavationDrawingMode.Delete;
            bool directDepthCommand = _excavationMode == DigExcavationDrawingMode.None
                && !_caveRoomPreset.HasValue
                && _agentRenderer != null
                && _agentRenderer.SelectedCount > 0;
            _renderer!.SetDepthDesignationInteractionActive(
                (erase && !_caveRoomPreset.HasValue) || directDepthCommand);
            if (_hud!.ContainsScreenPoint(Input.mousePosition)
                || _buildingPlacementMode.HasValue
                || _caveRoomPreset.HasValue
                || _excavationMode == DigExcavationDrawingMode.None)
            {
                _excavationCursorRenderer?.Hide();
                return;
            }

            CellId? target = erase
                ? ResolveExcavationEraseTarget()
                : _excavationMode == DigExcavationDrawingMode.Depth
                    ? ResolveDepthCursorTarget()
                    : ResolveTunnelCursorTarget();
            if (!target.HasValue && erase)
            {
                target = ProjectPointerToLayer(0);
            }

            if (!target.HasValue)
            {
                _excavationCursorRenderer?.Hide();
                return;
            }

            EnsureExcavationCursorRenderer();
            _excavationCursorRenderer!.Show(
                target.Value,
                _excavationMode == DigExcavationDrawingMode.Depth,
                erase);
        }

        private CellId? ResolveTunnelCursorTarget()
        {
            RaycastHit[] hits = GetPointerHits();
            for (int index = 0; index < hits.Length; index++)
            {
                CellId? target = ResolveExcavationPaintTarget(hits[index]);
                if (target.HasValue)
                {
                    return target;
                }
            }

            return ProjectPointerToLayer(0);
        }

        private CellId? ResolveDepthCursorTarget()
        {
            CellId? source = ResolveTunnelDepthSource() ?? ProjectPointerToLayer(0);
            return source.HasValue
                ? new CellId(source.Value.X, source.Value.Y, source.Value.Z + 1)
                : (CellId?)null;
        }

        private CellId? ProjectPointerToLayer(int z)
        {
            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            float depth = DigTunnelProjection.CellWorldPosition(new CellId(0, 0, z)).z;
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, depth));
            if (!plane.Raycast(ray, out float distance))
            {
                return null;
            }

            Vector3 point = ray.GetPoint(distance);
            return new CellId(
                Mathf.RoundToInt(point.x),
                Mathf.RoundToInt(-point.y),
                z);
        }

        private void EnsureExcavationCursorRenderer()
        {
            if (_excavationCursorRenderer != null)
            {
                return;
            }

            _excavationCursorRenderer = GetComponent<DigExcavationCursorRenderer>()
                ?? gameObject.AddComponent<DigExcavationCursorRenderer>();
            DigOverlayManager overlays = GetComponent<DigOverlayManager>()
                ?? gameObject.AddComponent<DigOverlayManager>();
            _excavationCursorRenderer.Initialize(overlays);
        }
    }
}
