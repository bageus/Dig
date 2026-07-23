using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private CaveRoomPresetKind? _caveRoomPreset;
        private DigCaveRoomPreviewRenderer? _caveRoomPreviewRenderer;
        private DigCaveRoomFloorRenderer? _caveRoomFloorRenderer;
        private CaveRoomPlanResult? _hoveredCaveRoomPlan;
        private bool _roomPlacementHandledThisFrame;
        private long _lastCaveRoomRuntimeTick = -1;
        private readonly CaveRoomSkillAccessPolicy _caveRoomSkillAccess =
            new CaveRoomSkillAccessPolicy();

        internal CaveRoomPresetKind? CaveRoomPreset => _caveRoomPreset;

        internal void SetCaveRoomRenderers(
            DigCaveRoomPreviewRenderer previewRenderer,
            DigCaveRoomFloorRenderer floorRenderer)
        {
            _caveRoomPreviewRenderer = previewRenderer;
            _caveRoomFloorRenderer = floorRenderer;
            _caveRoomFloorRenderer.SetDigInteractionActive(
                UsesTunnelCellInteraction(_excavationMode));
        }

        internal void SetCaveRoomPlanningPreset(CaveRoomPresetKind kind)
        {
            if (!CanActivateExcavationDrawing)
            {
                _hud!.SetStatus("Clear the dwarf selection before placing a cave room.");
                return;
            }

            if (!CanUseCavePreset(kind, out string skillDetail))
            {
                _hud!.SetStatus(skillDetail);
                return;
            }

            CaveRoomPresetCatalog.Get(kind);
            _excavationMode = DigExcavationDrawingMode.None;
            ResetExcavationStroke();
            _caveRoomPreset = kind;
            _hoveredCaveRoomPlan = null;
            SetTunnelDigInteractionActive(active: true);
            CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
            _hud!.SetStatus(
                $"{kind} cave active: base {preset.BaseWidth}, top {preset.TopWidth}, " +
                $"depth {preset.Depth}, height {preset.Height}.");
        }

        private void DisableCaveRoomPlanning()
        {
            _caveRoomPreset = null;
            _hoveredCaveRoomPlan = null;
            _roomPlacementHandledThisFrame = false;
            _caveRoomPreviewRenderer?.Clear();
            SetTunnelDigInteractionActive(
                UsesTunnelCellInteraction(_excavationMode));
        }

        private void UpdateCaveRoomPreview()
        {
            _roomPlacementHandledThisFrame = false;
            RefreshCompletedCaveRooms();
            _hoveredCaveRoomPlan = null;
            if (!_caveRoomPreset.HasValue || _caveRoomPreviewRenderer == null)
            {
                _caveRoomPreviewRenderer?.Clear();
                return;
            }

            if (!CanUseCavePreset(_caveRoomPreset.Value, out _))
            {
                _caveRoomPreviewRenderer.Clear();
                return;
            }

            if (!CanActivateExcavationDrawing
                || _buildingPlacementMode.HasValue
                || _hud!.ContainsScreenPoint(Input.mousePosition))
            {
                _caveRoomPreviewRenderer.Clear();
                return;
            }

            CellId? entrance = ResolveCaveRoomPreviewEntrance();
            if (!entrance.HasValue)
            {
                _caveRoomPreviewRenderer.Clear();
                return;
            }

            CaveRoomPlanResult result = _session!.PlanCaveRoom(
                _caveRoomPreset.Value,
                entrance.Value);
            _hoveredCaveRoomPlan = result;
            _caveRoomPreviewRenderer.Show(
                CaveRoomPresetCatalog.Get(_caveRoomPreset.Value),
                entrance.Value,
                result.Succeeded);

            if (Input.GetMouseButtonDown(0) && result.Succeeded)
            {
                ApplyCaveRoomPlan(result.Plan!);
                _roomPlacementHandledThisFrame = true;
            }
        }

        private CellId? ResolveCaveRoomPreviewEntrance()
        {
            RaycastHit[] hits = GetPointerHits();
            for (int index = 0; index < hits.Length; index++)
            {
                if (_renderer!.TryGetCell(hits[index], out DigCellVisual cell))
                {
                    return new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
                }
            }

            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            float frontDepth = DigTunnelProjection.CellWorldPosition(
                new CellId(0, 0, 0)).z;
            Plane frontLayer = new Plane(Vector3.forward, new Vector3(0f, 0f, frontDepth));
            if (!frontLayer.Raycast(ray, out float distance))
            {
                return null;
            }

            Vector3 point = ray.GetPoint(distance);
            return new CellId(
                Mathf.RoundToInt(point.x),
                Mathf.RoundToInt(-point.y),
                0);
        }

        private void RefreshCompletedCaveRooms(bool force = false)
        {
            if (_simulation == null
                || _session == null
                || _caveRoomFloorRenderer == null)
            {
                return;
            }

            long tick = _simulation.CurrentTick;
            if (!force && _lastCaveRoomRuntimeTick == tick)
            {
                return;
            }

            _lastCaveRoomRuntimeTick = tick;
            _simulation.RefreshCaveRoomRuntime(
                _session.LoadCompletedCaveRoomPlans(),
                _caveRoomFloorRenderer);
        }

        private bool TryHandleCaveRoomPlacement()
        {
            if (_roomPlacementHandledThisFrame)
            {
                return true;
            }

            if (!_caveRoomPreset.HasValue || !Input.GetMouseButtonDown(0))
            {
                return false;
            }

            if (!CanUseCavePreset(_caveRoomPreset.Value, out string skillDetail))
            {
                _hud!.SetStatus(skillDetail);
                DisableCaveRoomPlanning();
                return true;
            }

            if (_hud!.ContainsScreenPoint(Input.mousePosition))
            {
                return false;
            }

            CaveRoomPlanResult? hovered = _hoveredCaveRoomPlan;
            if (hovered == null)
            {
                _hud.SetStatus(
                    "Move the cave outline over an excavated horizontal tunnel cell.");
                return true;
            }

            if (!hovered.Succeeded)
            {
                _hud.SetStatus(hovered.Detail);
                return true;
            }

            ApplyCaveRoomPlan(hovered.Plan!);
            return true;
        }

        private void ApplyCaveRoomPlan(CaveRoomPlan plan)
        {
            Result result = _simulation!.ApplyCaveRoomPlan(
                plan,
                _excavationPriority);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _hud.SetStatus(
                    $"{plan.Preset.Kind} cave queued: " +
                    $"{plan.VolumeCells.Count} Dig Jobs, " +
                    $"depth {plan.Preset.Depth}.");
            }
        }

        private bool CanUseCavePreset(
            CaveRoomPresetKind kind,
            out string detail)
        {
            int maximum = _agentSession!.GetMaximumSkillLevel(
                Dig.Domain.Agents.AgentSkillCatalog.Stonework);
            CaveRoomSkillAccessResult access = _caveRoomSkillAccess.Evaluate(
                kind,
                maximum);
            if (access.Allowed)
            {
                detail = string.Empty;
                return true;
            }

            detail = $"{kind} cave requires Stonework "
                + $"{access.RequiredUnits / Dig.Domain.Agents.AgentSkillCatalog.UnitsPerPoint}; "
                + $"colony maximum is "
                + $"{maximum / Dig.Domain.Agents.AgentSkillCatalog.UnitsPerPoint}.";
            return false;
        }
    }
}
