using System;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigWorldInteraction : MonoBehaviour
    {
        private const float DoubleClickSeconds = 0.35f;
        private readonly ContextInputRouter _inputRouter = new ContextInputRouter();
        private Camera? _camera;
        private DigCameraController? _cameraController;
        private DigWorldSession? _session;
        private DigWorldRenderer? _renderer;
        private DigAgentRenderer? _agentRenderer;
        private DigJobRenderer? _jobRenderer;
        private DigBuildingRenderer? _buildingRenderer;
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
            DigBuildingRenderer buildingRenderer,
            DigWorldItemRenderer itemRenderer,
            DigBuildingBoxGhostRenderer buildingBoxGhostRenderer,
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
            _buildingRenderer = buildingRenderer;
            _itemRenderer = itemRenderer;
            _buildingBoxGhostRenderer = buildingBoxGhostRenderer;
            _terrainSession = terrainSession;
            _stockpileRenderer = stockpileRenderer;
            _simulation = simulation;
            _hud = hud;
            hud.SetBuildingPlacementControls(this);
            hud.SetExcavationControls(this);
        }

        private void Update()
        {
            if (!IsInitialized())
            {
                return;
            }

            HandleStoragePlacement();
            UpdateBuildingPlacementHover();
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
                        altPressed: IsAltPressed(),
                        isPointerOverBlockingUi: true),
                    BuildState(button),
                    new ContextPointerTarget(ContextWorldTargetKind.None));
                return;
            }

            if (TryClearResidentSelection(right))
            {
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

            if (TryHandleExcavationInput(hit, left, right)
                || TryApplyTunnelMove(hit, left))
            {
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
                if (left && !_buildingPlacementMode.HasValue)
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

            if (_itemRenderer!.TryGetItem(hit, out DigWorldItemVisual item))
            {
                ContextWorldTargetKind kind = item.Model.IsBuildingBox
                    ? ContextWorldTargetKind.BuildingBox
                    : ContextWorldTargetKind.GenericItem;
                ContextPointerTarget target = new ContextPointerTarget(
                    kind,
                    EntityId.Parse(item.Model.StackId),
                    new CellId(item.Model.CellX, item.Model.CellY),
                    reachable: true,
                    supportsAltInteraction: item.Model.IsBuildingBox
                        ? item.Model.AvailableQuantity == 1
                        : item.Model.CanPickup);
                ApplyDecision(_inputRouter.Route(
                    Pointer(button),
                    BuildState(button),
                    target),
                    item: item);
                return;
            }

            if (_buildingRenderer!.TryGetBuilding(hit, out DigBuildingVisual building))
            {
                ContextWorldTargetKind kind = building.Model.IsSelectable
                    ? ContextWorldTargetKind.CompletedBuilding
                    : ContextWorldTargetKind.None;
                ContextPointerTarget target = new ContextPointerTarget(
                    kind,
                    EntityId.Parse(building.Model.Id),
                    new CellId(building.Model.OriginX, building.Model.OriginY));
                ApplyDecision(_inputRouter.Route(
                    Pointer(button),
                    BuildState(button),
                    target),
                    building: building);
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
            string? selectedBuildingId = _buildingRenderer!.SelectedBuildingId;
            EntityId? selectedBuilding = selectedBuildingId == null
                ? null
                : EntityId.Parse(selectedBuildingId);
            bool selectedAlive = _agentRenderer.SelectedModel?.IsAlive ?? true;
            return new ContextInputState(
                selectedResidentId: selectedResident,
                selectedResidentAlive: selectedAlive,
                selectedCompletedBuildingId: selectedBuilding,
                selectedInventoryStackId: _buildingPlacementMode?.SourceStackId,
                selectedInventoryItemIsBuildingBox: _buildingPlacementMode.HasValue,
                buildingPlacementActive: _buildingPlacementMode.HasValue,
                buildingPlacementValid: _buildingPlacementPreview?.IsValid ?? false,
                buildingPlacementReasonCode: _buildingPlacementPreview?.ReasonCode,
                excavationTool: ExcavationToolKind.None);
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
                clickCount,
                altPressed: IsAltPressed());
        }

        private static bool IsAltPressed()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        private bool IsInitialized()
        {
            return _camera != null
                && _cameraController != null
                && _session != null
                && _renderer != null
                && _agentRenderer != null
                && _jobRenderer != null
                && _buildingRenderer != null
                && _itemRenderer != null
                && _buildingBoxGhostRenderer != null
                && _terrainSession != null
                && _stockpileRenderer != null
                && _simulation != null
                && _hud != null;
        }
    }
}
