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
            else
            {
                RefreshHoverTintsIfStale(next);
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
                    return item;
                }

                if (_mushroomRenderer != null
                    && _mushroomRenderer.TryGetMushroom(hit, out DigMushroomVisual mushroom))
                {
                    return mushroom;
                }

                if (_barrelRenderer != null
                    && _barrelRenderer.TryGetBarrel(hit, out DigBarrelVisual barrel))
                {
                    return barrel;
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
                DigVisualTintTarget tint = _hoverTintTargets[index];
                if (tint != null)
                {
                    _hoverBaseTints[index] = tint.CurrentTint;
                }
            }
        }

        private void RefreshHoverTintsIfStale(Component? target)
        {
            if (!HasStaleHoverTints())
            {
                return;
            }

            RestoreHoverTints();
            CaptureHoverTints(target);
        }

        private bool HasStaleHoverTints()
        {
            if (_hoverTintTargets.Length != _hoverBaseTints.Length)
            {
                return true;
            }

            for (int index = 0; index < _hoverTintTargets.Length; index++)
            {
                if (_hoverTintTargets[index] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyHoverTints()
        {
            int count = Math.Min(_hoverTintTargets.Length, _hoverBaseTints.Length);
            for (int index = 0; index < count; index++)
            {
                DigVisualTintTarget tint = _hoverTintTargets[index];
                if (tint == null)
                {
                    continue;
                }

                tint.SetTint(
                    Color.Lerp(_hoverBaseTints[index], Color.white, 0.42f));
            }
        }

        private void RestoreHoverTints()
        {
            int count = Math.Min(_hoverTintTargets.Length, _hoverBaseTints.Length);
            for (int index = 0; index < count; index++)
            {
                DigVisualTintTarget tint = _hoverTintTargets[index];
                if (tint != null)
                {
                    tint.SetTint(_hoverBaseTints[index]);
                }
            }

            _hoverTintTargets = Array.Empty<DigVisualTintTarget>();
            _hoverBaseTints = Array.Empty<Color>();
        }
    }
}
