using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public sealed class ResidentInventoryPlacementQueue
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public ResidentInventoryPlacementQueue(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Synchronize(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        bool changed = false;
        foreach (JobSnapshot snapshot in jobs.GetAll()
            .Where(value => value.Definition is ResidentInventoryPlacementJobDefinition)
            .OrderBy(value => value.Definition.CreatedTick)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            var definition = (ResidentInventoryPlacementJobDefinition)snapshot.Definition;
            if (snapshot.Status == JobStatus.Cancelled || snapshot.Status == JobStatus.Failed)
            {
                changed = inventory.ReleaseReservations(snapshot.Id, tick) > 0 || changed;
                continue;
            }

            if (snapshot.Status == JobStatus.Blocked && tick >= snapshot.NextRetryTick)
            {
                Result retried = jobs.Retry(snapshot.Id, tick);
                if (retried.IsFailure)
                {
                    if (retried.Error != JobErrors.DependenciesIncomplete)
                    {
                        return retried;
                    }

                    continue;
                }

                changed = true;
                Result claimed = ClaimExactResident(jobs, snapshot.Id, definition.ResidentId, tick);
                if (claimed.IsFailure)
                {
                    return claimed;
                }

                continue;
            }

            if (snapshot.Status == JobStatus.Created)
            {
                JobSnapshot[] dependencies = definition.Dependencies
                    .Select(jobs.Get)
                    .Where(value => value != null)
                    .Cast<JobSnapshot>()
                    .ToArray();
                bool missingDependency = dependencies.Length != definition.Dependencies.Count;
                bool failedDependency = dependencies.Any(value =>
                    value.Status == JobStatus.Cancelled || value.Status == JobStatus.Failed);
                if (missingDependency || failedDependency)
                {
                    Result cancelled = jobs.Cancel(
                        snapshot.Id,
                        new JobBlockReason(
                            ResidentInventoryPlacementErrors.DependencyFailed.Code,
                            ResidentInventoryPlacementErrors.DependencyFailed.Message),
                        tick);
                    if (cancelled.IsFailure)
                    {
                        return cancelled;
                    }

                    inventory.ReleaseReservations(snapshot.Id, tick);
                    changed = true;
                    continue;
                }

                if (dependencies.Any(value => value.Status != JobStatus.Completed))
                {
                    continue;
                }

                Result activated = CreateResidentInventoryPlacementHandler.Activate(
                    jobs,
                    snapshot.Id,
                    definition.ResidentId,
                    tick);
                if (activated.IsFailure)
                {
                    if (activated.Error == JobErrors.AgentUnavailable
                        || activated.Error == JobErrors.ReservationConflict)
                    {
                        changed = true;
                        continue;
                    }

                    return activated;
                }

                changed = true;
                continue;
            }

            if (snapshot.Status == JobStatus.Available)
            {
                Result claimed = ClaimExactResident(
                    jobs,
                    snapshot.Id,
                    definition.ResidentId,
                    tick);
                if (claimed.IsFailure)
                {
                    return claimed;
                }

                changed = true;
            }
        }

        if (!changed)
        {
            return Result.Success();
        }

        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static Result ClaimExactResident(
        JobSystem jobs,
        EntityId jobId,
        EntityId residentId,
        long tick)
    {
        Result claimed = jobs.Claim(jobId, residentId, tick);
        return claimed.IsSuccess
            || claimed.Error == JobErrors.AgentUnavailable
            || claimed.Error == JobErrors.ReservationConflict
                ? Result.Success()
                : claimed;
    }
}
}
