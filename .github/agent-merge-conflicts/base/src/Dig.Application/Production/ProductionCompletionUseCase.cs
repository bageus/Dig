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

        EntityId[] outputIds = command.OutputStackIds
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (outputIds.Length != order.Recipe.Outputs.Count
            || outputIds.Any(value => value.IsEmpty)
            || outputIds.Distinct().Count() != outputIds.Length)
        {
            return Result.Failure(ProductionErrors.OutputIdsMismatch);
        }

        SkillGrantBundle? skillBundle = CreateSkillBundle(
            order,
            workerId,
            command.Tick);
        if (skillBundle is not null)
        {
            Result skillValidation = _skillGrants.Validate(skillBundle);
            if (skillValidation.IsFailure)
            {
                return skillValidation;
            }
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
        ItemLocation outputLocation = command.OutputLocation
            ?? ItemLocation.InBuilding(order.BuildingId);
        Result committed = order.Recipe.UsesMaterialSteps
            ? inventory.CreateProductionOutputs(
                order.Id,
                outputs,
                outputLocation,
                command.Tick)
            : inventory.CompleteProductionTransaction(
                order.Id,
                order.InputAllocations,
                outputs,
                outputLocation,
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
            == Dig.Domain.Content.ProductionSkillGrantScale.PerOrder
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
