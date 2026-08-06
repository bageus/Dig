using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public sealed class CompletedRoomInfrastructureProvenance
{
    public CompletedRoomInfrastructureProvenance(
        EntityId roomInfrastructureId,
        string templateInstanceId,
        RoomTemplateKind templateKind,
        IEnumerable<CellId> orderedRoomCells)
    {
        if (roomInfrastructureId.IsEmpty)
        {
            throw new ArgumentException("Room infrastructure id cannot be empty.", nameof(roomInfrastructureId));
        }

        if (string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));
        }

        if (!Enum.IsDefined(typeof(RoomTemplateKind), templateKind))
        {
            throw new ArgumentOutOfRangeException(nameof(templateKind));
        }

        CellId[] cells = (orderedRoomCells
            ?? throw new ArgumentNullException(nameof(orderedRoomCells)))
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        if (cells.Length == 0)
        {
            throw new ArgumentException("Completed room provenance requires room cells.", nameof(orderedRoomCells));
        }

        RoomInfrastructureId = roomInfrastructureId;
        TemplateInstanceId = templateInstanceId.Trim();
        TemplateKind = templateKind;
        OrderedRoomCells = new ReadOnlyCollection<CellId>(cells);
    }

    public EntityId RoomInfrastructureId { get; }
    public string TemplateInstanceId { get; }
    public RoomTemplateKind TemplateKind { get; }
    public IReadOnlyList<CellId> OrderedRoomCells { get; }
}

public sealed class RoomInfrastructureProvenanceProjector
{
    public IReadOnlyList<CompletedRoomInfrastructureProvenance> Project(
        IEnumerable<ExcavationTemplateInstance> instances)
    {
        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        CompletedRoomInfrastructureProvenance[] projected = instances
            .Where(value => value != null
                && value.LifecycleState == ExcavationTemplateLifecycleState.Completed)
            .Select(value => new CompletedRoomInfrastructureProvenance(
                CreateRoomInfrastructureId(value.Id),
                value.Id,
                ResolveTemplateKind(value.TemplateId),
                value.OrderedMask))
            .OrderBy(value => value.TemplateInstanceId, StringComparer.Ordinal)
            .ToArray();
        if (projected.Select(value => value.TemplateInstanceId)
                .Distinct(StringComparer.Ordinal).Count() != projected.Length
            || projected.Select(value => value.RoomInfrastructureId)
                .Distinct().Count() != projected.Length)
        {
            throw new ArgumentException("Completed room provenance identities must be unique.", nameof(instances));
        }

        return new ReadOnlyCollection<CompletedRoomInfrastructureProvenance>(projected);
    }

    public static EntityId CreateRoomInfrastructureId(string templateInstanceId)
    {
        if (string.IsNullOrWhiteSpace(templateInstanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));
        }

        string value = "room-infrastructure:" + templateInstanceId.Trim();
        ulong first = Hash(value, 14695981039346656037UL);
        ulong second = Hash(value, 7809847782465536322UL);
        if (first == 0UL && second == 0UL)
        {
            second = 1UL;
        }

        return EntityId.Parse(first.ToString("x16") + second.ToString("x16"));
    }

    private static RoomTemplateKind ResolveTemplateKind(string templateId)
    {
        if (templateId == CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Small).Id)
        {
            return RoomTemplateKind.Small;
        }

        if (templateId == CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Medium).Id)
        {
            return RoomTemplateKind.Medium;
        }

        if (templateId == CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Large).Id)
        {
            return RoomTemplateKind.Large;
        }

        if (templateId == CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Tall).Id)
        {
            return RoomTemplateKind.Tall;
        }

        throw new ArgumentException("Unsupported completed room template id.", nameof(templateId));
    }

    private static ulong Hash(string value, ulong seed)
    {
        const ulong prime = 1099511628211UL;
        ulong hash = seed;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            hash ^= (byte)character;
            hash *= prime;
            hash ^= (byte)(character >> 8);
            hash *= prime;
        }

        return hash;
    }
}

}
