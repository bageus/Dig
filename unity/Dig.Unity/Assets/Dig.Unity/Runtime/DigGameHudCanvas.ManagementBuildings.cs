using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void RefreshBuildingManagement()
    {
        IReadOnlyList<BuildingWorldViewModel> allBuildings =
            _terrainSession!.LoadBuildings();
        IReadOnlyDictionary<string, BuildingWorldViewModel> boxTransformations =
            IndexPendingBuildingBoxTransformations(allBuildings);
        IReadOnlyList<BuildingWorldViewModel> buildings = allBuildings
            .Where(value => !value.IsPendingBuildingBoxLifecycle)
            .ToArray();
        IReadOnlyList<WorldItemViewModel> buildingBoxes = _terrainSession
            .LoadAllWorldItems()
            .Where(item => item.IsBuildingBox)
            .ToArray();
        ResidentRosterViewModel residentRoster = _simulation!.LoadResidentRoster(null);
        IReadOnlyList<HeldBuildingBoxRosterEntry> heldBuildingBoxes =
            LoadHeldBuildingBoxes(residentRoster.Rows);
        string signature = "buildings:"
            + string.Join("|", allBuildings.Select(value =>
                value.Id + ":" + value.Version + ":" + value.Status + ":"
                + value.BuildingBoxCommitState))
            + ":boxes:"
            + string.Join("|", buildingBoxes.Select(value =>
                value.StackId + ":" + value.Quantity + ":" + value.ReservedQuantity
                + ":" + value.CellX + ":" + value.CellY + ":" + value.CellZ))
            + ":held:"
            + string.Join("|", heldBuildingBoxes.Select(value =>
                value.StackId + ":" + value.ResidentId + ":" + value.ReservedQuantity));
        if (string.Equals(signature, _managementSignature, StringComparison.Ordinal))
        {
            return;
        }

        _managementSignature = signature;
        BeginManagementOverlay(
            "Buildings",
            new[] { "All buildings" },
            activeTab: 0,
            selectTab: _ => { });
        BuildBuildingManagementTable(
            buildings,
            buildingBoxes,
            heldBuildingBoxes,
            boxTransformations);
    }

    private void BuildBuildingManagementTable(
        IReadOnlyList<BuildingWorldViewModel> buildings,
        IReadOnlyList<WorldItemViewModel> buildingBoxes,
        IReadOnlyList<HeldBuildingBoxRosterEntry> heldBuildingBoxes,
        IReadOnlyDictionary<string, BuildingWorldViewModel> boxTransformations)
    {
        ManagementColumn[] columns =
        {
            Column("management.name", 210f),
            Column("management.type", 230f),
            Column("management.status", 150f),
            Column("management.position", 120f),
            Column("management.condition", 150f),
            Column("management.progress", 150f),
        };
        BuildManagementHeader(columns);
        if (buildings.Count == 0
            && buildingBoxes.Count == 0
            && heldBuildingBoxes.Count == 0
            && boxTransformations.Count == 0)
        {
            BuildManagementEmptyState(
                DigManagementLocalization.Resolve("management.buildings.empty"));
            return;
        }

        foreach (BuildingWorldViewModel building in buildings)
        {
            string buildingId = building.Id;
            RectTransform row = CreateManagementRow(buildingId, 38f);
            ConfigureManagementSelection(
                row,
                () => SelectBuildingFromManagement(buildingId));
            CreateManagementTextCell(row, building.Name, columns[0].Width);
            CreateManagementTextCell(row, building.DefinitionId, columns[1].Width);
            CreateManagementTextCell(
                row,
                BuildingStatusLabel(building.Status),
                columns[2].Width,
                TextAnchor.MiddleCenter);
            CreateManagementTextCell(
                row,
                building.OriginX + ", " + building.OriginY,
                columns[3].Width,
                TextAnchor.MiddleCenter);
            CreateManagementBarCell(
                row,
                building.Functions.Durability,
                building.Functions.MaximumDurability,
                columns[4].Width,
                new Color(0.22f, 0.68f, 0.30f, 1f));
            CreateManagementBarCell(
                row,
                building.CompletedWork,
                building.RequiredWork,
                columns[5].Width,
                new Color(0.20f, 0.52f, 0.84f, 1f));
        }

        foreach (WorldItemViewModel box in buildingBoxes)
        {
            string stackId = box.StackId;
            RectTransform row = CreateManagementRow("BuildingBox " + stackId, 38f);
            ConfigureManagementSelection(
                row,
                () => SelectBuildingBoxFromManagement(stackId));
            boxTransformations.TryGetValue(
                stackId,
                out BuildingWorldViewModel? transformation);
            CreateManagementTextCell(
                row,
                transformation?.Name ?? "BuildingBox",
                columns[0].Width);
            CreateManagementTextCell(row, box.ItemId, columns[1].Width);
            CreateManagementTextCell(
                row,
                transformation == null
                    ? (box.ReservedQuantity == 0 ? "Packed" : "Reserved")
                    : FormatBuildingBoxTransformationStatus(transformation),
                columns[2].Width,
                TextAnchor.MiddleCenter);
            CreateManagementTextCell(
                row,
                box.CellX + ", " + box.CellY + ", Z" + box.CellZ,
                columns[3].Width,
                TextAnchor.MiddleCenter);
            CreateManagementBarCell(
                row,
                box.AvailableQuantity,
                box.Quantity,
                columns[4].Width,
                new Color(0.64f, 0.42f, 0.20f, 1f));
            CreateManagementBarCell(
                row,
                box.ReservedQuantity == 0 ? 1 : 0,
                1,
                columns[5].Width,
                new Color(0.20f, 0.52f, 0.84f, 1f));
        }
        foreach (HeldBuildingBoxRosterEntry box in heldBuildingBoxes)
        {
            string residentId = box.ResidentId;
            RectTransform row = CreateManagementRow(
                "Held BuildingBox " + box.StackId, 38f);
            ConfigureManagementSelection(
                row,
                () => SelectResidentFromBuildingManagement(residentId));
            boxTransformations.TryGetValue(
                box.StackId,
                out BuildingWorldViewModel? transformation);
            CreateManagementTextCell(
                row,
                transformation?.Name ?? box.DisplayName,
                columns[0].Width);
            CreateManagementTextCell(row, box.ItemId, columns[1].Width);
            CreateManagementTextCell(
                row,
                transformation == null
                    ? "Held by " + box.ResidentName
                    : FormatBuildingBoxTransformationStatus(transformation),
                columns[2].Width,
                TextAnchor.MiddleCenter);
            CreateManagementTextCell(
                row,
                "Inventory",
                columns[3].Width,
                TextAnchor.MiddleCenter);
            CreateManagementBarCell(
                row,
                box.Quantity - box.ReservedQuantity,
                box.Quantity,
                columns[4].Width,
                new Color(0.64f, 0.42f, 0.20f, 1f));
            CreateManagementBarCell(
                row,
                box.ReservedQuantity == 0 ? 1 : 0,
                1,
                columns[5].Width,
                new Color(0.20f, 0.52f, 0.84f, 1f));
        }

        HashSet<string> physicalStackIds = new HashSet<string>(
            buildingBoxes.Select(value => value.StackId)
                .Concat(heldBuildingBoxes.Select(value => value.StackId)),
            StringComparer.Ordinal);
        foreach (BuildingWorldViewModel transformation in boxTransformations.Values
            .Where(value => !physicalStackIds.Contains(value.SourceBuildingBoxStackId!))
            .OrderBy(value => value.Name, StringComparer.Ordinal))
        {
            string jobId = transformation.BuildingBoxJobId!;
            RectTransform row = CreateManagementRow(
                "BuildingBox transformation " + transformation.SourceBuildingBoxStackId,
                38f);
            ConfigureManagementSelection(
                row,
                () => SelectBuildingBoxTransformationFromManagement(jobId));
            CreateManagementTextCell(row, transformation.Name, columns[0].Width);
            CreateManagementTextCell(
                row,
                transformation.DefinitionId,
                columns[1].Width);
            CreateManagementTextCell(
                row,
                FormatBuildingBoxTransformationStatus(transformation),
                columns[2].Width,
                TextAnchor.MiddleCenter);
            CreateManagementTextCell(
                row,
                transformation.OriginX + ", " + transformation.OriginY,
                columns[3].Width,
                TextAnchor.MiddleCenter);
            CreateManagementBarCell(row, 1, 1, columns[4].Width,
                new Color(0.64f, 0.42f, 0.20f, 1f));
            CreateManagementBarCell(
                row,
                transformation.CompletedWork,
                transformation.RequiredWork,
                columns[5].Width,
                new Color(0.20f, 0.52f, 0.84f, 1f));
        }
    }

    private static void ConfigureManagementSelection(
        RectTransform row,
        Action select)
    {
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row.GetComponent<Image>();
        button.onClick.AddListener(() => select());
    }

    private void SelectBuildingFromManagement(string buildingId)
    {
        CloseManagementOverlay();
        _interaction!.SelectBuildingFromHud(buildingId);
        InvalidateAll();
    }

    private void SelectBuildingBoxFromManagement(string stackId)
    {
        CloseManagementOverlay();
        _interaction!.SelectBuildingBoxFromHud(stackId);
        InvalidateAll();
    }

    private void SelectResidentFromBuildingManagement(string residentId)
    {
        CloseManagementOverlay();
        _interaction!.SelectResidentFromHud(residentId);
        InvalidateAll();
    }

    private void SelectBuildingBoxTransformationFromManagement(string jobId)
    {
        CloseManagementOverlay();
        _interaction!.SelectJobFromHud(jobId);
        InvalidateAll();
    }

    private static string BuildingStatusLabel(BuildingStatus status)
    {
        return DigManagementLocalization.Resolve("management.building.status."
            + status.ToString().ToLowerInvariant());
    }
}

}
