using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Society;

namespace Dig.Application.Society
{

public sealed class ResidentDeathCleanupHandler
{
    private static readonly JobBlockReason ResidentDiedReason = new JobBlockReason(
        "resident_died",
        "The assigned resident died before the job completed.");

    private readonly IJobRepository _jobs;
    private readonly IInventoryRepository _inventory;
    private readonly IEventSink _eventSink;

    public ResidentDeathCleanupHandler(
        IJobRepository jobs,
        IInventoryRepository inventory,
        IEventSink eventSink)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public ResidentDeathCleanupReport Handle(ResidentDied residentDied)
    {
        if (residentDied is null)
        {
            throw new ArgumentNullException(nameof(residentDied));
        }

        JobSystem jobs = _jobs.Get();
        InventoryState inventory = _inventory.Get();
        JobSnapshot[] assignedJobs = jobs.GetAll()
            .Where(job => !job.IsTerminal
                && job.AssignedAgentId.HasValue
                && job.AssignedAgentId.Value == residentDied.ResidentId)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();

        List<EntityId> cancelledJobIds = new List<EntityId>(assignedJobs.Length);
        int releasedInventoryQuantity = 0;
        foreach (JobSnapshot job in assignedJobs)
        {
            Result cancelled = jobs.Cancel(job.Id, ResidentDiedReason, residentDied.Tick);
            if (cancelled.IsFailure)
            {
                continue;
            }

            cancelledJobIds.Add(job.Id);
            releasedInventoryQuantity = checked(
                releasedInventoryQuantity
                + inventory.ReleaseReservations(job.Id, residentDied.Tick));
        }

        _jobs.Save(jobs);
        _inventory.Save(inventory);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return new ResidentDeathCleanupReport(
            residentDied.ResidentId,
            cancelledJobIds,
            releasedInventoryQuantity);
    }
}
}
