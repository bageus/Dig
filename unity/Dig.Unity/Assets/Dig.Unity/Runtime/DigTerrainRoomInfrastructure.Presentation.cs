using System;
using System.Collections.Generic;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Presentation.Rooms;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly RoomInfrastructurePresenter _roomPresenter =
        new RoomInfrastructurePresenter();
    private OrderRoomUpgradeHandler? _roomOrder;
    private ChangeRoomRequestedPurposeHandler? _roomPurposeChange;

    internal IReadOnlyList<RoomInfrastructureViewModel>
        LoadRoomInfrastructurePresentation()
    {
        EnsureRoomInfrastructurePresentationHandlers();
        return _roomPresenter.Present(
            _roomInfrastructure!.Get().CaptureSnapshot(),
            _roomProvenance.Values);
    }

    internal Result OrderRoomUpgrade(
        string roomInfrastructureId,
        RoomPurposeKind requestedPurpose,
        long tick)
    {
        EnsureRoomInfrastructurePresentationHandlers();
        return _roomOrder!.Handle(new OrderRoomUpgradeCommand(
            ParseRoomId(roomInfrastructureId),
            requestedPurpose,
            tick));
    }

    internal Result ChangeRoomRequestedPurpose(
        string roomInfrastructureId,
        RoomPurposeKind purpose,
        long tick)
    {
        EnsureRoomInfrastructurePresentationHandlers();
        return _roomPurposeChange!.Handle(
            new ChangeRoomRequestedPurposeCommand(
                ParseRoomId(roomInfrastructureId),
                purpose,
                tick));
    }

    internal Result CancelRoomUpgrade(
        string roomInfrastructureId,
        long tick)
    {
        EnsureRoomInfrastructurePresentationHandlers();
        Result<RoomUpgradeCancellationResult> result = _roomCancellation!.Handle(
            new CancelRoomUpgradeOperationCommand(
                ParseRoomId(roomInfrastructureId),
                "player_cancel",
                tick));
        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error!);
    }

    private void EnsureRoomInfrastructurePresentationHandlers()
    {
        EnsureRoomInfrastructureRuntime();
        _roomOrder ??= new OrderRoomUpgradeHandler(
            _roomInfrastructure!, _journal);
        _roomPurposeChange ??= new ChangeRoomRequestedPurposeHandler(
            _roomInfrastructure!, _journal);
    }

    private static EntityId ParseRoomId(string roomInfrastructureId)
    {
        if (string.IsNullOrWhiteSpace(roomInfrastructureId))
        {
            throw new ArgumentException(
                "Room infrastructure id is required.",
                nameof(roomInfrastructureId));
        }

        return EntityId.Parse(roomInfrastructureId.Trim());
    }
}

}
