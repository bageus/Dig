using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void SelectResidentTab()
    {
        SelectRightPanelTab(RightPanelTab.Residents);
    }

    private void SelectBuildingTab()
    {
        SelectRightPanelTab(RightPanelTab.Buildings);
    }

    private void SelectJobTab()
    {
        SelectRightPanelTab(RightPanelTab.Jobs);
    }

    private void SelectRightPanelTab(RightPanelTab tab)
    {
        _rightTab = tab;
        _lastRosterSignature = string.Empty;
        RefreshRoster();
    }

    private void RefreshRoster()
    {
        string? selectedResidentId = _agentRenderer!.SelectedCount == 1
            ? _agentRenderer.SelectedAgentId
            : null;
        ResidentRosterViewModel residentRoster =
            _simulation!.LoadResidentRoster(selectedResidentId);
        IReadOnlyList<ResidentRosterRowViewModel> residents = residentRoster.Rows;
        IReadOnlyList<BuildingWorldViewModel> buildings = _terrainSession!
            .LoadBuildings()
            .OrderBy(building => building.Name, StringComparer.Ordinal)
            .ThenBy(building => building.Id, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<WorldItemViewModel> buildingBoxes = _terrainSession
            .LoadAllWorldItems()
            .Where(item => item.IsBuildingBox)
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<JobOverlayViewModel> jobs = _terrainSession.LoadJobs()
            .OrderBy(job => IsTerminalStatus(job.Status))
            .ThenByDescending(job => job.Priority)
            .ThenBy(job => job.Description, StringComparer.Ordinal)
            .ThenBy(job => job.Id, StringComparer.Ordinal)
            .ToArray();
        SetButtonActive(_residentTabButton, _rightTab == RightPanelTab.Residents);
        SetButtonActive(_buildingTabButton, _rightTab == RightPanelTab.Buildings);
        SetButtonActive(_jobTabButton, _rightTab == RightPanelTab.Jobs);
        if (_rightTab == RightPanelTab.Residents)
        {
            RefreshResidentRows(residentRoster);
            return;
        }

        string signature = BuildRosterSignature(residents, buildings, buildingBoxes, jobs);
        if (string.Equals(signature, _lastRosterSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastRosterSignature = signature;
        ResetResidentRowPool();
        ClearChildren(_rightContent!);
        switch (_rightTab)
        {
            case RightPanelTab.Buildings:
                BuildBuildingRows(buildings, buildingBoxes);
                break;
            case RightPanelTab.Jobs:
                BuildJobRows(jobs, residents);
                break;
            default:
                throw new InvalidOperationException("Unknown right-panel tab.");
        }
    }

    private void BuildBuildingRows(
        IReadOnlyList<BuildingWorldViewModel> buildings,
        IReadOnlyList<WorldItemViewModel> buildingBoxes)
    {
        if (buildings.Count == 0 && buildingBoxes.Count == 0)
        {
            AddEmptyRosterMessage("No completed buildings");
            return;
        }

        string? selectedId = _buildingRenderer!.SelectedBuildingId;
        for (int index = 0; index < buildings.Count; index++)
        {
            BuildingWorldViewModel building = buildings[index];
            string id = building.Id;
            bool isSelected = string.Equals(id, selectedId, StringComparison.Ordinal);
            string marker = isSelected ? "■ " : string.Empty;
            string label = marker
                + building.Name
                + $" · {building.Status} · Cell {building.OriginX},{building.OriginY}";
            Button row = CreateButton(
                $"Building {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectBuildingFromHud(id),
                preferredHeight: 36f);
            ConfigureSingleLineRosterRow(row);
            if (isSelected)
            {
                row.GetComponent<Image>().color =
                    new Color(0.48f, 0.36f, 0.22f, 0.96f);
            }
        }

        string? selectedBoxId = _interaction!.SelectedBuildingBox?.StackId;
        for (int index = 0; index < buildingBoxes.Count; index++)
        {
            WorldItemViewModel box = buildingBoxes[index];
            string id = box.StackId;
            bool isSelected = string.Equals(id, selectedBoxId, StringComparison.Ordinal);
            string marker = isSelected ? "■ " : string.Empty;
            string label = marker
                + "BuildingBox"
                + $" · Cell {box.CellX},{box.CellY}, Z{box.CellZ}";
            Button row = CreateButton(
                $"BuildingBox {id}",
                _rightContent!,
                label,
                () => _interaction.SelectBuildingBoxFromHud(id),
                preferredHeight: 36f);
            ConfigureSingleLineRosterRow(row);
            if (isSelected)
            {
                row.GetComponent<Image>().color =
                    new Color(0.52f, 0.34f, 0.16f, 0.96f);
            }
        }
    }

    private void BuildJobRows(
        IReadOnlyList<JobOverlayViewModel> jobs,
        IReadOnlyList<ResidentRosterRowViewModel> residents)
    {
        if (jobs.Count == 0)
        {
            AddEmptyRosterMessage("No jobs");
            return;
        }

        Dictionary<string, string> names = residents.ToDictionary(
            resident => resident.Id,
            resident => resident.Name,
            StringComparer.Ordinal);
        string? selectedId = _jobRenderer!.SelectedJobId;
        for (int index = 0; index < jobs.Count; index++)
        {
            JobOverlayViewModel job = jobs[index];
            string id = job.Id;
            bool isSelected = string.Equals(id, selectedId, StringComparison.Ordinal);
            string worker = job.AssignedAgentId != null
                && names.TryGetValue(job.AssignedAgentId, out string? name)
                    ? name
                    : "Unassigned";
            string marker = isSelected ? "▲ " : string.Empty;
            string label = marker
                + job.Description
                + $" · {job.Status} · P{job.Priority} · {worker}";
            Button row = CreateButton(
                $"Job {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectJobFromHud(id),
                preferredHeight: 36f);
            ConfigureSingleLineRosterRow(row);
            if (isSelected)
            {
                row.GetComponent<Image>().color =
                    new Color(0.46f, 0.30f, 0.54f, 0.96f);
            }
        }
    }

    private static void ConfigureSingleLineRosterRow(Button row)
    {
        Text label = row.GetComponentInChildren<Text>();
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 16;
    }

    private void AddEmptyRosterMessage(string message)
    {
        Text text = CreateText(
            "Empty",
            _rightContent!,
            message,
            15,
            TextAnchor.MiddleCenter);
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 44f;
    }

    private string BuildRosterSignature(
        IReadOnlyList<ResidentRosterRowViewModel> residents,
        IReadOnlyList<BuildingWorldViewModel> buildings,
        IReadOnlyList<WorldItemViewModel> buildingBoxes,
        IReadOnlyList<JobOverlayViewModel> jobs)
    {
        string residentVersions = string.Join(
            ",",
            residents.Select(resident =>
                $"{resident.Id}:{resident.Version}:{resident.IsExpanded}:"
                + $"{resident.ScheduledActivity}:{resident.IsIdleAtWork}:"
                + $"{resident.Health.Value}:{resident.Nutrition.Value}:"
                + $"{resident.Alertness.Value}:{resident.Mood.Value}:"
                + $"{resident.Activity.Kind}:{resident.Activity.ProgressCurrent}:"
                + $"{resident.Activity.BlockReasonCode}"));
        string buildingVersions = string.Join(
            ",",
            buildings.Select(building => $"{building.Id}:{building.Version}"));
        string buildingBoxVersions = string.Join(
            ",",
            buildingBoxes.Select(item =>
                $"{item.StackId}:{item.Quantity}:{item.ReservedQuantity}:"
                + $"{item.CellX}:{item.CellY}:{item.CellZ}"));
        string jobVersions = string.Join(
            ",",
            jobs.Select(job =>
                $"{job.Id}:{job.Status}:{job.Stage}:{job.Priority}:"
                + $"{job.AssignedAgentId}:{job.RetryCount}"));
        return $"{_rightTab}|{_agentRenderer!.SelectedAgentId}|"
            + $"{_buildingRenderer!.SelectedBuildingId}|{_jobRenderer!.SelectedJobId}|"
            + $"{_interaction!.SelectedBuildingBox?.StackId}|"
            + $"{residentVersions}|{buildingVersions}|{buildingBoxVersions}|{jobVersions}";
    }

    private static bool IsTerminalStatus(string status)
    {
        return string.Equals(status, "Completed", StringComparison.Ordinal)
            || string.Equals(status, "Cancelled", StringComparison.Ordinal)
            || string.Equals(status, "Failed", StringComparison.Ordinal);
    }
}

}
