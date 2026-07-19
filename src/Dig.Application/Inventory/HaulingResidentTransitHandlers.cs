using System;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public sealed class AcquireHaulingItemCommand : ICommand<Result>
{
    public AcquireHaulingItemCommand(
        EntityId jobId,
        EntityId destinationStackId,
        long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        JobId = jobId;
        DestinationStackId = destinationStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId DestinationStackId { get; }
    public long Tick { get; }
}

public sealed class AcquireHaulingItemHandler
    : ICommandHandler<AcquireHaulingItemCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AcquireHaulingItemHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(AcquireHaulingItemCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Definition is not HaulJobDefinition hauling)
        {
            return Result.Failure(HaulingErrors.JobNotHauling);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.AcquireItem
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(HaulingErrors.InvalidStage);
        }

        Result acquired = inventory.AcquireReservedIntoResidentSlots(
            hauling.SourceStackId,
            job.Id,
            job.AssignedAgentId.Value,
            command.DestinationStackId,
            command.Tick);
        if (acquired.IsFailure)
        {
            return acquired;
        }

        Result advanced = jobs.AdvanceStage(job.Id, command.Tick);
        if (advanced.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated hauling acquisition failed its stage transition.");
        }

        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}