using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private BuildingBoxPlacementModeState? _buildingPlacementMode;
        private BuildingBoxGhostViewModel? _buildingPlacementPreview;
        private DigWorldInteraction? _buildingPlacementInteraction;

        private bool HasBuildingPlacement => _buildingPlacementMode.HasValue;

        internal void SetBuildingPlacementControls(DigWorldInteraction interaction)
        {
            _buildingPlacementInteraction = interaction;
        }

        internal void SetBuildingPlacement(
            BuildingBoxPlacementModeState mode,
            BuildingBoxGhostViewModel preview)
        {
            _buildingPlacementMode = mode;
            _buildingPlacementPreview = preview;
            _selectedCell = null;
            _selectedAgent = null;
            ClearJobSelection();
            ClearBuildingSelection();
        }

        internal void UpdateBuildingPlacement(
            BuildingBoxPlacementModeState mode,
            BuildingBoxGhostViewModel preview)
        {
            _buildingPlacementMode = mode;
            _buildingPlacementPreview = preview;
        }

        internal void ClearBuildingPlacement()
        {
            _buildingPlacementMode = null;
            _buildingPlacementPreview = null;
        }

        private void DrawBuildingPlacement()
        {
            if (!_buildingPlacementMode.HasValue || _buildingPlacementPreview == null)
            {
                return;
            }

            BuildingBoxPlacementModeState mode = _buildingPlacementMode.Value;
            BuildingBoxGhostViewModel preview = _buildingPlacementPreview;
            GUILayout.Label("BUILDING PLACEMENT");
            GUILayout.Label($"Definition: {mode.DefinitionId}");
            GUILayout.Label($"Source box: {mode.SourceStackId}");
            GUILayout.Label($"Orientation: {mode.Orientation}");
            GUILayout.Label($"Origin: {preview.Origin.X},{preview.Origin.Y}");
            GUILayout.Label(preview.IsValid
                ? "Preview: valid"
                : $"Preview: blocked — {preview.ReasonCode}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate left", GUILayout.Width(100f)))
            {
                _buildingPlacementInteraction?.RotateBuildingPlacement(clockwise: false);
            }

            if (GUILayout.Button("Rotate right", GUILayout.Width(100f)))
            {
                _buildingPlacementInteraction?.RotateBuildingPlacement(clockwise: true);
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
            {
                _buildingPlacementInteraction?.CancelBuildingPlacement();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label("LMB confirm | RMB cancel");
        }
    }
}
