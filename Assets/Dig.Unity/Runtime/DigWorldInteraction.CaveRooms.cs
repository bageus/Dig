using System;
using System.Collections.Generic;
using System.Linq;
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

            CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
            _excavationMode = DigExcavationDrawingMode.None;
            ResetExcavationStroke();
            _caveRoomPreset = kind;
            _hoveredCaveRoomPlan = null;
            SetTunnelDigInteractionActive(active: true);

            if (!CanUseCavePreset(kind, out string skillDetail))
            {
                _hud!.SetStatus(
                    $"{kind} cave preview: base {preset.BaseWidth}, top {preset.TopWidth}, " +
                    $"depth {preset.Depth}, height {preset.Height}. {skillDetail}");
                return;
            }

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

            if (!CanActivateExcavationDrawing
                || _buildingPlacementMode.HasValue
                || _hud!.ContainsScreenPoint(Input.mousePosition))
            {
                _caveRoomPreviewRenderer.Clear();
                return;
            }

            if (!TryResolveCaveRoomPreview(
                    out CellId entrance,
                    out CaveRoomPlanResult result))
            {
                _caveRoomPreviewRenderer.Clear();
                return;
            }

            CaveRoomPresetKind kind = _caveRoomPreset.Value;
            _hoveredCaveRoomPlan = result;
            _caveRoomPreviewRenderer.Show(
                CaveRoomPresetCatalog.Get(kind),
                entrance,
                result);

            bool skillAllowed = CanUseCavePreset(kind, out string skillDetail);
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            // The room tool owns its LMB click even for an invalid preview. This
            // prevents marquee/movement handlers from consuming the placement click.
            _roomPlacementHandledThisFrame = true;
            if (!skillAllowed)
            {
                _hud.SetStatus(skillDetail);
                return;
            }

            if (!result.Succeeded)
            {
                _hud.SetStatus(result.Detail);
                return;
            }

            ApplyCaveRoomPlan(result.Plan!);
        }

        private bool TryResolveCaveRoomPreview(
            out CellId entrance,
            out CaveRoomPlanResult result)
        {
            entrance = default;
            result = null!;
            CellId? pointerCell = ResolveCaveRoomPointerCell();
            if (!pointerCell.HasValue || !_caveRoomPreset.HasValue)
            {
                return false;
            }

            CaveRoomPresetKind kind = _caveRoomPreset.Value;
            CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
            CaveRoomPlanResult? best = null;
            CellId bestEntrance = default;
            int bestScore = int.MaxValue;
            IReadOnlyList<CellId> candidates = CaveRoomPlacementCandidateResolver.Resolve(
                preset,
                pointerCell.Value);
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                CellId candidate = candidates[candidateIndex];
                CaveRoomPlanResult planned = _session!.PlanCaveRoom(kind, candidate);
                if (planned.Succeeded)
                {
                    entrance = candidate;
                    result = planned;
                    return true;
                }

                int baseInvalid = planned.InvalidCells.Count(value =>
                    value.Reason == CaveRoomPlanFailureReason.BaseTunnelMissing);
                int score = checked(
                    (baseInvalid * 1_000)
                    + (planned.InvalidCells.Count * 10)
                    + candidateIndex);
                if (score < bestScore)
                {
                    best = planned;
                    bestEntrance = candidate;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                return false;
            }

            entrance = bestEntrance;
            result = best;
            return true;
        }

        private CellId? ResolveCaveRoomPointerCell()
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
                RefreshPersistentCaveRoomDesignations();
                _hud.SetStatus(
                    $"{plan.Preset.Kind} cave queued: " +
                    $"{plan.ExcavationTargets.Count} Dig Jobs, " +
                    $"depth {plan.Preset.Depth}.");
                DisableCaveRoomPlanning();
            }
        }

        private void RefreshPersistentCaveRoomDesignations()
        {
            EnsureExcavationCursorRenderer();
            _excavationCursorRenderer!.InvalidateDesignationSynchronization();
            _excavationCursorRenderer.SynchronizeTunnelDesignations(
                _session!.LoadView());
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
