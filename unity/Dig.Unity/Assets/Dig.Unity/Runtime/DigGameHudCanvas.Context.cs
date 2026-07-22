using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const float CompactBottomPanelHeight = 98f;

    private void RefreshContextPanel()
    {
        if (_interaction!.HasActiveBuildingPlacement)
        {
            ShowBuildingPlacement();
            return;
        }

        if (_technologyDescriptionId != null)
        {
            ShowTechnologyDescriptionPanel(_technologyDescriptionId);
            return;
        }

        BuildingWorldViewModel? building = _buildingRenderer!.SelectedModel;
        if (building != null)
        {
            ShowBuildingFunctions(building);
            return;
        }

        string? selectedJobId = _jobRenderer!.SelectedJobId;
        JobOverlayViewModel? job = selectedJobId == null
            ? null
            : _terrainSession!.LoadJobs().FirstOrDefault(value =>
                string.Equals(value.Id, selectedJobId, StringComparison.Ordinal));
        if (job != null)
        {
            ShowJobDetails(job);
            return;
        }

        if (_agentRenderer!.SelectedCount > 1)
        {
            ShowNoGroupContext();
            return;
        }

        if (_agentRenderer.SelectedCount == 1
            && _agentRenderer.SelectedAgentId != null)
        {
            ResidentInventoryLayoutViewModel inventory = _terrainSession!
                .LoadResidentInventoryLayout(_agentRenderer.SelectedAgentId);
            string signature = $"resident:{inventory.ResidentId}:"
                + $"{inventory.InventoryVersion}:{inventory.MoveSpeedMultiplier}";
            if (ApplyContextSignature(signature))
            {
                BuildInventoryContext(inventory);
            }

            return;
        }

        ShowExcavationPalette();
    }

    private void ShowNoGroupContext()
    {
        string signature = $"group:{_agentRenderer!.SelectedCount}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        _bottomPanel!.gameObject.SetActive(false);
    }

    private void ShowExcavationPalette()
    {
        _interaction!.EnsureDefaultExcavationDrawingMode();
        string signature = "excavation:"
            + $"{_interaction.ExcavationModeLabel}:"
            + $"{_interaction.CaveRoomPreset}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout();
        RectTransform section = CreateSection(
            "Excavation",
            _bottomContent!,
            string.Empty,
            preferredWidth: 1240f);
        RectTransform row = CreateHorizontalRow("Excavation Tools", section, 56f);
        Button tunnel = CreateButton("Tunnel", row, "Tunnel", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Tunnel),
            preferredHeight: 52f);
        Button depth = CreateButton("Depth", row, "Depth", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Depth),
            preferredHeight: 52f);
        Button erase = CreateButton("Erase", row, "Erase", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Delete),
            preferredHeight: 52f);
        SetButtonActive(
            tunnel,
            _interaction.ExcavationDrawingMode == DigExcavationDrawingMode.Tunnel
                && !_interaction.CaveRoomPreset.HasValue);
        SetButtonActive(
            depth,
            _interaction.ExcavationDrawingMode == DigExcavationDrawingMode.Depth);
        SetButtonActive(
            erase,
            _interaction.ExcavationDrawingMode == DigExcavationDrawingMode.Delete);

        Button small = CreateRoomIconButton(
            "Small Room",
            row,
            new Vector2(18f, 18f),
            () => _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Small));
        Button medium = CreateRoomIconButton(
            "Medium Room",
            row,
            new Vector2(30f, 18f),
            () => _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Medium));
        Button large = CreateRoomIconButton(
            "Large Room",
            row,
            new Vector2(38f, 22f),
            () => _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Large));
        Button tall = CreateRoomIconButton(
            "Tall Room",
            row,
            new Vector2(18f, 32f),
            () => _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Tall));
        SetButtonActive(small, _interaction.CaveRoomPreset == CaveRoomPresetKind.Small);
        SetButtonActive(medium, _interaction.CaveRoomPreset == CaveRoomPresetKind.Medium);
        SetButtonActive(large, _interaction.CaveRoomPreset == CaveRoomPresetKind.Large);
        SetButtonActive(tall, _interaction.CaveRoomPreset == CaveRoomPresetKind.Tall);
    }

    private void ShowJobDetails(JobOverlayViewModel job)
    {
        string signature = $"job:{job.Id}:{job.Status}:{job.Stage}:"
            + $"{job.AssignedAgentId}:{job.Priority}:{job.RetryCount}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout();
        RectTransform section = CreateSection(
            "Job Details",
            _bottomContent!,
            job.Description.ToUpperInvariant(),
            preferredWidth: 900f);
        string target = job.HasTarget
            ? $" · Target {job.TargetX},{job.TargetY}"
            : string.Empty;
        Text details = CreateText(
            "Details",
            section,
            $"{job.Status} · {job.Stage} · Priority {job.Priority}"
                + $" · Worker {job.AssignedAgentId ?? "Unassigned"}{target}",
            18,
            TextAnchor.MiddleCenter);
        details.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
    }

    private void ShowBuildingFunctions(BuildingWorldViewModel building)
    {
        BuildingFunctionsViewModel functions = building.Functions;
        string signature = $"building:{building.Id}:{building.Version}:"
            + $"{functions.IsPacking}:{functions.PackingCompletedWork}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout(142f);
        RectTransform section = CreateSection(
            "Building Functions",
            _bottomContent!,
            building.Name.ToUpperInvariant(),
            preferredWidth: 900f);
        Text details = CreateText(
            "Details",
            section,
            $"Durability {functions.Durability}/{functions.MaximumDurability}",
            18,
            TextAnchor.MiddleCenter);
        details.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
        BuildingFunctionActionViewModel action = functions.Actions[0];
        Button pack = CreateButton(
            "Pack",
            section,
            functions.IsPacking ? "Packing in progress" : "Pack into a box",
            () => ExecutePacking(building.Id),
            preferredHeight: 44f);
        pack.interactable = action.IsEnabled;
    }

    private void ShowBuildingPlacement()
    {
        string signature = $"placement:{_interaction!.BuildingPlacementValid}:"
            + _interaction.BuildingPlacementReasonCode;
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout(142f);
        RectTransform section = CreateSection(
            "Building Placement",
            _bottomContent!,
            "BUILDING PLACEMENT",
            preferredWidth: 900f);
        string state = _interaction.BuildingPlacementValid
            ? "Valid position — LMB confirms"
            : "Invalid: " + (_interaction.BuildingPlacementReasonCode ?? "no cell");
        Text message = CreateText(
            "Placement State",
            section,
            state,
            19,
            TextAnchor.MiddleCenter);
        message.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        CreateButton(
            "Cancel Placement",
            section,
            "Cancel",
            _interaction.CancelBuildingPlacementFromHud,
            preferredHeight: 42f);
    }

    private void ExecutePacking(string buildingId)
    {
        long tick = _simulation?.CurrentTick ?? 0;
        Result result = _terrainSession!.StartBuildingPacking(buildingId, tick);
        _legacyHud!.SetCommandResult(result);
        if (result.IsFailure)
        {
            return;
        }

        _buildingRenderer!.Render(_terrainSession.LoadBuildings());
        _buildingRenderer.SelectById(buildingId);
        InvalidateAll();
    }

    private bool ApplyContextSignature(string signature)
    {
        if (string.Equals(signature, _lastContextSignature, StringComparison.Ordinal))
        {
            return false;
        }

        _lastContextSignature = signature;
        return true;
    }

    private void BeginBottomLayout(float height = CompactBottomPanelHeight)
    {
        _bottomPanel!.gameObject.SetActive(true);
        SetBottomPanelHeight(height);
        ClearChildren(_bottomContent!);
        HorizontalLayoutGroup layout =
            _bottomContent!.gameObject.GetComponent<HorizontalLayoutGroup>()
            ?? _bottomContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
    }

    private static RectTransform CreateHorizontalRow(
        string name,
        Transform parent,
        float preferredHeight)
    {
        RectTransform row = CreateRect(name, parent);
        LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = preferredHeight;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 7f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        return row;
    }
}

}
