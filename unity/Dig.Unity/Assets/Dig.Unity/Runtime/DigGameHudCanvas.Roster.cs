using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void SelectResidentTab()
    {
        _rightTab = RightPanelTab.Residents;
        _lastRosterSignature = string.Empty;
    }

    private void SelectBuildingTab()
    {
        _rightTab = RightPanelTab.Buildings;
        _lastRosterSignature = string.Empty;
    }

    private void RefreshRoster()
    {
        IReadOnlyList<AgentViewModel> residents = _agentRenderer!.GetHudModels();
        IReadOnlyList<BuildingWorldViewModel> buildings = _terrainSession!
            .LoadBuildings()
            .Where(building => building.IsSelectable)
            .OrderBy(building => building.Name, StringComparer.Ordinal)
            .ThenBy(building => building.Id, StringComparer.Ordinal)
            .ToArray();
        string signature = BuildRosterSignature(residents, buildings);
        if (string.Equals(signature, _lastRosterSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastRosterSignature = signature;
        SetButtonActive(_residentTabButton, _rightTab == RightPanelTab.Residents);
        SetButtonActive(_buildingTabButton, _rightTab == RightPanelTab.Buildings);
        ClearChildren(_rightContent!);
        if (_rightTab == RightPanelTab.Residents)
        {
            BuildResidentRows(residents);
        }
        else
        {
            BuildBuildingRows(buildings);
        }
    }

    private void BuildResidentRows(IReadOnlyList<AgentViewModel> residents)
    {
        if (residents.Count == 0)
        {
            AddEmptyRosterMessage("Нет гномов");
            return;
        }

        string? selectedId = _agentRenderer!.SelectedAgentId;
        for (int index = 0; index < residents.Count; index++)
        {
            AgentViewModel resident = residents[index];
            string id = resident.Id;
            string selected = string.Equals(id, selectedId, StringComparison.Ordinal)
                ? "◆ "
                : string.Empty;
            string label = selected
                + resident.Name
                + $"\nЗдоровье {resident.Health / 100}% · {resident.ActiveIntent}";
            Button row = CreateButton(
                $"Resident {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectResidentFromHud(id),
                preferredHeight: 58f);
            if (selected.Length > 0)
            {
                row.GetComponent<Image>().color =
                    new Color(0.30f, 0.48f, 0.64f, 0.96f);
            }
        }
    }

    private void BuildBuildingRows(IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        if (buildings.Count == 0)
        {
            AddEmptyRosterMessage("Нет завершённых строений");
            return;
        }

        string? selectedId = _buildingRenderer!.SelectedBuildingId;
        for (int index = 0; index < buildings.Count; index++)
        {
            BuildingWorldViewModel building = buildings[index];
            string id = building.Id;
            string selected = string.Equals(id, selectedId, StringComparison.Ordinal)
                ? "■ "
                : string.Empty;
            string label = selected
                + building.Name
                + $"\nКлетка {building.OriginX},{building.OriginY}";
            Button row = CreateButton(
                $"Building {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectBuildingFromHud(id),
                preferredHeight: 58f);
            if (selected.Length > 0)
            {
                row.GetComponent<Image>().color =
                    new Color(0.48f, 0.36f, 0.22f, 0.96f);
            }
        }
    }

    private void AddEmptyRosterMessage(string message)
    {
        Text text = CreateText(
            "Empty",
            _rightContent!,
            message,
            17,
            TextAnchor.MiddleCenter);
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 48f;
    }

    private string BuildRosterSignature(
        IReadOnlyList<AgentViewModel> residents,
        IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        string residentVersions = string.Join(
            ",",
            residents.Select(resident => $"{resident.Id}:{resident.Version}"));
        string buildingVersions = string.Join(
            ",",
            buildings.Select(building => $"{building.Id}:{building.Version}"));
        return $"{_rightTab}|{_agentRenderer!.SelectedAgentId}|"
            + $"{_buildingRenderer!.SelectedBuildingId}|"
            + $"{residentVersions}|{buildingVersions}";
    }
}

}
