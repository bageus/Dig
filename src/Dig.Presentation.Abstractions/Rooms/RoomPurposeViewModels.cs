using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Presentation.Rooms
{

public sealed class RoomPurposeViewModel
{
    public RoomPurposeViewModel(
        string roomId,
        string templateId,
        float centerX,
        int topY,
        int frontZ,
        RoomPurposeKind requestedPurpose,
        RoomPurposeKind activePurpose,
        RoomImprovementStatus improvementStatus,
        long version)
    {
        RoomId = roomId;
        TemplateId = templateId;
        CenterX = centerX;
        TopY = topY;
        FrontZ = frontZ;
        RequestedPurpose = requestedPurpose;
        ActivePurpose = activePurpose;
        ImprovementStatus = improvementStatus;
        Version = version;
    }

    public string RoomId { get; }
    public string TemplateId { get; }
    public float CenterX { get; }
    public int TopY { get; }
    public int FrontZ { get; }
    public RoomPurposeKind RequestedPurpose { get; }
    public RoomPurposeKind ActivePurpose { get; }
    public RoomImprovementStatus ImprovementStatus { get; }
    public long Version { get; }
}

public sealed class RoomPurposePresenter
{
    public IReadOnlyList<RoomPurposeViewModel> Present(
        IReadOnlyCollection<RoomPurposeSnapshot> rooms)
    {
        if (rooms is null)
        {
            throw new ArgumentNullException(nameof(rooms));
        }

        return new ReadOnlyCollection<RoomPurposeViewModel>(rooms
            .OrderBy(room => room.RoomId.ToString(), StringComparer.Ordinal)
            .Select(Present)
            .ToArray());
    }

    private static RoomPurposeViewModel Present(RoomPurposeSnapshot room)
    {
        int minX = room.VolumeCells.Min(cell => cell.X);
        int maxX = room.VolumeCells.Max(cell => cell.X);
        int topY = room.VolumeCells.Min(cell => cell.Y);
        int frontZ = room.VolumeCells.Min(cell => cell.Z);
        return new RoomPurposeViewModel(
            room.RoomId.ToString(),
            room.TemplateId,
            (minX + maxX) * 0.5f,
            topY,
            frontZ,
            room.RequestedPurpose,
            room.ActivePurpose,
            room.ImprovementStatus,
            room.Version);
    }
}

}
