using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void RefreshBuildingManagement()
    {
        IReadOnlyList<BuildingWorldViewModel> buildings =
            _terrainSession!.LoadBuildings();
        string signature = "buildings:"
            + string.Join("|", buildings.Select(value =>
                value.Id + ":" + value.Version + ":" + value.Status));
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
        BuildBuildingManagementTable(buildings);
    }

    private void BuildBuildingManagementTable(
        IReadOnlyList<BuildingWorldViewModel> buildings)
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
        if (buildings.Count == 0)
        {
            BuildManagementEmptyState(DigHudLocalization.Resolve("management.buildings.empty"));
            return;
        }

        foreach (BuildingWorldViewModel building in buildings)
        {
            RectTransform row = CreateManagementRow(building.Id, 38f);
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
    }

    private static string BuildingStatusLabel(BuildingStatus status)
    {
        return DigHudLocalization.Resolve("management.building.status."
            + status.ToString().ToLowerInvariant());
    }
}

}
