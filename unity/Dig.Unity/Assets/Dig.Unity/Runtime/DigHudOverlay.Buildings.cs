using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private BuildingWorldViewModel? _selectedBuilding;
        private DigTerrainWorkSession? _buildingSession;
        private DigBuildingRenderer? _buildingRenderer;
        private DigJobRenderer? _buildingJobRenderer;

        private bool HasBuildingSelection => _selectedBuilding != null;

        internal void SetBuildingControls(
            DigTerrainWorkSession session,
            DigBuildingRenderer renderer,
            DigJobRenderer jobRenderer)
        {
            _buildingSession = session;
            _buildingRenderer = renderer;
            _buildingJobRenderer = jobRenderer;
        }

        public void SetBuildingSelection(BuildingWorldViewModel? selected)
        {
            _selectedBuilding = selected;
            _selectedCell = null;
            _selectedAgent = null;
            ClearJobSelection();
        }

        private void ClearBuildingSelection()
        {
            _selectedBuilding = null;
        }

        private void DrawBuildingSelection()
        {
            if (_selectedBuilding == null)
            {
                return;
            }

            BuildingWorldViewModel building = _selectedBuilding;
            BuildingFunctionsViewModel functions = building.Functions;
            GUILayout.Label("SELECTED BUILDING");
            GUILayout.Label($"{building.Name} | {building.Status} | v{building.Version}");
            GUILayout.Label($"Cell: {building.OriginX},{building.OriginY}");
            GUILayout.Label(
                $"Durability: {functions.Durability}/{functions.MaximumDurability}");
            if (functions.IsPacking)
            {
                GUILayout.Label(
                    $"Packing: {functions.PackingCompletedWork}/" +
                    $"{functions.PackingRequiredWork} " +
                    $"({functions.PackingProgress:P0})");
            }

            BuildingFunctionActionViewModel action = functions.Actions[0];
            bool previousEnabled = GUI.enabled;
            GUI.enabled = action.IsEnabled;
            bool requested = GUILayout.Button("Pack into BuildingBox", GUILayout.Width(210f));
            GUI.enabled = previousEnabled;
            if (!action.IsEnabled && action.DisabledReasonCode != null)
            {
                GUILayout.Label($"Unavailable: {action.DisabledReasonCode}");
            }

            if (requested)
            {
                ExecutePacking(building.Id);
            }
        }

        private void ExecutePacking(string buildingId)
        {
            if (_buildingSession == null
                || _buildingRenderer == null
                || _buildingJobRenderer == null)
            {
                SetStatus("unity.buildings.not_initialized");
                return;
            }

            long tick = _simulation?.CurrentTick ?? _tick;
            Result result = _buildingSession.StartBuildingPacking(buildingId, tick);
            SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            _buildingRenderer.Render(_buildingSession.LoadBuildings());
            DigBuildingVisual? selected = _buildingRenderer.SelectById(buildingId);
            _selectedBuilding = selected?.Model;
            var jobs = _buildingSession.LoadJobs();
            _buildingJobRenderer.Render(jobs);
            SetJobs(jobs);
        }
    }
}
