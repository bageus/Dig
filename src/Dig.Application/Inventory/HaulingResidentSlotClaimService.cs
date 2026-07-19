using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public static class HaulingResidentSlotClaimErrors
{
    public static readonly DomainError SourceStackMissing = new DomainError(
        "hauling.slot_claim.source_missing",
        "The hauling source stack does not exist.");
    public static readonly DomainError WorkerRequired = new DomainError(
        "hauling.slot_claim.worker_required",
        "A hauling worker is required before resident slot capacity can be reserved.");
}

public interface IHaulingResidentSlotClaimService
{
    Result Reserve(JobSnapshot job, EntityId residentId, long tick);

    int Release(EntityId jobId, long tick);

    int Reconcile(JobSystem jobs, long tick);
}

public sealed class HaulingResidentSlotClaimService
    : IHaulingResidentSlotClaimService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEventSink _eventSink;

    public HaulingResidentSlotClaimService(
        IInventoryRepository inventoryRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Reserve(JobSnapshot job, EntityId residentId, long tick)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (residentId.IsEmpty)
        {
            return Result.Failure(HaulingResidentSlotClaimErrors.WorkerRequired);
        }

        if (job.Definition is not HaulJobDefinition haul)
        {
            return Result.Success();
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? source = inventory.GetStack(haul.SourceStackId);
        if (source is null)
        {
            return Result.Failure(HaulingResidentSlotClaimErrors.SourceStackMissing);
        }

        var reserved = inventory.ReserveResidentSlotCapacity(
            job.Id,
            residentId,
            source.ItemId,
            haul.Quantity,
            tick);
        if (reserved.IsFailure)
        {
            return Result.Failure(reserved.Error!);
        }

        Save(inventory);
        return Result.Success();
    }

    public int Release(EntityId jobId, long tick)
    {
        InventoryState inventory = _inventoryRepository.Get();
        int released = inventory.ReleaseResidentSlotClaims(jobId, tick);
        if (released > 0)
        {
            Save(inventory);
        }

        return released;
    }

    public int Reconcile(JobSystem jobs, long tick)
    {
        InventoryState inventory = _inventoryRepository.Get();
        int released = HaulingResidentSlotClaimReconciler.ReleaseStale(
            inventory,
            jobs,
            tick);
        if (released > 0)
        {
            Save(inventory);
        }

        return released;
    }

    private void Save(InventoryState inventory)
    {
        _inventoryRepository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
    }
}

}