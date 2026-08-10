using System;
using System.Linq;
using Dig.Domain.World;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private WorldItemViewModel? _selectedBuildingBox;
        private DigWorldItemVisual? _selectedBuildingBoxVisual;

        internal WorldItemViewModel? SelectedBuildingBox => _selectedBuildingBox;

        internal void SelectBuildingBoxFromHud(string stackId)
        {
            if (string.IsNullOrWhiteSpace(stackId))
            {
                throw new ArgumentException(
                    "BuildingBox stack id is required.",
                    nameof(stackId));
            }

            WorldItemViewModel? item = _terrainSession!.LoadAllWorldItems()
                .FirstOrDefault(value => value.IsBuildingBox
                    && string.Equals(value.StackId, stackId, StringComparison.Ordinal));
            if (item == null)
            {
                ClearBuildingBoxSelection();
                _hud?.SetStatus("building_box.selection.stale");
                return;
            }

            SelectBuildingBox(item);
        }

        internal void ActivateSelectedBuildingBoxFromHud()
        {
            if (_selectedBuildingBox == null)
            {
                _hud?.SetStatus("building_box.action.no_selection");
                return;
            }

            string stackId = _selectedBuildingBox.StackId;
            if (string.Equals(
                ActiveBuildingPlacementStackId,
                stackId,
                StringComparison.Ordinal))
            {
                CancelBuildingPlacement();
                return;
            }

            BeginBuildingPlacement(
                stackId,
                new CellId(
                    _selectedBuildingBox.CellX,
                    _selectedBuildingBox.CellY,
                    _selectedBuildingBox.CellZ));
        }

        private void SelectBuildingBox(WorldItemViewModel item)
        {
            SelectBuildingBox(item, ResolveWorldItemVisual(item.StackId));
        }

        private void SelectBuildingBox(
            WorldItemViewModel item,
            DigWorldItemVisual? visual)
        {
            _selectedBuildingBox = item
                ?? throw new ArgumentNullException(nameof(item));
            ClearRoomInfrastructureSelection();
            SetBuildingBoxVisualSelection(visual);
            _selectedCell = null;
            _renderer!.Select(null);
            _agentRenderer!.ClearSelection();
            _jobRenderer!.Select(null);
            _buildingRenderer!.Select(null);
            ClearSelectedInventoryStack();
            _hud!.SetBuildingSelection(null);
            _hud.ActivateBuildingRosterForSelection();
            _hud.SetStatus("BuildingBox selected.");
        }

        private DigWorldItemVisual? ResolveWorldItemVisual(string stackId)
        {
            if (_itemRenderer == null)
            {
                return null;
            }

            return _itemRenderer
                .GetComponentsInChildren<DigWorldItemVisual>(includeInactive: true)
                .FirstOrDefault(value => value.Model != null
                    && string.Equals(value.Model.StackId, stackId, StringComparison.Ordinal));
        }

        private void SetBuildingBoxVisualSelection(DigWorldItemVisual? visual)
        {
            if (_selectedBuildingBoxVisual != null)
            {
                _selectedBuildingBoxVisual.SetSelectionHighlighted(false);
            }

            _selectedBuildingBoxVisual = visual;
            _selectedBuildingBoxVisual?.SetSelectionHighlighted(true);
        }

        private void ClearBuildingBoxSelection()
        {
            _selectedBuildingBox = null;
            SetBuildingBoxVisualSelection(null);
        }
    }
}
