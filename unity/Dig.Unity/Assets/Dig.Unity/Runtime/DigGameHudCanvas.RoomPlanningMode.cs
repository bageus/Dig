using System;
using Dig.Application.World;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void ShowExcavationPalette()
    {
        if (!_lastContextSignature.StartsWith(
                "excavation:",
                StringComparison.Ordinal))
        {
            _interaction!.OpenExcavationMenuInDigMode();
        }

        _interaction!.EnsureDefaultExcavationDrawingMode();
        string signature = $"excavation:{_interaction.ExcavationModeLabel}:"
            + $"{_interaction.CaveRoomPreset}:{_interaction.RoomUpgradeMode}:"
            + $"{_interaction.IsRoomUpgradeModeUnlocked}";
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
        if (_interaction.IsRoomUpgradeModeUnlocked)\n        {\n            AddRoomPlanningModeToggle(row);\n        }

        AddExcavationDrawingButtons(row);
        AddCaveRoomPlanningButtons(row);
    }

    private void AddExcavationDrawingButtons(RectTransform row)
    {
        Button tunnel = CreateButton("Tunnel", row, "Tunnel", () =>
            _interaction!.SetExcavationDrawingMode(DigExcavationDrawingMode.Tunnel),
            preferredHeight: 52f);
        Button depth = CreateButton("Depth", row, "Depth", () =>
            _interaction!.SetExcavationDrawingMode(DigExcavationDrawingMode.Depth),
            preferredHeight: 52f);
        Button erase = CreateButton("Erase", row, "Erase", () =>
            _interaction!.SetExcavationDrawingMode(DigExcavationDrawingMode.Delete),
            preferredHeight: 52f);
        SetButtonActive(
            tunnel,
            _interaction!.ExcavationDrawingMode == DigExcavationDrawingMode.Tunnel
                && !_interaction.CaveRoomPreset.HasValue);
        SetButtonActive(
            depth,
            _interaction.ExcavationDrawingMode == DigExcavationDrawingMode.Depth);
        SetButtonActive(
            erase,
            _interaction.ExcavationDrawingMode == DigExcavationDrawingMode.Delete);
    }

    private void AddCaveRoomPlanningButtons(RectTransform row)
    {
        Button small = CreateRoomIconButton(
            "Small Room",
            row,
            new Vector2(18f, 18f),
            () => _interaction!.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Small));
        Button medium = CreateRoomIconButton(
            "Medium Room",
            row,
            new Vector2(30f, 18f),
            () => _interaction!.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Medium));
        Button large = CreateRoomIconButton(
            "Large Room",
            row,
            new Vector2(38f, 22f),
            () => _interaction!.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Large));
        Button tall = CreateRoomIconButton(
            "Tall Room",
            row,
            new Vector2(18f, 32f),
            () => _interaction!.SetCaveRoomPlanningPreset(CaveRoomPresetKind.Tall));
        SetButtonActive(small, _interaction!.CaveRoomPreset == CaveRoomPresetKind.Small);
        SetButtonActive(medium, _interaction.CaveRoomPreset == CaveRoomPresetKind.Medium);
        SetButtonActive(large, _interaction.CaveRoomPreset == CaveRoomPresetKind.Large);
        SetButtonActive(tall, _interaction.CaveRoomPreset == CaveRoomPresetKind.Tall);
    }

    private void AddRoomPlanningModeToggle(RectTransform row)
    {
        Button toggle = CreateButton("Room Types Toggle", row, "Room Types", () =>
            _interaction!.SetRoomUpgradeMode(!_interaction.RoomUpgradeMode),
            preferredHeight: 52f);
        SetButtonActive(toggle, _interaction!.RoomUpgradeMode);
    }
}

}
