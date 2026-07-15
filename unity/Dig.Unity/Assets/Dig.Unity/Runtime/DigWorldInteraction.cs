using Dig.Domain.Core;
using Dig.Domain.World;
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
        private DigJobRenderer? _jobRenderer;
        private DigTerrainWorkSession? _terrainSession;
        private DigStockpileRenderer? _stockpileRenderer;
        private DigAgentSimulationDriver? _simulation;
        private DigHudOverlay? _hud;
        private DigCellVisual? _selectedCell;

        internal void Initialize(
            Camera targetCamera,
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
            if (_camera == null
                || _session == null
                || _renderer == null
                || _agentRenderer == null
                || _jobRenderer == null
                || _terrainSession == null
                || _stockpileRenderer == null
                || _simulation == null
                || _hud == null)
            {
                return;
            }

            HandleStoragePlacement();
            bool select = Input.GetMouseButtonDown(0);
            bool updateCell = Input.GetMouseButtonDown(1);
            if ((!select && !updateCell) || _hud.ContainsScreenPoint(Input.mousePosition))
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
                    _selectedCell = null;
                    _renderer.Select(null);
                    _jobRenderer.Select(null);
                    _agentRenderer.Select(agent);
                    _hud.SetAgentSelection(agent.Model);
                }

                return;
            }

            if (_jobRenderer.TryGetJob(hit, out DigJobVisual job))
            {
                if (select)
                {
                    _selectedCell = null;
                    _renderer.Select(null);
                    _agentRenderer.Select(null);
                    _jobRenderer.Select(job);
                    _hud.SetJobSelection(job.Model);
                }

                return;
            }

            if (!_renderer.TryGetCell(hit, out DigCellVisual cell))
            {
                return;
            }

            _selectedCell = cell;
            _agentRenderer.Select(null);
            _jobRenderer.Select(null);
            _renderer.Select(cell);
            _hud.SetSelection(cell.Model);
            if (updateCell)
            {
                ToggleDesignation(cell.Model);
            }
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
