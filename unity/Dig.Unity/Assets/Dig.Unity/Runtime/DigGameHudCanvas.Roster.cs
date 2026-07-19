using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
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
        IReadOnlyList<AgentViewModel> residents = _agentRenderer!.GetHudModels()
            .OrderBy(resident => resident.Name, StringComparer.Ordinal)
            .ThenBy(resident => resident.Id, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<BuildingWorldViewModel> buildings = _terrainSession!
            .LoadBuildings()
            .Where(building => building.IsSelectable)
            .OrderBy(building => building.Name, StringComparer.Ordinal)
            .ThenBy(building => building.Id, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<JobOverlayViewModel> jobs = _terrainSession.LoadJobs()
            .OrderBy(job => IsTerminalStatus(job.Status))
            .ThenByDescending(job => job.Priority)
            .ThenBy(job => job.Description, StringComparer.Ordinal)
            .ThenBy(job => job.Id, StringComparer.Ordinal)
            .ToArray();
        string signature = BuildRosterSignature(residents, buildings, jobs);
        if (string.Equals(signature, _lastRosterSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastRosterSignature = signature;
        SetButtonActive(_residentTabButton, _rightTab == RightPanelTab.Residents);
        SetButtonActive(_buildingTabButton, _rightTab == RightPanelTab.Buildings);
        SetButtonActive(_jobTabButton, _rightTab == RightPanelTab.Jobs);
        ClearChildren(_rightContent!);
        switch (_rightTab)
        {
            case RightPanelTab.Residents:
                BuildResidentRows(residents);
                break;
            case RightPanelTab.Buildings:
                BuildBuildingRows(buildings);
                break;
            case RightPanelTab.Jobs:
                BuildJobRows(jobs, residents);
                break;
            default:
                throw new InvalidOperationException("Unknown right-panel tab.");
        }
    }

    private void BuildResidentRows(IReadOnlyList<AgentViewModel> residents)
    {
        if (residents.Count == 0)
        {
            AddEmptyRosterMessage("No dwarfs");
            return;
        }

        string? selectedId = _agentRenderer!.SelectedAgentId;
        for (int index = 0; index < residents.Count; index++)
        {
            AgentViewModel resident = residents[index];
            string id = resident.Id;
            bool isSelected = string.Equals(id, selectedId, StringComparison.Ordinal);
            string marker = isSelected ? "◆ " : string.Empty;
            string label = marker
                + resident.Name
                + $"\nHealth {resident.Health / 100}% · {resident.ActiveIntent}";
            Button row = CreateButton(
                $"Resident {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectResidentFromHud(id),
                preferredHeight: 58f);
            if (isSelected)
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
                + $"\nCell {building.OriginX},{building.OriginY}";
            Button row = CreateButton(
                $"Building {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectBuildingFromHud(id),
                preferredHeight: 58f);
            if (isSelected)
            {
                row.GetComponent<Image>().color =
                    new Color(0.48f, 0.36f, 0.22f, 0.96f);
            }
        }
    }

    private void BuildJobRows(
        IReadOnlyList<JobOverlayViewModel> jobs,
        IReadOnlyList<AgentViewModel> residents)
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
                + $"\n{job.Status} · P{job.Priority} · {worker}";
            Button row = CreateButton(
                $"Job {id}",
                _rightContent!,
                label,
                () => _interaction!.SelectJobFromHud(id),
                preferredHeight: 58f);
            if (isSelected)
            {
                row.GetComponent<Image>().color =
                    new Color(0.46f, 0.30f, 0.54f, 0.96f);
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
        IReadOnlyList<BuildingWorldViewModel> buildings,
        IReadOnlyList<JobOverlayViewModel> jobs)
    {
        string residentVersions = string.Join(
            ",",
            residents.Select(resident => $"{resident.Id}:{resident.Version}"));
        string buildingVersions = string.Join(
            ",",
            buildings.Select(building => $"{building.Id}:{building.Version}"));
        string jobVersions = string.Join(
            ",",
            jobs.Select(job =>
                $"{job.Id}:{job.Status}:{job.Stage}:{job.Priority}:"
                + $"{job.AssignedAgentId}:{job.RetryCount}"));
        return $"{_rightTab}|{_agentRenderer!.SelectedAgentId}|"
            + $"{_buildingRenderer!.SelectedBuildingId}|{_jobRenderer!.SelectedJobId}|"
            + $"{residentVersions}|{buildingVersions}|{jobVersions}";
    }

    private static bool IsTerminalStatus(string status)
    {
        return string.Equals(status, "Completed", StringComparison.Ordinal)
            || string.Equals(status, "Cancelled", StringComparison.Ordinal)
            || string.Equals(status, "Failed", StringComparison.Ordinal);
    }
}

}