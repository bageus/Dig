using Dig.Domain.World;
using Dig.Presentation.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{
public sealed partial class DigGameHudCanvas
{
    private bool TryShowSelectedRoomPurpose()
    {
        RoomPurposeViewModel? room = _interaction!.SelectedRoomPurpose;
        if (room == null)
        {
            return false;
        }

        ShowRoomPurpose(room);
        return true;
    }

    private void ShowRoomPurpose(RoomPurposeViewModel room)
    {
        string signature = $"room:{room.RoomId}:{room.Version}:" +
            $"{room.RequestedPurpose}:{room.ImprovementStatus}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout();
        RectTransform section = CreateSection(
            "Room Purpose",
            _bottomContent!,
            room.TemplateId,
            preferredWidth: 1240f);
        RectTransform row = CreateHorizontalRow("Room Types", section, 56f);
        AddPurposeButton(row, room, RoomPurposeKind.None, "None");
        AddPurposeButton(row, room, RoomPurposeKind.Bedroom, "Bedroom");
        AddPurposeButton(row, room, RoomPurposeKind.KitchenDining, "Kitchen");
        AddPurposeButton(row, room, RoomPurposeKind.Workshop, "Workshop");
        AddPurposeButton(row, room, RoomPurposeKind.Farm, "Farm");
        CreateText(
            "Room State",
            section,
            $"Requested: {room.RequestedPurpose}   Active: {room.ActivePurpose}   " +
                $"Upgrade: {room.ImprovementStatus}",
            15,
            TextAnchor.MiddleLeft);
    }

    private void AddPurposeButton(
        RectTransform row,
        RoomPurposeViewModel room,
        RoomPurposeKind purpose,
        string label)
    {
        Button button = CreateButton(
            $"Room purpose {purpose}",
            row,
            label,
            () =>
            {
                _legacyHud!.SetCommandResult(
                    _interaction!.ChangeSelectedRoomPurpose(purpose));
                InvalidateAll();
            },
            preferredHeight: 52f);
        SetButtonActive(button, room.RequestedPurpose == purpose);
    }
}
}
