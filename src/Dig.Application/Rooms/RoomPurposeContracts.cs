using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public interface IRoomPurposeRepository
{
    RoomPurposeState Get();
    void Save(RoomPurposeState state);
}

public sealed class RegisterCompletedRoomCommand : ICommand<Result>
{
    public RegisterCompletedRoomCommand(
        EntityId roomId,
        string templateId,
        IEnumerable<CellId> volumeCells,
        long tick)
    {
        if (volumeCells is null)
        {
            throw new ArgumentNullException(nameof(volumeCells));
        }

        RoomId = roomId;
        TemplateId = templateId;
        VolumeCells = new ReadOnlyCollection<CellId>(
            volumeCells.Distinct().OrderBy(cell => cell).ToArray());
        Tick = tick;
    }

    public EntityId RoomId { get; }
    public string TemplateId { get; }
    public IReadOnlyList<CellId> VolumeCells { get; }
    public long Tick { get; }
}

public sealed class ChangeRoomPurposeCommand : ICommand<Result>
{
    public ChangeRoomPurposeCommand(
        EntityId roomId,
        RoomPurposeKind purpose,
        long tick)
    {
        RoomId = roomId;
        Purpose = purpose;
        Tick = tick;
    }

    public EntityId RoomId { get; }
    public RoomPurposeKind Purpose { get; }
    public long Tick { get; }
}

public sealed class GetRoomPurposesQuery
    : IQuery<IReadOnlyList<RoomPurposeSnapshot>>
{
}

}
