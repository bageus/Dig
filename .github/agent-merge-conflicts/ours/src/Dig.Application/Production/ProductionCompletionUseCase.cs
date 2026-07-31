using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;

namespace Dig.Application.Production
{

public sealed class CompleteProductionOrderHandler
    : ICommandHandler<CompleteProductionOrderCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;
    private readonly IAgentSkillGrantService _skillGrants;

    public CompleteProductionOrderHandler(
        IProductionRepository productionRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink,
        IAgentSkillGrantService skillGrants)
    {
        _productionRepository = productionRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
        _skillGrants = skillGrants
            ?? throw new ArgumentNullException(nameof(skillGrants));
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

        EntityId workerId = job.AssignedAgentId
            ?? throw new InvalidOperationException(
                "An in-progress production job must retain its worker.");
        SkillGrantBundle? skillBundle = CreateSkillBundle(order, workerId, command.Tick);
        if (skillBundle is not null)
        {
            Result skillValidation = _skillGrants.Validate(skillBundle);
            if (skillValidation.IsFailure)
            {
                return skillValidation;
            }
        }

        Result committed = command.PackageStackId.HasValue
            ? CommitStagedPackage(production, inventory, order, command)
            : CommitLegacyOutputs(inventory, order, command);
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

        if (skillBundle is not null)
        {
            ApplyConfirmedSkillResult(skillBundle);
        }

        _productionRepository.Save(production);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static Result CommitStagedPackage(
        ProductionState production,
        InventoryState inventory,
        ProductionOrderSnapshot order,
        CompleteProductionOrderCommand command)
    {
        EntityId packageStackId = command.PackageStackId!.Value;
        ProductionOutputPackageSnapshot? package =
            production.GetOutputPackage(packageStackId);
        ItemStackSnapshot? packageStack = inventory.GetStack(packageStackId);
        if (package == null
            || package.OrderId != order.Id
            || package.Kind != ProductionOutputPackageKind.Unfinished
            || packageStack == null
            || packageStack.ItemId != ProductionPackageContent.UnfinishedPackageItemId)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotFound);
        }

        ProductionOutputPackageKind kind = ResolveOutputKind(inventory, order);
        if (kind == ProductionOutputPackageKind.Building)
        {
            EntityId[] outputIds = ValidateOutputIds(order, command.OutputStackIds);
            if (outputIds.Length == 0)
            {
                return Result.Failure(ProductionErrors.OutputIdsMismatch);
            }

            ItemStackCreation[] outputs = order.Recipe.Outputs
                .OrderBy(value => value.ItemId)
                .Zip(outputIds, (definition, id) => new ItemStackCreation(
                    id,
                    definition.ItemId,
                    definition.Quantity))
                .ToArray();
            Result replaced = inventory.ReplaceProductionPackage(
                packageStackId,
                ProductionPackageContent.UnfinishedPackageItemId,
                outputs,
                command.Tick);
            if (replaced.IsFailure)
            {
                return replaced;
            }

            Result removed = production.RemoveOutputPackage(packageStackId, command.Tick);
            return removed;
        }

        ItemId closedItemId = ProductionPackageContent.GetClosedItemId(kind);
        Result closedStack = inventory.ReplaceProductionPackage(
            packageStackId,
            ProductionPackageContent.UnfinishedPackageItemId,
            new[] { new ItemStackCreation(packageStackId, closedItemId, 1) },
            command.Tick);
        if (closedStack.IsFailure)
        {
            return closedStack;
        }

        return production.CloseOutputPackage(
            packageStackId,
            kind,
            order.Recipe.Outputs,
            command.Tick);
    }

    private static Result CommitLegacyOutputs(
        InventoryState inventory,
        ProductionOrderSnapshot order,
        CompleteProductionOrderCommand command)
    {
        EntityId[] outputIds = ValidateOutputIds(order, command.OutputStackIds);
        if (outputIds.Length == 0)
        {
            return Result.Failure(ProductionErrors.OutputIdsMismatch);
        }

        ItemStackCreation[] outputs = order.Recipe.Outputs
            .OrderBy(value => value.ItemId)
            .Zip(outputIds, (definition, stackId) => new ItemStackCreation(
                stackId,
                definition.ItemId,
                definition.Quantity))
            .ToArray();
        ItemLocation outputLocation = command.OutputLocation
            ?? ItemLocation.InBuilding(order.BuildingId);
        return order.Recipe.UsesMaterialSteps
            ? inventory.CreateProductionOutputs(order.Id, outputs, outputLocation, command.Tick)
            : inventory.CompleteProductionTransaction(
                order.Id,
                order.InputAllocations,
                outputs,
                outputLocation,
                command.Tick);
    }

    private static EntityId[] ValidateOutputIds(
        ProductionOrderSnapshot order,
        System.Collections.Generic.IReadOnlyCollection<EntityId> ids)
    {
        EntityId[] values = ids
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        return values.Length == order.Recipe.Outputs.Count
            && values.All(value => !value.IsEmpty)
            && values.Distinct().Count() == values.Length
                ? values
                : Array.Empty<EntityId>();
    }

    private static ProductionOutputPackageKind ResolveOutputKind(
        InventoryState inventory,
        ProductionOrderSnapshot order)
    {
        ProductionOutputPackageKind[] kinds = order.Recipe.Outputs
            .Select(value => ProductionPackageContent.ResolveKind(
                inventory.Catalog.Get(value.ItemId)))
            .Distinct()
            .ToArray();
        return kinds.Length == 1 ? kinds[0] : ProductionOutputPackageKind.Tool;
    }

    private static SkillGrantBundle? CreateSkillBundle(
        ProductionOrderSnapshot order,
        EntityId workerId,
        long tick)
    {
        SkillGrantProfile? profile = order.Recipe.SkillGrantProfile;
        if (profile is null)
        {
            return null;
        }

        int multiplier = order.Recipe.SkillGrantScale
            == ProductionSkillGrantScale.PerOrder
                ? 1
                : order.Recipe.Outputs.Sum(value => value.Quantity);
        return new SkillGrantBundle(
            workerId,
            SkillGrantSourceKind.ProductionCommitted,
            order.Id.ToString(),
            tick,
            profile.Multiply(multiplier));
    }

    private void ApplyConfirmedSkillResult(SkillGrantBundle bundle)
    {
        Result<SkillRedistributionReport> applied = _skillGrants.ApplyConfirmed(bundle);
        if (applied.IsFailure)
        {
            throw new InvalidOperationException(
                $"Committed production skill grant failed: {applied.Error}");
        }
    }
}

}
