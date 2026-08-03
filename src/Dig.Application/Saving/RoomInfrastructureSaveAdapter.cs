using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class RoomInfrastructureSaveErrors
{
    public static readonly DomainError InvalidSnapshot = new DomainError(
        "save.room_infrastructure.invalid",
        "Room infrastructure save data is malformed or inconsistent.");
}

public static partial class RoomInfrastructureSaveAdapter
{
    public static RoomInfrastructureSaveData Encode(
        RoomInfrastructureRuntimeSnapshot runtime,
        InventoryState inventory,
        JobSystem jobs,
        WorldSize worldSize)
    {
        if (runtime == null || inventory == null || jobs == null)
        {
            throw new ArgumentNullException(
                runtime == null
                    ? nameof(runtime)
                    : inventory == null
                        ? nameof(inventory)
                        : nameof(jobs));
        }

        Result<RoomInfrastructureState> restored =
            RoomInfrastructureState.Restore(runtime.Infrastructure);
        if (restored.IsFailure
            || !ValidateRuntime(
                restored.Value.CaptureSnapshot(),
                runtime.Provenance,
                runtime.NextRuntimeSequence,
                inventory,
                jobs,
                worldSize))
        {
            throw new InvalidOperationException(
                RoomInfrastructureSaveErrors.InvalidSnapshot.ToString());
        }

        RoomInfrastructureSnapshot snapshot = restored.Value.CaptureSnapshot();
        RoomInfrastructureSaveData data = new RoomInfrastructureSaveData
        {
            Version = snapshot.Version,
            NextRuntimeSequence = runtime.NextRuntimeSequence,
        };
        foreach (RoomInfrastructureProjectSnapshot room in snapshot.Rooms)
        {
            data.Rooms.Add(EncodeProject(room));
        }

        foreach (CompletedRoomInfrastructureProvenance provenance in
            runtime.Provenance)
        {
            data.Provenance.Add(EncodeProvenance(provenance));
        }

        return data;
    }

    public static Result<RoomInfrastructureRuntimeSnapshot> Decode(
        RoomInfrastructureSaveData? data,
        InventoryState inventory,
        JobSystem jobs,
        WorldSize worldSize)
    {
        if (inventory == null || jobs == null)
        {
            throw new ArgumentNullException(
                inventory == null ? nameof(inventory) : nameof(jobs));
        }

        data ??= new RoomInfrastructureSaveData();
        try
        {
            if (data.Rooms == null
                || data.Provenance == null
                || data.Rooms.Any(value => value == null)
                || data.Provenance.Any(value => value == null))
            {
                return Failure();
            }

            RoomInfrastructureSnapshot saved = new RoomInfrastructureSnapshot(
                data.Version,
                data.Rooms
                    .OrderBy(
                        value => value.RoomInfrastructureId,
                        StringComparer.Ordinal)
                    .Select(DecodeProject));
            Result<RoomInfrastructureState> restored =
                RoomInfrastructureState.Restore(saved);
            if (restored.IsFailure)
            {
                return Failure();
            }

            CompletedRoomInfrastructureProvenance[] provenance = data.Provenance
                .OrderBy(value => value.TemplateInstanceId, StringComparer.Ordinal)
                .Select(DecodeProvenance)
                .ToArray();
            RoomInfrastructureSnapshot derived = restored.Value.CaptureSnapshot();
            if (!ValidateRuntime(
                derived,
                provenance,
                data.NextRuntimeSequence,
                inventory,
                jobs,
                worldSize))
            {
                return Failure();
            }

            return Result<RoomInfrastructureRuntimeSnapshot>.Success(
                new RoomInfrastructureRuntimeSnapshot(
                    derived,
                    provenance,
                    data.NextRuntimeSequence));
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Failure();
        }
    }

    public static ulong ResolveLegacyNextRuntimeSequence(
        JobsSaveData? jobs,
        InventorySaveData? inventory)
    {
        ulong next = 1UL;
        IEnumerable<string?> ids = (jobs?.Jobs
                ?? new List<JobSaveData>())
            .Select(value => value?.Definition?.JobId)
            .Concat((inventory?.Stacks
                ?? new List<ItemStackSaveData>())
                .Select(value => value?.StackId));
        foreach (string? id in ids)
        {
            if (RoomUpgradeRuntimeIdentity.TryParseSequence(
                    id,
                    out ulong sequence))
            {
                next = Math.Max(next, checked(sequence + 1UL));
            }
        }

        return next;
    }

    private static Result<RoomInfrastructureRuntimeSnapshot> Failure()
    {
        return Result<RoomInfrastructureRuntimeSnapshot>.Failure(
            RoomInfrastructureSaveErrors.InvalidSnapshot);
    }
}

}
