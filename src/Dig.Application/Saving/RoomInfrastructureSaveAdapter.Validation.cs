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

public static partial class RoomInfrastructureSaveAdapter
{
    private static bool ValidateRuntime(
        RoomInfrastructureSnapshot rooms,
        IReadOnlyList<CompletedRoomInfrastructureProvenance> provenance,
        ulong nextRuntimeSequence,
        InventoryState inventory,
        JobSystem jobs,
        WorldSize worldSize)
    {
        if (nextRuntimeSequence == 0UL
            || !ValidateSequence(nextRuntimeSequence, inventory, jobs)
            || !ValidateProvenance(rooms, provenance, worldSize))
        {
            return false;
        }

        Dictionary<EntityId, RoomInfrastructureProjectSnapshot> owners =
            rooms.Rooms.ToDictionary(value => value.RoomInfrastructureId);
        HashSet<EntityId> activeJobIds = owners.Values
            .SelectMany(value => value.ActiveJobIds)
            .ToHashSet();
        foreach (JobSnapshot job in jobs.GetAll())
        {
            bool runtimeOwned = RoomUpgradeRuntimeIdentity.TryParseSequence(
                job.Id,
                out _);
            if (runtimeOwned && !job.IsTerminal && !activeJobIds.Contains(job.Id))
            {
                return false;
            }
        }

        foreach (RoomInfrastructureProjectSnapshot room in owners.Values)
        {
            if (!ValidateRoomJobs(room, inventory, jobs))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateSequence(
        ulong next,
        InventoryState inventory,
        JobSystem jobs)
    {
        ulong highest = 0UL;
        foreach (EntityId id in jobs.GetAll().Select(value => value.Id)
            .Concat(inventory.CreateSnapshot().Stacks.Select(value => value.StackId)))
        {
            if (RoomUpgradeRuntimeIdentity.TryParseSequence(id, out ulong sequence))
            {
                highest = Math.Max(highest, sequence);
            }
        }

        return next > highest;
    }

    private static bool ValidateProvenance(
        RoomInfrastructureSnapshot rooms,
        IReadOnlyList<CompletedRoomInfrastructureProvenance> provenance,
        WorldSize worldSize)
    {
        if (provenance == null || provenance.Count != rooms.Rooms.Count)
        {
            return false;
        }

        Dictionary<EntityId, RoomInfrastructureProjectSnapshot> byId =
            rooms.Rooms.ToDictionary(value => value.RoomInfrastructureId);
        HashSet<CellId> ownedCells = new HashSet<CellId>();
        foreach (CompletedRoomInfrastructureProvenance source in provenance)
        {
            if (source == null
                || !byId.TryGetValue(source.RoomInfrastructureId, out var room)
                || !string.Equals(
                    source.TemplateInstanceId,
                    room.TemplateInstanceId,
                    StringComparison.Ordinal)
                || source.TemplateKind != room.TemplateKind
                || source.OrderedRoomCells.Count == 0)
            {
                return false;
            }

            foreach (CellId cell in source.OrderedRoomCells)
            {
                if (!worldSize.Contains(cell) || !ownedCells.Add(cell))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ValidateRoomJobs(
        RoomInfrastructureProjectSnapshot room,
        InventoryState inventory,
        JobSystem jobs)
    {
        List<JobSnapshot> active = new List<JobSnapshot>();
        foreach (EntityId jobId in room.ActiveJobIds)
        {
            JobSnapshot? job = jobs.Get(jobId);
            if (job == null || job.IsTerminal)
            {
                return false;
            }

            active.Add(job);
        }

        JobSnapshot[] workJobs = active
            .Where(value => value.Definition is RoomUpgradeWorkJobDefinition)
            .ToArray();
        if (workJobs.Length > 1
            || active.Any(value => value.Definition is not RoomUpgradeWorkJobDefinition
                && value.Definition is not HaulJobDefinition))
        {
            return false;
        }

        if ((room.Status == RoomImprovementStatus.ReadyForWork
                || room.Status == RoomImprovementStatus.Improving)
            && workJobs.Length != 1)
        {
            return false;
        }

        if (!room.TemporaryStockCell.HasValue)
        {
            return active.Count == 0;
        }

        ItemLocation stock = ItemLocation.InWorld(room.TemporaryStockCell.Value);
        if (workJobs.Length == 1)
        {
            RoomUpgradeWorkJobDefinition work =
                (RoomUpgradeWorkJobDefinition)workJobs[0].Definition;
            if (work.RoomInfrastructureId != room.RoomInfrastructureId
                || work.WorkCell != room.TemporaryStockCell.Value)
            {
                return false;
            }

            foreach (RoomMaterialLedgerSnapshot material in room.Materials)
            {
                int expected = material.Delivered - material.Consumed;
                if (inventory.GetReservedQuantityAt(
                        work.Id,
                        material.ItemId,
                        stock) != expected)
                {
                    return false;
                }
            }
        }

        foreach (JobSnapshot delivery in active.Where(
            value => value.Definition is HaulJobDefinition))
        {
            HaulJobDefinition haul = (HaulJobDefinition)delivery.Definition;
            if (haul.Destination != stock
                || room.Materials.All(value => value.ItemId != haul.ItemId)
                || ReservedForJob(inventory, delivery.Id) != haul.Quantity)
            {
                return false;
            }
        }

        return true;
    }

    private static int ReservedForJob(
        InventoryState inventory,
        EntityId jobId)
    {
        return inventory.CreateSnapshot().Stacks
            .SelectMany(value => value.Reservations)
            .Where(value => value.JobId == jobId)
            .Sum(value => value.Quantity);
    }
}

}
