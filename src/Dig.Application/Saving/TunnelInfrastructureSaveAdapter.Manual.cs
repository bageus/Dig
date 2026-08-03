using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public static partial class TunnelInfrastructureSaveAdapter
{
    public static ulong ResolveLegacyNextManualSequence(JobsSaveData? jobs)
    {
        ulong next = 1UL;
        IEnumerable<JobSaveData> savedJobs = jobs == null
            ? Array.Empty<JobSaveData>()
            : jobs.Jobs;
        foreach (JobSaveData job in savedJobs)
        {
            JobDefinitionSaveData? definition = job?.Definition;
            if (definition == null
                || !string.Equals(
                    definition.TypeId,
                    new TunnelManualWorkJobSaveCodec().TypeId,
                    StringComparison.Ordinal)
                || !TryParseManualJobSequence(definition.JobId, out ulong sequence))
            {
                continue;
            }

            next = Math.Max(next, checked(sequence + 1UL));
        }

        return next;
    }

    private static bool IsNextManualSequenceValid(ulong next, JobSystem jobs)
    {
        if (next == 0)
        {
            return false;
        }

        return !jobs.GetAll().Any(job =>
            job.Definition is TunnelManualWorkJobDefinition
            && TryParseManualJobSequence(job.Id.ToString(), out ulong sequence)
            && sequence >= next);
    }

    private static bool TryParseManualJobSequence(
        string? value,
        out ulong sequence)
    {
        sequence = 0;
        return value != null
            && value.Length == 32
            && value[0] == 'b'
            && ulong.TryParse(
                value.Substring(1),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out sequence)
            && sequence > 0;
    }
    private static bool ValidateManualJobs(
        Dig.Domain.World.TunnelInfrastructureSnapshot snapshot,
        JobSystem jobs,
        Dig.Domain.Inventory.InventoryState? inventory)
    {
        foreach (JobSnapshot job in jobs.GetAll().Where(candidate =>
            candidate.Definition is TunnelManualWorkJobDefinition))
        {
            var definition = (TunnelManualWorkJobDefinition)job.Definition;
            if (!job.IsTerminal
                && job.AssignedAgentId != definition.OwnerResidentId)
            {
                return false;
            }

            string itemId = definition.RequiredItemId.ToString();
            Result<Dig.Application.Tunnels.TunnelManualPlacementPlan> target =
                Dig.Application.Tunnels.TunnelManualTargetResolver.Resolve(
                    snapshot,
                    definition.OwnerResidentId,
                    definition.SourceStackId,
                    itemId,
                    definition.TargetCell);
            if (!job.IsTerminal
                && (target.IsFailure
                    || target.Value.SegmentId != definition.SegmentId
                    || target.Value.Kind != definition.Kind))
            {
                return false;
            }

            if (inventory == null || job.IsTerminal)
            {
                continue;
            }

            Dig.Domain.Inventory.ItemStackSnapshot? source =
                inventory.GetStack(definition.SourceStackId);
            if (source == null
                || source.ItemId != definition.RequiredItemId
                || source.Location.Kind !=
                    Dig.Domain.Inventory.ItemLocationKind.AgentInventory
                || !source.Location.HasOwner
                || source.Location.OwnerId != definition.OwnerResidentId
                || inventory.GetReservedQuantity(
                    definition.SourceStackId,
                    job.Id) < 1)
            {
                return false;
            }
        }

        return true;
    }

}

}
