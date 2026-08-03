using System;
using System.Collections.Generic;
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

public static class ProductionPackageMaterialization
{
    public static int RequiredOutputStackCount(ProductionOutputPackageSnapshot package)
    {
        if (package == null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        return package.Kind == ProductionOutputPackageKind.Food
            ? checked(package.Manifest.Sum(value => value.Quantity))
            : package.Manifest.Count;
    }

    public static ItemStackCreation[] CreateOutputs(
        ProductionOutputPackageSnapshot package,
        IReadOnlyList<EntityId> outputStackIds)
    {
        if (package == null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        if (outputStackIds == null)
        {
            throw new ArgumentNullException(nameof(outputStackIds));
        }

        int requiredCount = RequiredOutputStackCount(package);
        if (outputStackIds.Count != requiredCount)
        {
            throw new ArgumentException(
                "Output stack ids must match the package materialization count.",
                nameof(outputStackIds));
        }

        if (package.Kind != ProductionOutputPackageKind.Food)
        {
            return package.Manifest
                .Zip(
                    outputStackIds,
                    (item, id) => new ItemStackCreation(
                        id,
                        item.ItemId,
                        item.Quantity))
                .ToArray();
        }

        List<ItemStackCreation> outputs = new List<ItemStackCreation>(requiredCount);
        int idIndex = 0;
        foreach (ContentItemQuantity item in package.Manifest)
        {
            for (int unit = 0; unit < item.Quantity; unit++)
            {
                outputs.Add(new ItemStackCreation(
                    outputStackIds[idIndex++],
                    item.ItemId,
                    quantity: 1));
            }
        }

        return outputs.ToArray();
    }
}

public sealed class StartProductionPackageUseHandler
    : ICommandHandler<StartProductionPackageUseCommand, Result>
{
    private readonly IProductionRepository _production;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public StartProductionPackageUseHandler(
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

    public Result Handle(StartProductionPackageUseCommand command)
    {
        ProductionOutputPackageSnapshot? package =
            _production.Get().GetOutputPackage(command.PackageStackId);
        ItemStackSnapshot? stack = _inventory.Get().GetStack(command.PackageStackId);
        if (package == null
            || !package.IsClosed
            || stack == null
            || stack.Location.Kind != ItemLocationKind.World
            || !stack.Location.HasCell)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotUsable);
        }

        JobSystem jobs = _jobs.Get();
        if (jobs.GetReservations().Any(value =>
            value.Key == ReservationKey.ForAgent(command.WorkerId)))
        {
            return Result.Failure(JobErrors.AgentUnavailable);
        }

        ProductionPackageUseJobDefinition definition = new ProductionPackageUseJobDefinition(
            command.JobId,
            command.PackageStackId,
            stack.Location.CellId,
            command.WorkPosition,
            package.Version,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result result = jobs.Add(definition);
        if (result.IsSuccess) result = jobs.MakeAvailable(command.JobId, command.Tick);
        if (result.IsSuccess) result = jobs.Claim(command.JobId, command.WorkerId, command.Tick);
        if (result.IsSuccess) result = jobs.Start(command.JobId, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _jobs.Save(jobs);
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class AdvanceProductionPackageUseHandler
    : ICommandHandler<AdvanceProductionPackageUseCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public AdvanceProductionPackageUseHandler(IJobRepository jobs, IEventSink events)
    {
        _jobs = jobs;
        _events = events;
    }

    public Result Handle(AdvanceProductionPackageUseCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not ProductionPackageUseJobDefinition
            || job.Status != JobStatus.InProgress
            || job.Stage == JobStageKind.Finalize)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotUsable);
        }

        Result advanced = jobs.AdvanceStage(command.JobId, command.Tick);
        if (advanced.IsSuccess)
        {
            _jobs.Save(jobs);
            _events.Append(jobs.DequeueUncommittedEvents());
        }

        return advanced;
    }
}

public sealed class CompleteProductionPackageUseHandler
    : ICommandHandler<CompleteProductionPackageUseCommand, Result>
{
    private readonly IProductionRepository _production;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CompleteProductionPackageUseHandler(
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

    public Result Handle(CompleteProductionPackageUseCommand command)
    {
        ProductionState production = _production.Get();
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not ProductionPackageUseJobDefinition definition
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotUsable);
        }

        ProductionOutputPackageSnapshot? package =
            production.GetOutputPackage(definition.PackageStackId);
        if (package == null
            || !package.IsClosed
            || package.Version != definition.PackageVersion)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotUsable);
        }

        EntityId[] ids = command.OutputStackIds
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        int requiredOutputCount =
            ProductionPackageMaterialization.RequiredOutputStackCount(package);
        if (ids.Length != requiredOutputCount
            || ids.Any(value => value.IsEmpty)
            || ids.Distinct().Count() != ids.Length)
        {
            return Result.Failure(ProductionErrors.OutputIdsMismatch);
        }

        ItemStackCreation[] outputs =
            ProductionPackageMaterialization.CreateOutputs(package, ids);
        Result replaced = inventory.ReplaceProductionPackage(
            package.StackId,
            ProductionPackageContent.GetClosedItemId(package.Kind),
            outputs,
            command.Tick);
        if (replaced.IsFailure)
        {
            return replaced;
        }

        Result removed = production.RemoveOutputPackage(package.StackId, command.Tick);
        if (removed.IsFailure)
        {
            throw new InvalidOperationException("Validated package metadata could not be removed.");
        }

        Result completed = jobs.AdvanceStage(command.JobId, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException("Validated package use job could not complete.");
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

public sealed class CancelProductionPackageUseHandler
    : ICommandHandler<CancelProductionPackageUseCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CancelProductionPackageUseHandler(IJobRepository jobs, IEventSink events)
    {
        _jobs = jobs;
        _events = events;
    }

    public Result Handle(CancelProductionPackageUseCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not ProductionPackageUseJobDefinition || job.IsTerminal)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotUsable);
        }

        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason("production_package_use_cancelled", command.Reason),
            command.Tick);
        if (cancelled.IsSuccess)
        {
            _jobs.Save(jobs);
            _events.Append(jobs.DequeueUncommittedEvents());
        }

        return cancelled;
    }
}

}
