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

public sealed class StageProductionMaterialCommand : ICommand<Result>
{
    public StageProductionMaterialCommand(
        EntityId orderId,
        EntityId jobId,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        Tick = tick;
    }

    public EntityId OrderId { get; }
    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class DepositProductionMaterialCommand : ICommand<Result>
{
    public DepositProductionMaterialCommand(
        EntityId orderId,
        EntityId jobId,
        EntityId packageStackId,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        PackageStackId = packageStackId;
        Tick = tick;
    }

    public EntityId OrderId { get; }
    public EntityId JobId { get; }
    public EntityId PackageStackId { get; }
    public long Tick { get; }
}

public sealed class StageProductionMaterialHandler
    : ICommandHandler<StageProductionMaterialCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public StageProductionMaterialHandler(
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

    public Result Handle(StageProductionMaterialCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        JobSnapshot? job = _jobRepository.Get().Get(command.JobId);
        ProductionOrderSnapshot? order = production.Get(command.OrderId);
        if (!MatchesActiveWork(job, order, command.OrderId)
            || job?.AssignedAgentId.HasValue != true)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot? step = GetCurrentStep(order!);
        if (!step.HasValue
            || step.Value.Phase != ProductionMaterialStepPhase.AwaitingMaterial)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        EntityId workerId = job!.AssignedAgentId.GetValueOrDefault();
        InventoryState inventory = _inventoryRepository.Get();
        bool hasCarriedReservation = inventory.CreateSnapshot().Stacks.Any(stack =>
            stack.ItemId == step.Value.ItemId
            && stack.Location.Kind == ItemLocationKind.AgentInventory
            && stack.Location.HasOwner
            && stack.Location.OwnerId == workerId
            && stack.Reservations.Any(value =>
                value.JobId == command.OrderId && value.Quantity > 0));
        if (!hasCarriedReservation)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        Result consumed = inventory.ConsumeReservedProductionUnit(
            command.OrderId,
            workerId,
            step.Value.ItemId,
            command.Tick);
        if (consumed.IsFailure)
        {
            return consumed;
        }

        Result staged = production.StageMaterial(command.OrderId, command.Tick);
        if (staged.IsFailure)
        {
            throw new InvalidOperationException(
                "Prevalidated production material could not be staged.");
        }

        _inventoryRepository.Save(inventory);
        _productionRepository.Save(production);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(production.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static bool MatchesActiveWork(
        JobSnapshot? job,
        ProductionOrderSnapshot? order,
        EntityId orderId)
    {
        return job?.Definition is ProductionWorkJobDefinition definition
            && definition.OrderId == orderId
            && job.Status == JobStatus.InProgress
            && job.Stage == JobStageKind.PerformWork
            && order?.Status == ProductionOrderStatus.InProgress;
    }

    private static ProductionMaterialStepSnapshot? GetCurrentStep(
        ProductionOrderSnapshot order)
    {
        return order.MaterialSteps
            .Where(value => !value.Consumed)
            .Select(value => (ProductionMaterialStepSnapshot?)value)
            .FirstOrDefault();
    }
}

public sealed class DepositProductionMaterialHandler
    : ICommandHandler<DepositProductionMaterialCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public DepositProductionMaterialHandler(
        IProductionRepository productionRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(DepositProductionMaterialCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        ProductionOrderSnapshot? order = production.Get(command.OrderId);
        ProductionOutputPackageSnapshot? package = production.GetOutputPackage(
            command.PackageStackId);
        if (job?.Definition is not ProductionWorkJobDefinition definition
            || definition.OrderId != command.OrderId
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork
            || order?.Status != ProductionOrderStatus.InProgress
            || package?.OrderId != command.OrderId
            || package.Kind != ProductionOutputPackageKind.Unfinished)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot? step = order.MaterialSteps
            .Where(value => !value.Consumed)
            .Select(value => (ProductionMaterialStepSnapshot?)value)
            .FirstOrDefault();
        if (!step.HasValue
            || step.Value.Phase
                != ProductionMaterialStepPhase.ProcessedAwaitingPackage)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        Result deposited = production.DepositProcessedMaterial(
            command.OrderId,
            command.Tick);
        if (deposited.IsFailure)
        {
            return deposited;
        }

        if (production.Get(command.OrderId)!.Status
            == ProductionOrderStatus.ReadyToComplete)
        {
            Result advanced = jobs.AdvanceStage(command.JobId, command.Tick);
            if (advanced.IsFailure)
            {
                throw new InvalidOperationException(
                    "Deposited final material could not advance production job.");
            }
        }

        _productionRepository.Save(production);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
