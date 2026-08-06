using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Presentation.Rooms
{

public sealed class RoomInfrastructurePresenter
{
    private readonly RoomInfrastructureDiagnosticsProjector _diagnostics =
        new RoomInfrastructureDiagnosticsProjector();

    public IReadOnlyList<RoomInfrastructureViewModel> Present(
        RoomInfrastructureSnapshot snapshot,
        IEnumerable<CompletedRoomInfrastructureProvenance> provenance)
    {
        if (snapshot == null || provenance == null)
        {
            throw new ArgumentNullException(
                snapshot == null ? nameof(snapshot) : nameof(provenance));
        }

        Dictionary<EntityId, CompletedRoomInfrastructureProvenance> sources =
            provenance.ToDictionary(value => value.RoomInfrastructureId);
        Dictionary<EntityId, RoomInfrastructureDiagnostic> diagnostics =
            _diagnostics.Project(snapshot).ToDictionary(
                value => value.RoomInfrastructureId);
        List<RoomInfrastructureViewModel> rooms = new List<RoomInfrastructureViewModel>();
        for (int index = 0; index < snapshot.Rooms.Count; index++)
        {
            RoomInfrastructureProjectSnapshot room = snapshot.Rooms[index];
            if (!sources.TryGetValue(
                    room.RoomInfrastructureId,
                    out CompletedRoomInfrastructureProvenance? source)
                || !diagnostics.TryGetValue(
                    room.RoomInfrastructureId,
                    out RoomInfrastructureDiagnostic? diagnostic))
            {
                throw new InvalidOperationException(
                    "Room presentation requires matching completed provenance and diagnostics.");
            }

            rooms.Add(PresentRoom(room, source, diagnostic));
        }

        return new ReadOnlyCollection<RoomInfrastructureViewModel>(
            rooms.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
    }

    private static RoomInfrastructureViewModel PresentRoom(
        RoomInfrastructureProjectSnapshot room,
        CompletedRoomInfrastructureProvenance source,
        RoomInfrastructureDiagnostic diagnostic)
    {
        if (!string.Equals(
                room.TemplateInstanceId,
                source.TemplateInstanceId,
                StringComparison.Ordinal)
            || room.TemplateKind != source.TemplateKind)
        {
            throw new InvalidOperationException(
                "Room presentation provenance identity drift was detected.");
        }

        CellId[] cells = source.OrderedRoomCells.ToArray();
        int minX = cells.Min(value => value.X);
        int maxX = cells.Max(value => value.X);
        int minY = cells.Min(value => value.Y);
        int maxY = cells.Max(value => value.Y);
        int markerZ = cells.Min(value => value.Z);
        float markerX = (minX + maxX) * 0.5f;
        return new RoomInfrastructureViewModel(
            room.RoomInfrastructureId.ToString(),
            room.TemplateInstanceId,
            room.TemplateKind,
            room.UpgradeOrderCount,
            room.Status,
            room.RequestedPurpose,
            room.ActivePurpose,
            diagnostic.CancellationAllowed,
            diagnostic.BlockReason,
            markerX,
            minY,
            markerZ,
            minX,
            maxX,
            minY,
            maxY,
            room.Materials.Select(value => new RoomMaterialProgressViewModel(
                value.ItemId.ToString(),
                value.Required,
                value.Delivered,
                value.Consumed)),
            room.CompletedMaterialUnits.Select(value =>
                new RoomMaterialUnitProgressViewModel(
                    value.ItemId.ToString(),
                    value.Ordinal)),
            room.Version);
    }
}

}
