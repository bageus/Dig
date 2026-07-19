using System;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
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
        string signature = "excavation:"
            + $"{_interaction!.ExcavationModeLabel}:"
            + $"{_interaction.ExcavationPriority}:"
            + $"{_interaction.CaveRoomPreset}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout();
        RectTransform section = CreateSection(
            "Excavation",
            _bottomContent!,
            "КОПКА",
            preferredWidth: 1200f);
        RectTransform row = CreateHorizontalRow("Tools", section, 44f);
        CreateButton("Off", row, "Выкл", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.None));
        CreateButton("Tunnel", row, "Тоннель", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Tunnel));
        CreateButton("Depth", row, "Глубина", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Depth));
        CreateButton("Delete", row, "Ластик", () =>
            _interaction.SetExcavationDrawingMode(DigExcavationDrawingMode.Delete));
        CreateButton("Priority Down", row, "Приоритет −", () =>
            _interaction.AdjustExcavationPriority(-50));
        CreateButton("Priority Up", row, "Приоритет +", () =>
            _interaction.AdjustExcavationPriority(50));

        RectTransform rooms = CreateHorizontalRow("Rooms", section, 44f);
        CreateButton("Small Room", rooms, "Малая", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Small));
        CreateButton("Medium Room", rooms, "Средняя", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Medium));
        CreateButton("Large Room", rooms, "Большая", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Large));
        CreateButton("Tall Room", rooms, "Высокая", () =>
            _interaction.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Tall));
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
            $"Прочность {functions.Durability}/{functions.MaximumDurability}",
            18,
            TextAnchor.MiddleCenter);
        details.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
        BuildingFunctionActionViewModel action = functions.Actions[0];
        Button pack = CreateButton(
            "Pack",
            section,
            functions.IsPacking ? "Упаковка выполняется" : "Упаковать в коробку",
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
            "РАЗМЕЩЕНИЕ СТРОЕНИЯ",
            preferredWidth: 900f);
        string state = _interaction.BuildingPlacementValid
            ? "Позиция допустима — ЛКМ подтверждает"
            : "Недопустимо: " + (_interaction.BuildingPlacementReasonCode ?? "нет клетки");
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
            "Отменить",
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
