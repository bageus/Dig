using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;

namespace Dig.Application.Production
{

public sealed class BeginProductionWorkHandler
    : ICommandHandler<BeginProductionWorkCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public BeginProductionWorkHandler(
        IProductionRepository productionRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(BeginProductionWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (!Matches(job, command.OrderId) || job!.Status != JobStatus.Claimed)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        Result jobStarted = jobs.Start(command.JobId, command.Tick);
        if (jobStarted.IsFailure)
        {
            return jobStarted;
        }

        Result orderStarted = production.Start(command.OrderId, command.Tick);
        if (orderStarted.IsFailure)
        {
            jobs.Block(
                command.JobId,
                new JobBlockReason("production_order_not_ready", orderStarted.Error!.Message),
                command.Tick);
            return orderStarted;
        }

        SaveAndPublish(production, jobs);
        return Result.Success();
    }

    private static bool Matches(JobSnapshot? job, EntityId orderId)
    {
        return job?.Definition is ProductionWorkJobDefinition work
            && work.OrderId == orderId;
    }

    private void SaveAndPublish(ProductionState production, JobSystem jobs)
    {
        _productionRepository.Save(production);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class ApplyProductionWorkHandler
    : ICommandHandler<ApplyProductionWorkCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public ApplyProductionWorkHandler(
        IProductionRepository productionRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(ApplyProductionWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not ProductionWorkJobDefinition work
            || work.OrderId != command.OrderId
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        int effectiveWork = ProductionEfficiency.CalculateEffectiveWork(
            command.BaseWork,
            command.Context);
        Result applied = production.AddWork(command.OrderId, effectiveWork, command.Tick);
        if (applied.IsFailure)
        {
            return applied;
        }

        if (production.Get(command.OrderId)!.Status
            == ProductionOrderStatus.ReadyToComplete)
        {
            Result advanced = jobs.AdvanceStage(command.JobId, command.Tick);
            if (advanced.IsFailure)
            {
                throw new InvalidOperationException(
                    "Production reached completion but its work job could not finalize.");
            }
        }

        _productionRepository.Save(production);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class CompleteProductionOrderHandler
    : ICommandHandler<CompleteProductionOrderCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteProductionOrderHandler(
        IProductionRepository productionRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CompleteProductionOrderCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        ProductionOrderSnapshot? order = production.Get(command.OrderId);
        JobSnapshot? job = jobs.Get(command.JobId);
        if (order is null
            || job?.Definition is not ProductionWorkJobDefinition work
            || work.OrderId != command.OrderId
            || order.Status != ProductionOrderStatus.ReadyToComplete
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        EntityId[] outputIds = command.OutputStackIds
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (outputIds.Length != order.Recipe.Outputs.Count
            || outputIds.Any(value => value.IsEmpty)
            || outputIds.Distinct().Count() != outputIds.Length)
        {
            return Result.Failure(ProductionErrors.OutputIdsMismatch);
        }

        ItemStackCreation[] outputs = order.Recipe.Outputs
            .OrderBy(value => value.ItemId)
            .Zip(
                outputIds,
                (definition, stackId) => new ItemStackCreation(
                    stackId,
                    definition.ItemId,
                    definition.Quantity))
            .ToArray();
        Result committed = inventory.CompleteProductionTransaction(
            order.Id,
            order.InputAllocations,
            outputs,
            ItemLocation.InBuilding(order.BuildingId),
            command.Tick);
        if (committed.IsFailure)
        {
            return committed;
        }

        Result completed = production.Complete(order.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated production order could not complete after inventory commit.");
        }

        Result jobCompleted = jobs.AdvanceStage(command.JobId, command.Tick);
        if (jobCompleted.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated production job could not complete its final stage.");
        }

        _productionRepository.Save(production);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
