using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using Dig.Presentation.Navigation;
using Dig.Presentation.Overlays;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigWorldOverlayRenderer
{
    internal void RenderWorld(
        WorldViewModel world,
        TerrainDepositVolumeViewModel deposits)
    {
        int designationCount = 0;
        int fogCount = 0;
        int dirtyCount = 0;
        for (int chunkIndex = 0; chunkIndex < world.Chunks.Count; chunkIndex++)
        {
            WorldChunkViewModel chunk = world.Chunks[chunkIndex];
            Vector2Int chunkKey = new Vector2Int(chunk.X, chunk.Y);
            bool changed = _chunkVersions.TryGetValue(chunkKey, out long previous)
                && previous != chunk.Version;
            _chunkVersions[chunkKey] = chunk.Version;
            if (changed && dirtyCount < MaximumDiagnosticMarkers)
            {
                WorldCellViewModel center = chunk.Cells[chunk.Cells.Count / 2];
                GameObject marker = Acquire(
                    _dirtyChunks,
                    dirtyCount++,
                    _diagnosticRoot!,
                    "Dirty Chunk",
                    OverlayLayerKind.Diagnostics,
                    OverlaySemanticKind.DirtyChunk);
                PlaceCell(marker, center.X, center.Y, 1.18f, 1.25f);
            }

            for (int cellIndex = 0; cellIndex < chunk.Cells.Count; cellIndex++)
            {
                WorldCellViewModel cell = chunk.Cells[cellIndex];
                if (cell.IsDesignated)
                {
                    GameObject marker = Acquire(
                        _designations,
                        designationCount++,
                        _designationRoot!,
                        "Designation",
                        OverlayLayerKind.Designation,
                        OverlaySemanticKind.Designation);
                    PlaceCellAtDepth(marker, cell.X, cell.Y, cell.Z, 0.08f);
                }

                if (!cell.IsExplored && fogCount < MaximumDiagnosticMarkers)
                {
                    GameObject marker = Acquire(
                        _fog,
                        fogCount++,
                        _diagnosticRoot!,
                        "Fog",
                        OverlayLayerKind.Diagnostics,
                        OverlaySemanticKind.Fog);
                    PlaceCell(marker, cell.X, cell.Y, 1.02f, 0.88f);
                }
            }
        }

        int depositCount = 0;
        for (int index = 0; index < deposits.Cells.Count
            && depositCount < MaximumDiagnosticMarkers; index++)
        {
            TerrainDepositCellViewModel deposit = deposits.Cells[index];
            if (!deposit.IsVisible)
            {
                continue;
            }

            GameObject marker = Acquire(
                _deposits,
                depositCount++,
                _diagnosticRoot!,
                "Deposit",
                OverlayLayerKind.Diagnostics,
                OverlaySemanticKind.Deposit);
            PlaceCell(marker, deposit.Cell.X, deposit.Cell.Y, 1.08f, 0.42f);
        }

        HideRemainder(_designations, designationCount);
        HideRemainder(_fog, fogCount);
        HideRemainder(_dirtyChunks, dirtyCount);
        HideRemainder(_deposits, depositCount);
    }

    internal void RenderDynamic(
        IReadOnlyList<BuildingWorldViewModel> buildings,
        DigStorageStatus storage,
        IReadOnlyList<RouteViewModel> routes)
    {
        int footprintCount = 0;
        for (int index = 0; index < buildings.Count; index++)
        {
            BuildingWorldViewModel building = buildings[index];
            if (building.IsSelectable)
            {
                continue;
            }

            for (int cellIndex = 0; cellIndex < building.Footprint.Count; cellIndex++)
            {
                BuildingFootprintCellViewModel cell = building.Footprint[cellIndex];
                GameObject marker = Acquire(
                    _buildingFootprints,
                    footprintCount++,
                    _previewRoot!,
                    "Building Footprint",
                    OverlayLayerKind.Preview,
                    OverlaySemanticKind.BuildingFootprint);
                PlaceCell(marker, cell.X, cell.Y, 0.96f, 0.82f);
            }
        }

        int storageCount = 0;
        if (storage.ReservedIncomingQuantity > 0)
        {
            GameObject marker = Acquire(
                _storageDemand,
                storageCount++,
                _reservationRoot!,
                "Storage Demand",
                OverlayLayerKind.Reservations,
                OverlaySemanticKind.StorageDemand);
            PlaceCell(marker, storage.Cell.X, storage.Cell.Y, 1.12f, 0.94f);
        }

        int navigationCount = 0;
        for (int index = 0; index < routes.Count
            && navigationCount < MaximumDiagnosticMarkers; index++)
        {
            RouteViewModel route = routes[index];
            if (route.Succeeded)
            {
                continue;
            }

            GameObject marker = Acquire(
                _navigation,
                navigationCount++,
                _diagnosticRoot!,
                "Navigation Failure",
                OverlayLayerKind.Diagnostics,
                OverlaySemanticKind.NavigationDiagnostic);
            PlaceCell(marker, route.WorkX, route.WorkY, 1.16f, 0.62f);
        }

        HideRemainder(_buildingFootprints, footprintCount);
        HideRemainder(_storageDemand, storageCount);
        HideRemainder(_navigation, navigationCount);
    }

    private void LateUpdate()
    {
        if (_agents == null || _buildings == null || _world == null)
        {
            return;
        }

        int count = 0;
        IReadOnlyList<string> selectedResidents = _agents.SelectedAgentIds;
        for (int index = 0; index < selectedResidents.Count; index++)
        {
            if (!_agents.TryGetWorldPosition(selectedResidents[index], out Vector3 position))
            {
                continue;
            }

            GameObject marker = Acquire(
                _selection,
                count++,
                _selectionRoot!,
                "Resident Selection",
                OverlayLayerKind.Selection,
                OverlaySemanticKind.Selection);
            marker.transform.position = position + (Vector3.up * 0.05f);
            marker.transform.localScale = new Vector3(0.78f, 0.035f, 0.78f);
        }

        BuildingWorldViewModel? building = _buildings.SelectedModel;
        if (building != null)
        {
            for (int index = 0; index < building.Footprint.Count; index++)
            {
                BuildingFootprintCellViewModel cell = building.Footprint[index];
                GameObject marker = Acquire(
                    _selection,
                    count++,
                    _selectionRoot!,
                    "Building Selection",
                    OverlayLayerKind.Selection,
                    OverlaySemanticKind.Selection);
                PlaceCell(marker, cell.X, cell.Y, 1.04f, 0.86f);
            }
        }

        WorldCellViewModel? cellSelection = _world.SelectedModel;
        if (cellSelection is WorldCellViewModel selectedCell)
        {
            GameObject marker = Acquire(
                _selection,
                count++,
                _selectionRoot!,
                "Cell Selection",
                OverlayLayerKind.Selection,
                OverlaySemanticKind.Selection);
            PlaceCell(marker, selectedCell.X, selectedCell.Y, 1.10f, 0.86f);
        }

        HideRemainder(_selection, count);
    }
}
}
