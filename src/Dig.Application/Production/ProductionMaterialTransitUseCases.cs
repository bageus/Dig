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

public sealed class AcquireProductionMaterialCommand : ICommand<Result>
{
    public AcquireProductionMaterialCommand(
        EntityId orderId,
        EntityId jobId,
        EntityId destinationStackId,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        DestinationStackId = destinationStackId;
        Tick = tick;
    }

    public EntityId OrderId { get; }
    public EntityId JobId { get; }
    public EntityId DestinationStackId { get; }
    public long Tick { get; }
}

public sealed class AcquireProductionMaterialHandler
    : ICommandHandler<AcquireProductionMaterialCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AcquireProductionMaterialHandler(
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

    public Result Handle(AcquireProductionMaterialCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionOrderSnapshot? order = _productionRepository.Get().Get(command.OrderId);
        JobSnapshot? job = _jobRepository.Get().Get(command.JobId);
        if (order == null
            || job?.Definition is not ProductionWorkJobDefinition definition
            || definition.OrderId != command.OrderId
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork
            || !job.AssignedAgentId.HasValue
            || order.Status != ProductionOrderStatus.InProgress)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot? step = order.MaterialSteps
            .Where(value => !value.Consumed)
            .Select(value => (ProductionMaterialStepSnapshot?)value)
            .FirstOrDefault();
        if (!step.HasValue
            || step.Value.Phase != ProductionMaterialStepPhase.AwaitingMaterial)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        EntityId residentId = job.AssignedAgentId.Value;
        InventoryState inventory = _inventoryRepository.Get();
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        bool alreadyCarried = snapshot.Stacks.Any(stack =>
            stack.ItemId == step.Value.ItemId
            && stack.Location.Kind == ItemLocationKind.AgentInventory
            && stack.Location.HasOwner
            && stack.Location.OwnerId == residentId
            && stack.Reservations.Any(value =>
                value.JobId == command.OrderId && value.Quantity > 0));
        if (alreadyCarried)
        {
            return Result.Success();
        }

        ItemStackSnapshot? source = snapshot.Stacks
            .Where(stack => stack.ItemId == step.Value.ItemId
                && stack.Location == ItemLocation.InBuilding(definition.BuildingId)
                && stack.Reservations.Any(value =>
                    value.JobId == command.OrderId && value.Quantity > 0))
            .OrderBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (source == null)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        Result<EntityId> acquired = ResidentItemTransferService.AcquireReservedProductionUnit(
            inventory,
            source.StackId,
            command.OrderId,
            residentId,
            command.DestinationStackId,
            command.Tick);
        if (acquired.IsFailure)
        {
            return Result.Failure(acquired.Error!);
        }

        _inventoryRepository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
