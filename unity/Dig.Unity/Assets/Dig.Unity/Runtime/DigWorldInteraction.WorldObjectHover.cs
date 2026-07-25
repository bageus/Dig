using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigVisualTintTarget[] _hoverTintTargets =
            Array.Empty<DigVisualTintTarget>();
        private Color[] _hoverBaseTints = Array.Empty<Color>();
        private Component? _hoveredWorldObject;

        private void UpdateWorldObjectHover()
        {
            Component? next = ResolveHoverTarget();
            if (!ReferenceEquals(_hoveredWorldObject, next))
            {
                RestoreHoverTints();
                _hoveredWorldObject = next;
                CaptureHoverTints(next);
            }

            ApplyHoverTints();
        }

        private Component? ResolveHoverTarget()
        {
            if (_hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return null;
            }

            RaycastHit[] hits = GetPointerHits();
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (_itemRenderer!.TryGetItem(hit, out DigWorldItemVisual item))
                {
                    return item.Model.IsBuildingBox ? item : null;
                }

                if (_buildingRenderer!.TryGetBuilding(hit, out DigBuildingVisual building))
                {
                    return building;
                }
            }

            return null;
        }

        private void CaptureHoverTints(Component? target)
        {
            if (target == null)
            {
                _hoverTintTargets = Array.Empty<DigVisualTintTarget>();
                _hoverBaseTints = Array.Empty<Color>();
                return;
            }

            _hoverTintTargets = target.GetComponentsInChildren<DigVisualTintTarget>(true);
            _hoverBaseTints = new Color[_hoverTintTargets.Length];
            for (int index = 0; index < _hoverTintTargets.Length; index++)
            {
                _hoverBaseTints[index] = _hoverTintTargets[index].CurrentTint;
            }
        }

        private void ApplyHoverTints()
        {
            for (int index = 0; index < _hoverTintTargets.Length; index++)
            {
                _hoverTintTargets[index].SetTint(
                    Color.Lerp(_hoverBaseTints[index], Color.white, 0.42f));
            }
        }

        private void RestoreHoverTints()
        {
            for (int index = 0; index < _hoverTintTargets.Length; index++)
            {
                if (_hoverTintTargets[index] != null)
                {
                    _hoverTintTargets[index].SetTint(_hoverBaseTints[index]);
                }
            }

            _hoverTintTargets = Array.Empty<DigVisualTintTarget>();
            _hoverBaseTints = Array.Empty<Color>();
        }
    }
}
