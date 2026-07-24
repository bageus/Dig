using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
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
        IReadOnlyList<BuildingWorldViewModel> buildings =
            _terrainSession!.LoadBuildings();
        IReadOnlyList<WorldItemViewModel> buildingBoxes = _terrainSession
            .LoadAllWorldItems()
            .Where(item => item.IsBuildingBox)
            .ToArray();
        string signature = "buildings:"
            + string.Join("|", buildings.Select(value =>
                value.Id + ":" + value.Version + ":" + value.Status))
            + ":boxes:"
            + string.Join("|", buildingBoxes.Select(value =>
                value.StackId + ":" + value.Quantity + ":" + value.ReservedQuantity
                + ":" + value.CellX + ":" + value.CellY + ":" + value.CellZ));
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
        BuildBuildingManagementTable(buildings, buildingBoxes);
    }

    private void BuildBuildingManagementTable(
        IReadOnlyList<BuildingWorldViewModel> buildings,
        IReadOnlyList<WorldItemViewModel> buildingBoxes)
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
        if (buildings.Count == 0 && buildingBoxes.Count == 0)
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
            CreateManagementTextCell(row, "BuildingBox", columns[0].Width);
            CreateManagementTextCell(row, box.ItemId, columns[1].Width);
            CreateManagementTextCell(
                row,
                box.ReservedQuantity == 0 ? "Packed" : "Reserved",
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

    private static string BuildingStatusLabel(BuildingStatus status)
    {
        return DigManagementLocalization.Resolve("management.building.status."
            + status.ToString().ToLowerInvariant());
    }
}

}
