using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Rooms;

namespace Dig.Application.Rooms
{

public sealed class RoomInfrastructureRuntimeSnapshot
{
    public RoomInfrastructureRuntimeSnapshot(
        RoomInfrastructureSnapshot infrastructure,
        IEnumerable<CompletedRoomInfrastructureProvenance> provenance,
        ulong nextRuntimeSequence)
    {
        Infrastructure = infrastructure
            ?? throw new ArgumentNullException(nameof(infrastructure));
        if (provenance == null)
        {
            throw new ArgumentNullException(nameof(provenance));
        }

        if (nextRuntimeSequence == 0UL)
        {
            throw new ArgumentOutOfRangeException(nameof(nextRuntimeSequence));
        }

        CompletedRoomInfrastructureProvenance[] ordered = provenance
            .OrderBy(value => value.TemplateInstanceId, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Any(value => value == null)
            || ordered.Select(value => value.TemplateInstanceId)
                .Distinct(StringComparer.Ordinal).Count() != ordered.Length
            || ordered.Select(value => value.RoomInfrastructureId)
                .Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException(
                "Room runtime provenance identities must be unique.",
                nameof(provenance));
        }

        Provenance = new ReadOnlyCollection<CompletedRoomInfrastructureProvenance>(
            ordered);
        NextRuntimeSequence = nextRuntimeSequence;
    }

    public RoomInfrastructureSnapshot Infrastructure { get; }

    public IReadOnlyList<CompletedRoomInfrastructureProvenance> Provenance { get; }

    public ulong NextRuntimeSequence { get; }

    public static RoomInfrastructureRuntimeSnapshot Empty()
    {
        return new RoomInfrastructureRuntimeSnapshot(
            new RoomInfrastructureState().CaptureSnapshot(),
            Array.Empty<CompletedRoomInfrastructureProvenance>(),
            nextRuntimeSequence: 1UL);
    }
}

}
