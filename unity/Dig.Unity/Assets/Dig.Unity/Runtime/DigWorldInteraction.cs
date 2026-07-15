using System;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldInteraction : MonoBehaviour
    {
        private const float DoubleClickSeconds = 0.35f;
        private readonly ContextInputRouter _inputRouter = new ContextInputRouter();
        private Camera? _camera;
        private DigCameraController? _cameraController;
        private DigWorldSession? _session;
        private DigWorldRenderer? _renderer;
        private DigAgentRenderer? _agentRenderer;
        private DigJobRenderer? _jobRenderer;
        private DigTerrainWorkSession? _terrainSession;
        private DigStockpileRenderer? _stockpileRenderer;
        private DigAgentSimulationDriver? _simulation;
        private DigHudOverlay? _hud;
        private DigCellVisual? _selectedCell;
        private string? _lastResidentClickId;
        private float _lastResidentClickTime = float.NegativeInfinity;

        internal void Initialize(
            Camera targetCamera,
            DigCameraController cameraController,
            DigWorldSession session,
            DigWorldRenderer renderer,
            DigAgentRenderer agentRenderer,
            DigJobRenderer jobRenderer,
            DigTerrainWorkSession terrainSession,
            DigStockpileRenderer stockpileRenderer,
            DigAgentSimulationDriver simulation,
            DigHudOverlay hud)
        {
            _camera = targetCamera;
            _cameraController = cameraController;
            _session = session;
            _renderer = renderer;
            _agentRenderer = agentRenderer;
            _jobRenderer = jobRenderer;
            _terrainSession = terrainSession;
            _stockpileRenderer = stockpileRenderer;
            _simulation = simulation;
            _hud = hud;
        }

        private void Update()
        {
            if (!IsInitialized())
            {
                return;
            }

            HandleStoragePlacement();
            bool left = Input.GetMouseButtonDown(0);
            bool right = Input.GetMouseButtonDown(1);
            if (!left && !right)
            {
                return;
            }

            PointerButtonKind button = left
                ? PointerButtonKind.Left
                : PointerButtonKind.Right;
            if (_hud!.ContainsScreenPoint(Input.mousePosition))
            {
                _inputRouter.Route(
                    new ContextPointerEvent(
                        PointerInputSurface.World,
                        button,
                        isPointerOverBlockingUi: true),
                    BuildState(button),
                    new ContextPointerTarget(ContextWorldTargetKind.None));
                return;
            }

            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                ApplyDecision(_inputRouter.Route(
                    Pointer(button),
                    BuildState(button),
                    new ContextPointerTarget(ContextWorldTargetKind.None)));
                return;
            }

            if (_agentRenderer!.TryGetAgent(hit, out DigAgentVisual agent))
            {
                int clickCount = left ? RegisterResidentClick(agent.Model.Id) : 1;
                ContextPointerTarget target = new ContextPointerTarget(
                    ContextWorldTargetKind.Resident,
                    EntityId.Parse(agent.Model.Id),
                    new CellId(agent.Model.CellX, agent.Model.CellY),
                    isAlive: agent.Model.IsAlive);
                ApplyDecision(_inputRouter.Route(
                    Pointer(button, clickCount),
                    BuildState(button),
                    target),
                    agent: agent);
                return;
            }

            if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job))
            {
                if (left)
                {
                    SelectJob(job);
                    return;
                }

                ApplyDecision(_inputRouter.Route(
                    Pointer(button),
                    BuildState(button),
                    new ContextPointerTarget(ContextWorldTargetKind.None)));
                return;
            }

            if (!_renderer!.TryGetCell(hit, out DigCellVisual cell))
            {
                ApplyDecision(_inputRouter.Route(
                    Pointer(button),
                    BuildState(button),
                    new ContextPointerTarget(ContextWorldTargetKind.None)));
                return;
            }

            ContextPointerTarget ground = new ContextPointerTarget(
                ContextWorldTargetKind.Ground,
                cell: new CellId(cell.Model.X, cell.Model.Y),
                reachable: !cell.Model.IsSolid);
            ApplyDecision(_inputRouter.Route(
                Pointer(button),
                BuildState(button),
                ground),
                cell: cell);
        }

        private ContextInputState BuildState(PointerButtonKind button)
        {
            string? selectedId = _agentRenderer!.SelectedAgentId;
            EntityId? selectedResident = selectedId == null
                ? null
                : EntityId.Parse(selectedId);
            bool selectedAlive = _agentRenderer.SelectedModel?.IsAlive ?? true;
            ExcavationToolKind tool = button == PointerButtonKind.Right
                ? ExcavationToolKind.Tunnel
                : ExcavationToolKind.None;
            return new ContextInputState(
                selectedResidentId: selectedResident,
                selectedResidentAlive: selectedAlive,
                excavationTool: tool);
        }

        private void ApplyDecision(
            ContextInputDecision decision,
            DigAgentVisual? agent = null,
            DigCellVisual? cell = null)
        {
            ApplyEffects(decision, agent, cell);
            if (!decision.HasApplicationCommand)
            {
                return;
            }

            switch (decision.CommandKind)
            {
                case ApplicationInputCommandKind.MoveResident:
                    ApplyMove(decision);
                    break;
                case ApplicationInputCommandKind.ApplyExcavation:
                    ApplyExcavation(decision, cell);
                    break;
                default:
                    _hud!.SetStatus(
                        $"Input command '{decision.CommandKind}' is not wired in this demo slice.");
                    break;
            }
        }

        private void ApplyEffects(
            ContextInputDecision decision,
            DigAgentVisual? agent,
            DigCellVisual? cell)
        {
            if (decision.Effects.HasFlag(PresentationInputEffect.DeselectResident))
            {
                _agentRenderer!.Select(null);
                _hud!.SetAgentSelection(null);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.SelectResident))
            {
                DigAgentVisual? selected = agent;
                if (selected == null && decision.TargetEntityId.HasValue)
                {
                    selected = _agentRenderer!.SelectById(
                        decision.TargetEntityId.Value.ToString());
                }
                else
                {
                    _agentRenderer!.Select(selected);
                }

                _selectedCell = null;
                _renderer!.Select(null);
                _jobRenderer!.Select(null);
                _hud!.SetAgentSelection(selected?.Model);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.SelectGround)
                && cell != null)
            {
                SelectCell(cell);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.FocusResident)
                && decision.TargetEntityId.HasValue
                && _agentRenderer!.TryGetWorldPosition(
                    decision.TargetEntityId.Value.ToString(),
                    out Vector3 position))
            {
                _cameraController!.Focus(position);
            }

            if (decision.Effects.HasFlag(PresentationInputEffect.ShowReason)
                && decision.ReasonCode != null)
            {
                _hud!.SetStatus(decision.ReasonCode);
            }
        }

        private void ApplyMove(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue || !decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.move.missing_target");
                return;
            }

            Result result = _simulation!.MoveResident(
                decision.ActorId.Value.ToString(),
                decision.TargetCell.Value);
            _hud!.SetCommandResult(result);
        }

        private void ApplyExcavation(
            ContextInputDecision decision,
            DigCellVisual? cell)
        {
            if (!decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.excavation.missing_target");
                return;
            }

            DigCellVisual? target = cell ?? _renderer!.SelectAt(
                decision.TargetCell.Value.X,
                decision.TargetCell.Value.Y);
            if (target == null)
            {
                _hud!.SetStatus("input.excavation.stale_target");
                return;
            }

            SelectCell(target);
            ToggleDesignation(target.Model);
        }

        private void SelectCell(DigCellVisual cell)
        {
            _selectedCell = cell;
            _agentRenderer!.Select(null);
            _jobRenderer!.Select(null);
            _renderer!.Select(cell);
            _hud!.SetSelection(cell.Model);
        }

        private void SelectJob(DigJobVisual job)
        {
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.Select(null);
            _jobRenderer!.Select(job);
            _hud!.SetJobSelection(job.Model);
        }

        private int RegisterResidentClick(string residentId)
        {
            float now = Time.unscaledTime;
            bool doubleClick = string.Equals(
                    _lastResidentClickId,
                    residentId,
                    StringComparison.Ordinal)
                && now - _lastResidentClickTime <= DoubleClickSeconds;
            _lastResidentClickId = residentId;
            _lastResidentClickTime = now;
            return doubleClick ? 2 : 1;
        }

        private static ContextPointerEvent Pointer(
            PointerButtonKind button,
            int clickCount = 1)
        {
            return new ContextPointerEvent(
                PointerInputSurface.World,
                button,
                clickCount);
        }

        private bool IsInitialized()
        {
            return _camera != null
                && _cameraController != null
                && _session != null
                && _renderer != null
                && _agentRenderer != null
                && _jobRenderer != null
                && _terrainSession != null
                && _stockpileRenderer != null
                && _simulation != null
                && _hud != null;
        }

        private void HandleStoragePlacement()
        {
            bool requested = Input.GetKeyDown(KeyCode.Alpha5)
                || Input.GetKeyDown(KeyCode.Keypad5);
            if (!requested)
            {
                return;
            }

            if (_selectedCell == null)
            {
                _hud!.SetStatus("Select an open cell before placing the stockpile.");
                return;
            }

            WorldCellViewModel selected = _selectedCell.Model;
            Result result = _terrainSession!.MoveStorageZone(
                new CellId(selected.X, selected.Y),
                _simulation!.CurrentTick);
            if (result.IsFailure)
            {
                _hud!.SetCommandResult(result);
                return;
            }

            DigStorageStatus storage = _terrainSession.GetStorageStatus();
            _stockpileRenderer!.Render(storage);
            _hud!.SetStorageStatus(storage);
            _hud.SetStatus($"Stockpile moved to {storage.Cell.X},{storage.Cell.Y}.");
        }

        private void ToggleDesignation(WorldCellViewModel selected)
        {
            Result result = _session!.ToggleDesignation(selected);
            _hud!.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            WorldViewModel world = _session.LoadView();
            _renderer!.Render(world);
            DigCellVisual? refreshed = _renderer.SelectAt(selected.X, selected.Y);
            _selectedCell = refreshed;
            _hud.SetWorld(world);
            _hud.SetSelection(refreshed == null ? null : refreshed.Model);
        }
    }
}