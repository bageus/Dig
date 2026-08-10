using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Presentation.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private readonly Dictionary<string, RoomPurposeKind> _pendingRoomPurposes =
        new Dictionary<string, RoomPurposeKind>(StringComparer.Ordinal);

    private bool TryShowSelectedRoomInfrastructure()
    {
        RoomInfrastructureViewModel? room = _interaction!.SelectedRoomInfrastructure;
        if (room == null)
        {
            return false;
        }

        ShowRoomInfrastructure(room);
        return true;
    }

    private void ShowRoomInfrastructure(RoomInfrastructureViewModel room)
    {
        RoomPurposeKind displayedPurpose = room.UpgradeOrderCount == 0
            ? PendingPurpose(room.Id)
            : room.RequestedPurpose;
        string signature = $"room:{room.Id}:{room.Version}:{room.Status}:"
            + $"{room.UpgradeOrderCount}:{room.RequestedPurpose}:{room.ActivePurpose}:"
            + $"{room.DeliveredUnits}:{room.ConsumedUnits}:{displayedPurpose}:"
            + $"{room.BlockReason}:{room.CancellationAllowed}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout(188f);
        RectTransform section = CreateSection(
            "Room Infrastructure",
            _bottomContent!,
            $"{TemplateLabel(room.TemplateKind).ToUpperInvariant()} · {StatusLabel(room.Status)}",
            preferredWidth: 1240f);
        Text state = CreateText(
            "Room State",
            section,
            $"Order {room.UpgradeOrderCount}/1 · Requested: {PurposeLabel(displayedPurpose)}"
                + $" · Active: {PurposeLabel(room.ActivePurpose)}",
            16,
            TextAnchor.MiddleCenter);
        state.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        Text progress = CreateText(
            "Room Progress",
            section,
            FormatProgress(room),
            15,
            TextAnchor.MiddleCenter);
        progress.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform actions = CreateHorizontalRow("Room Actions", section, 38f);
        Button improve = CreateButton(
            "Improve Room",
            actions,
            $"Improve {room.UpgradeOrderCount}/1",
            () => OrderSelectedRoom(room),
            preferredHeight: 36f);
        improve.interactable = room.CanOrderUpgrade;
        Button cancel = CreateButton(
            "Cancel Room Upgrade",
            actions,
            "Cancel improvement",
            () => CancelSelectedRoom(room),
            preferredHeight: 36f);
        cancel.interactable = room.CancellationAllowed;

        RectTransform purposes = CreateHorizontalRow("Room Purposes", section, 38f);
        CreatePurposeButton(purposes, room, RoomPurposeKind.None, "None", displayedPurpose);
        CreatePurposeButton(purposes, room, RoomPurposeKind.Bedroom, "Bedroom", displayedPurpose);
        CreatePurposeButton(
            purposes,
            room,
            RoomPurposeKind.KitchenDining,
            "Kitchen",
            displayedPurpose);
        CreatePurposeButton(
            purposes,
            room,
            RoomPurposeKind.Workshop,
            "Workshop",
            displayedPurpose);
        CreatePurposeButton(purposes, room, RoomPurposeKind.Farm, "Farm", displayedPurpose);
    }

    private void CreatePurposeButton(
        Transform parent,
        RoomInfrastructureViewModel room,
        RoomPurposeKind purpose,
        string label,
        RoomPurposeKind displayedPurpose)
    {
        Button button = CreateButton(
            "Purpose " + purpose,
            parent,
            label,
            () => SelectRoomPurpose(room, purpose),
            preferredHeight: 36f);
        button.interactable = room.CanChangePurpose;
        SetButtonActive(button, displayedPurpose == purpose);
    }

    private void SelectRoomPurpose(
        RoomInfrastructureViewModel room,
        RoomPurposeKind purpose)
    {
        if (room.UpgradeOrderCount == 0)
        {
            _pendingRoomPurposes[room.Id] = purpose;
            _lastContextSignature = string.Empty;
            return;
        }

        Result result = _terrainSession!.ChangeRoomRequestedPurpose(
            room.Id,
            purpose,
            _simulation?.CurrentTick ?? 0);
        _legacyHud!.SetCommandResult(result);
        if (result.IsSuccess)
        {
            InvalidateAll();
        }
    }

    private void OrderSelectedRoom(RoomInfrastructureViewModel room)
    {
        Result result = _terrainSession!.OrderRoomUpgrade(
            room.Id,
            PendingPurpose(room.Id),
            _simulation?.CurrentTick ?? 0);
        _legacyHud!.SetCommandResult(result);
        if (result.IsSuccess)
        {
            InvalidateAll();
        }
    }

    private void CancelSelectedRoom(RoomInfrastructureViewModel room)
    {
        Result result = _terrainSession!.CancelRoomUpgrade(
            room.Id,
            _simulation?.CurrentTick ?? 0);
        _legacyHud!.SetCommandResult(result);
        if (result.IsSuccess)
        {
            _pendingRoomPurposes[room.Id] = RoomPurposeKind.None;
            InvalidateAll();
        }
    }

    private RoomPurposeKind PendingPurpose(string roomId)
    {
        return _pendingRoomPurposes.TryGetValue(roomId, out RoomPurposeKind purpose)
            ? purpose
            : RoomPurposeKind.None;
    }

    private static string FormatProgress(RoomInfrastructureViewModel room)
    {
        string materials = string.Join(
            " · ",
            room.Materials.Select(value =>
                $"{MaterialLabel(value.ItemId)} {value.Delivered}/{value.Required}"
                    + $" (completed {value.Consumed})"));
        string blocker = room.BlockReason ==
            Dig.Application.Rooms.RoomInfrastructureBlockReason.None
                ? string.Empty
                : " · " + BlockReasonLabel(room.BlockReason);
        return $"Delivery {room.DeliveryProgressBasisPoints / 100}%"
            + $" · Work {room.WorkProgressBasisPoints / 100}%"
            + $" · {materials}{blocker}";
    }

    private static string TemplateLabel(RoomTemplateKind kind) => kind switch
    {
        RoomTemplateKind.Small => "Small room",
        RoomTemplateKind.Medium => "Medium room",
        RoomTemplateKind.Large => "Large room",
        RoomTemplateKind.Tall => "Tall room",
        _ => kind.ToString(),
    };

    private static string StatusLabel(RoomImprovementStatus status) => status switch
    {
        RoomImprovementStatus.Unimproved => "Unimproved",
        RoomImprovementStatus.AwaitingMaterials => "Awaiting materials",
        RoomImprovementStatus.ReadyForWork => "Ready for work",
        RoomImprovementStatus.Improving => "Improving",
        RoomImprovementStatus.Improved => "Improved",
        _ => status.ToString(),
    };

    private static string PurposeLabel(RoomPurposeKind purpose) => purpose switch
    {
        RoomPurposeKind.None => "None",
        RoomPurposeKind.Bedroom => "Bedroom",
        RoomPurposeKind.KitchenDining => "Kitchen-Dining",
        RoomPurposeKind.Workshop => "Workshop",
        RoomPurposeKind.Farm => "Farm",
        _ => purpose.ToString(),
    };

    private static string MaterialLabel(string itemId)
    {
        int separator = itemId.LastIndexOf('.');
        return separator >= 0 ? itemId.Substring(separator + 1) : itemId;
    }

    private static string BlockReasonLabel(
        Dig.Application.Rooms.RoomInfrastructureBlockReason reason) => reason switch
    {
        Dig.Application.Rooms.RoomInfrastructureBlockReason.TemporaryStockCellUnavailable
            => "No reachable free stock cell",
        Dig.Application.Rooms.RoomInfrastructureBlockReason.MaterialsIncomplete
            => "Materials are incomplete",
        Dig.Application.Rooms.RoomInfrastructureBlockReason.WaitingForWorker
            => "Waiting for worker",
        _ => reason.ToString(),
    };
}

}
