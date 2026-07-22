using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Management;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void RefreshItemManagement()
    {
        SettlementItemSummaryViewModel model = _terrainSession!.LoadItemSummary();
        string signature = "items:" + _itemManagementTab + ":"
            + model.InventoryVersion + ":"
            + string.Join("|", model.Locations.Select(value => value.Id + ":" + value.Label));
        if (string.Equals(signature, _managementSignature, StringComparison.Ordinal))
        {
            return;
        }

        _managementSignature = signature;
        string[] tabs = { "Materials", "Weapons", "Food", "Potions", "Tools" };
        BeginManagementOverlay(
            "Items",
            tabs,
            (int)_itemManagementTab,
            SelectItemManagementTab);
        BuildItemManagementTable(model);
    }

    private void SelectItemManagementTab(int index)
    {
        _itemManagementTab = (SettlementItemGroup)index;
        _managementSignature = string.Empty;
        RefreshItemManagement();
    }

    private void BuildItemManagementTable(SettlementItemSummaryViewModel model)
    {
        List<ManagementColumn> columns = new List<ManagementColumn>
        {
            Column("management.item", 230f),
            Column("management.total", 110f),
        };
        columns.AddRange(model.Locations.Select(value =>
            new ManagementColumn(value.Label, 170f)));
        BuildManagementHeader(columns);
        IReadOnlyList<SettlementItemSummaryRowViewModel> rows =
            model.GetRows(_itemManagementTab);
        if (rows.Count == 0)
        {
            BuildManagementEmptyState(DigHudLocalization.Resolve("management.items.empty"));
            return;
        }

        foreach (SettlementItemSummaryRowViewModel row in rows)
        {
            RectTransform visual = CreateManagementRow(row.Id, 36f);
            CreateManagementTextCell(visual, row.Label, columns[0].Width);
            CreateManagementTextCell(
                visual,
                row.Total.ToString(),
                columns[1].Width,
                TextAnchor.MiddleCenter);
            for (int index = 0; index < model.Locations.Count; index++)
            {
                CreateManagementTextCell(
                    visual,
                    row.GetQuantity(model.Locations[index].Id).ToString(),
                    columns[index + 2].Width,
                    TextAnchor.MiddleCenter);
            }
        }
    }
}

}
