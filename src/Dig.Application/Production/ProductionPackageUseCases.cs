using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;

namespace Dig.Application.Production
{

public sealed class CreateProductionOutputPackageHandler
    : ICommandHandler<CreateProductionOutputPackageCommand, Result>
{
    private readonly IProductionRepository _production;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CreateProductionOutputPackageHandler(
        IProductionRepository production,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events)
    {
        _production = production;
        _inventory = inventory;
        _jobs = jobs;
        _events = events;
    }

    public Result Handle(CreateProductionOutputPackageCommand command)
    {
        ProductionState production = _production.Get();
        InventoryState inventory = _inventory.Get();
        JobSnapshot? job = _jobs.Get().Get(command.JobId);
        if (job?.Definition is not ProductionWorkJobDefinition definition
            || definition.OrderId != command.OrderId
            || job.Status != JobStatus.Claimed
            || command.Location.Kind != ItemLocationKind.World)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        Result registered = production.CreateOutputPackage(
            command.OrderId,
            command.PackageStackId,
            command.Tick);
        if (registered.IsFailure)
        {
            return registered;
        }

        Result created = inventory.AddUnit(
            command.PackageStackId,
            ProductionPackageContent.UnfinishedPackageItemId,
            command.Location,
            command.Tick);
        if (created.IsFailure)
        {
            Result rollback = production.RemoveOutputPackage(
                command.PackageStackId,
                command.Tick);
            if (rollback.IsFailure)
            {
                throw new InvalidOperationException(
                    "Failed production-package creation could not be rolled back.");
            }

            return created;
        }

        _production.Save(production);
        _inventory.Save(inventory);
        _events.Append(production.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class InterruptProductionOrderHandler
    : ICommandHandler<InterruptProductionOrderCommand, Result>
{
    private readonly IProductionRepository _production;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public InterruptProductionOrderHandler(
        IProductionRepository production,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events)
    {
        _production = production;
        _inventory = inventory;
        _jobs = jobs;
        _events = events;
    }

    public Result Handle(InterruptProductionOrderCommand command)
    {
        ProductionState production = _production.Get();
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not ProductionWorkJobDefinition definition
            || definition.OrderId != command.OrderId
            || job.IsTerminal)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        if (job.AssignedAgentId.HasValue)
        {
            Result recovered = ProductionReservedResidentRecovery.DropCarriedItems(
                inventory,
                command.OrderId,
                job.AssignedAgentId.Value,
                command.RecoveryCell,
                command.Tick);
            if (recovered.IsFailure)
            {
                return recovered;
            }
        }

        ProductionOutputPackageSnapshot? package =
            production.GetOutputPackageForOrder(command.OrderId);
        if (package != null)
        {
            ItemStackSnapshot? stack = inventory.GetStack(package.StackId);
            if (stack != null)
            {
                Result removed = inventory.ConsumeAvailableStack(
                    package.StackId,
                    quantity: 1,
                    command.Tick);
                if (removed.IsFailure)
                {
                    return removed;
                }
            }

            Result packageRemoved = production.RemoveOutputPackage(
                package.StackId,
                command.Tick);
            if (packageRemoved.IsFailure)
            {
                return packageRemoved;
            }
        }

        inventory.ReleaseReservations(command.OrderId, command.Tick);
        Result reset = production.ResetForRetry(
            command.OrderId,
            command.Reason,
            command.Tick);
        if (reset.IsFailure)
        {
            return reset;
        }

        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason("production_direct_command_replaced", command.Reason),
            command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        _production.Save(production);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(production.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
