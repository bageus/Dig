using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const float MarqueeThresholdPixels = 8f;
        private DigSelectionMarqueeRenderer? _marqueeRenderer;
        private bool _marqueePending;
        private bool _marqueeActive;
        private Vector2 _marqueeStart;
        private Vector2 _marqueeCurrent;

        private void InitializeResidentMarquee()
        {
            _marqueeRenderer = GetComponent<DigSelectionMarqueeRenderer>();
            if (_marqueeRenderer == null)
            {
                _marqueeRenderer = gameObject.AddComponent<DigSelectionMarqueeRenderer>();
            }
        }

        private bool TryHandleResidentMarqueeSelection()
        {
            if (!CanUseResidentMarquee())
            {
                CancelResidentMarquee();
                return false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (_hud!.ContainsScreenPoint(Input.mousePosition))
                {
                    return false;
                }

                Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 500f)
                    && _agentRenderer!.TryGetAgent(hit, out _))
                {
                    return false;
                }

                _marqueePending = true;
                _marqueeActive = false;
                _marqueeStart = Input.mousePosition;
                _marqueeCurrent = _marqueeStart;
                return true;
            }

            if (!_marqueePending)
            {
                return false;
            }

            if (Input.GetMouseButton(0))
            {
                _marqueeCurrent = Input.mousePosition;
                if (!_marqueeActive
                    && Vector2.Distance(_marqueeStart, _marqueeCurrent)
                        >= MarqueeThresholdPixels)
                {
                    _marqueeActive = true;
                }

                if (_marqueeActive)
                {
                    _marqueeRenderer!.Show(_marqueeStart, _marqueeCurrent);
                }

                return true;
            }

            if (!Input.GetMouseButtonUp(0))
            {
                return true;
            }

            if (_marqueeActive)
            {
                SelectResidentsInsideMarquee(
                    CreateScreenRect(_marqueeStart, _marqueeCurrent),
                    additive: IsAdditiveResidentSelectionPressed());
            }
            else
            {
                _hud!.SetStatus("Drag LMB to select multiple dwarfs.");
            }

            CancelResidentMarquee();
            return true;
        }

        private bool CanUseResidentMarquee()
        {
            if (_agentRenderer == null
                || _camera == null
                || _buildingPlacementMode.HasValue
                || _excavationMode != DigExcavationDrawingMode.None
                || _caveRoomPreset.HasValue)
            {
                return false;
            }

            return _agentRenderer.SelectedCount == 0
                || IsAdditiveResidentSelectionPressed()
                || _marqueePending;
        }

        private void SelectResidentsInsideMarquee(Rect screenRect, bool additive)
        {
            DigAgentVisual[] residents =
                _agentRenderer!.GetComponentsInChildren<DigAgentVisual>();
            Array.Sort(
                residents,
                (left, right) => string.CompareOrdinal(left.Model.Id, right.Model.Id));
            HashSet<string> alreadySelected = new HashSet<string>(
                _agentRenderer.SelectedAgentIds,
                StringComparer.Ordinal);
            if (!additive)
            {
                _agentRenderer.ClearSelection();
                alreadySelected.Clear();
            }

            for (int index = 0; index < residents.Length; index++)
            {
                DigAgentVisual resident = residents[index];
                if (!resident.Model.IsAlive || alreadySelected.Contains(resident.Model.Id))
                {
                    continue;
                }

                Vector3 projected = _camera!.WorldToScreenPoint(resident.transform.position);
                if (projected.z > 0f
                    && screenRect.Contains(new Vector2(projected.x, projected.y)))
                {
                    _agentRenderer.ToggleSelection(resident);
                    alreadySelected.Add(resident.Model.Id);
                }
            }

            _selectedCell = null;
            _renderer!.Select(null);
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            _tunnelRenderer?.Select(null);
            _hud!.SetAgentSelection(
                _agentRenderer.SelectedModel,
                _agentRenderer.SelectedCount);
            _hud.SetStatus(_agentRenderer.SelectedCount == 0
                ? "No dwarfs were inside the selection rectangle."
                : $"{_agentRenderer.SelectedCount} dwarf(s) selected. " +
                    "LMB on a destination moves the group.");
        }

        private void CancelResidentMarquee()
        {
            _marqueePending = false;
            _marqueeActive = false;
            _marqueeRenderer?.Clear();
        }

        private static Rect CreateScreenRect(Vector2 first, Vector2 second)
        {
            float left = Mathf.Min(first.x, second.x);
            float right = Mathf.Max(first.x, second.x);
            float bottom = Mathf.Min(first.y, second.y);
            float top = Mathf.Max(first.y, second.y);
            return Rect.MinMaxRect(left, bottom, right, top);
        }
    }
}
