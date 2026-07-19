using System;
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
    private void RefreshContextPanel()
    {
        if (_interaction!.HasActiveBuildingPlacement)
        {
            ShowBuildingPlacement();
            return;
        }

        BuildingWorldViewModel? building = _buildingRenderer!.SelectedModel;
        if (building != null)
        {
            ShowBuildingFunctions(building);
            return;
        }

        JobOverlayViewModel? job = _jobRenderer!.SelectedModel;
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
            preferredWidth: 1200f);
        RectTransform row = CreateHorizontalRow("Tools", section, 44f);
        Button tunnel = CreateButton("Tunnel", row, "Tunnel", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Tunnel));
        Button depth = CreateButton("Depth", row, "Depth", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Depth));
        Button erase = CreateButton("Erase", row, "Erase", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Delete));
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

        RectTransform rooms = CreateHorizontalRow("Rooms", section, 44f);
        Button small = CreateButton("Small Room", rooms, "□", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Small));
        Button medium = CreateButton("Medium Room", rooms, "▭", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Medium));
        Button large = CreateButton("Large Room", rooms, "▰", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Large));
        Button tall = CreateButton("Tall Room", rooms, "▯", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Tall));
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

        BeginBottomLayout();
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

        BeginBottomLayout();
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

    private void BeginBottomLayout()
    {
        _bottomPanel!.gameObject.SetActive(true);
        ClearChildren(_bottomContent!);
        HorizontalLayoutGroup layout =
            _bottomContent!.gameObject.GetComponent<HorizontalLayoutGroup>()
            ?? _bottomContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
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