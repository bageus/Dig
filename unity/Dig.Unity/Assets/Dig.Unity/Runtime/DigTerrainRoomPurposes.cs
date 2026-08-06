using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dig.Application.Rooms;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Rooms;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private readonly RoomPurposePresenter _roomPurposePresenter =
        new RoomPurposePresenter();
    private InMemoryRoomPurposeRepository? _roomPurposeRepository;
    private RegisterCompletedRoomHandler? _registerCompletedRoom;
    private ChangeRoomPurposeHandler? _changeRoomPurpose;

    internal Result SynchronizeRoomPurposes(
        IReadOnlyList<CaveRoomPlan> completedPlans,
        long tick)
    {
        if (completedPlans == null)
        {
            throw new ArgumentNullException(nameof(completedPlans));
        }

        EnsureRoomPurposeRuntime();
        for (int index = 0; index < completedPlans.Count; index++)
        {
            CaveRoomPlan plan = completedPlans[index];
            Result registered = _registerCompletedRoom!.Handle(
                new RegisterCompletedRoomCommand(
                    ResolveRoomId(plan),
                    plan.Preset.Kind.ToString(),
                    plan.VolumeCells,
                    tick));
            if (registered.IsFailure)
            {
                return registered;
            }
        }

        return Result.Success();
    }

    internal IReadOnlyList<RoomPurposeViewModel> LoadRoomPurposes()
    {
        EnsureRoomPurposeRuntime();
        return _roomPurposePresenter.Present(
            _roomPurposeRepository!.Get().CaptureSnapshot());
    }

    internal RoomPurposeViewModel? LoadRoomPurpose(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        return LoadRoomPurposes().FirstOrDefault(value =>
            string.Equals(value.RoomId, roomId, StringComparison.Ordinal));
    }

    internal Result ChangeRoomPurpose(
        string roomId,
        RoomPurposeKind purpose,
        long tick)
    {
        EnsureRoomPurposeRuntime();
        return _changeRoomPurpose!.Handle(
            new ChangeRoomPurposeCommand(EntityId.Parse(roomId), purpose, tick));
    }

    internal static EntityId ResolveRoomId(CaveRoomPlan plan)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        string identity = $"room:{plan.Preset.Kind}:{plan.Entrance.X}:" +
            $"{plan.Entrance.Y}:{plan.Entrance.Z}";
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(identity));
        byte[] bytes = new byte[16];
        Array.Copy(hash, bytes, bytes.Length);
        return new EntityId(new Guid(bytes));
    }

    private void EnsureRoomPurposeRuntime()
    {
        if (_roomPurposeRepository != null)
        {
            return;
        }

        _roomPurposeRepository = new InMemoryRoomPurposeRepository();
        _registerCompletedRoom = new RegisterCompletedRoomHandler(
            _roomPurposeRepository,
            _journal);
        _changeRoomPurpose = new ChangeRoomPurposeHandler(
            _roomPurposeRepository,
            _journal);
    }
}
}
