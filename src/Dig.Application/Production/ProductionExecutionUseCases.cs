using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Agents;
using Dig.Domain.Content;
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
    private readonly IAgentRepository? _agents;
    private readonly IEventSink _eventSink;

    public BeginProductionWorkHandler(
        IProductionRepository productionRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
        : this(productionRepository, jobRepository, agents: null, eventSink)
    {
    }

    public BeginProductionWorkHandler(
        IProductionRepository productionRepository,
        IJobRepository jobRepository,
        IAgentRepository? agents,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _jobRepository = jobRepository;
        _agents = agents;
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

        ProductionOrderSnapshot? order = production.Get(command.OrderId);
        long[]? resolvedDurations = null;
        if (order?.Recipe.UsesMaterialSteps == true)
        {
            EntityId workerId = job.AssignedAgentId
                ?? throw new InvalidOperationException("Claimed job has no worker.");
            AgentState? worker = _agents?.Get(workerId);
            if (worker is null)
            {
                return Result.Failure(AgentApplicationErrors.NotFound);
            }

            AgentSnapshot snapshot = worker.CreateSnapshot(command.Tick);
            resolvedDurations = order.Recipe.MaterialSteps
                .Select(step => ProductionStepTiming.ResolveDurationTicks(
                    step.BaseDurationTicks,
                    snapshot.GetSkillLevel(step.SkillId)))
                .ToArray();
        }

        Result jobStarted = jobs.Start(command.JobId, command.Tick);
        if (jobStarted.IsFailure)
        {
            return jobStarted;
        }

        Result orderStarted = production.Start(
            command.OrderId,
            command.Tick,
            resolvedDurations);
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
    private readonly IInventoryRepository? _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IAgentRepository _agents;
    private readonly IEventSink _eventSink;

    public ApplyProductionWorkHandler(
        IProductionRepository productionRepository,
        IJobRepository jobRepository,
        IAgentRepository agents,
        IEventSink eventSink)
        : this(productionRepository, inventoryRepository: null, jobRepository, agents, eventSink)
    {
    }

    public ApplyProductionWorkHandler(
        IProductionRepository productionRepository,
        IInventoryRepository? inventoryRepository,
        IJobRepository jobRepository,
        IAgentRepository agents,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
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
        ProductionOrderSnapshot? order = production.Get(command.OrderId);
        if (job?.Definition is not ProductionWorkJobDefinition work
            || work.OrderId != command.OrderId
            || order is null
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        if (!job.AssignedAgentId.HasValue)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        AgentState? workerAgent = _agents.Get(job.AssignedAgentId.Value);
        if (workerAgent is null)
        {
            return Result.Failure(AgentApplicationErrors.NotFound);
        }

        InventoryState? inventory = _inventoryRepository?.Get();
        if (order.Recipe.UsesMaterialSteps)
        {
            if (inventory is null)
            {
                return Result.Failure(ProductionErrors.InvalidStatus);
            }

            Result<ProductionMaterialWorkResult> preview = production.PreviewMaterialWork(
                command.OrderId,
                command.BaseWork);
            if (preview.IsFailure)
            {
                return Result.Failure(preview.Error!);
            }

            InventorySnapshot inventorySnapshot = inventory.CreateSnapshot();
            EntityId workerId = job.AssignedAgentId.Value;
            if (command.RequireResidentCarriedMaterial)
            {
                ProductionMaterialStepSnapshot? activeStep = order.MaterialSteps
                    .Where(value => !value.Consumed)
                    .Select(value => (ProductionMaterialStepSnapshot?)value)
                    .FirstOrDefault();
                if (!activeStep.HasValue
                    || CountResidentReservations(
                        inventorySnapshot,
                        command.OrderId,
                        workerId,
                        activeStep.Value.ItemId) < 1)
                {
                    return Result.Failure(InventoryErrors.ReservationNotFound);
                }
            }

            foreach (IGrouping<ItemId, ItemId> group in preview.Value.ConsumedItems
                .GroupBy(value => value))
            {
                int reserved = command.RequireResidentCarriedMaterial
                    ? CountResidentReservations(
                        inventorySnapshot,
                        command.OrderId,
                        workerId,
                        group.Key)
                    : inventorySnapshot.Stacks
                        .Where(stack => stack.ItemId == group.Key)
                        .SelectMany(stack => stack.Reservations)
                        .Where(reservation => reservation.JobId == command.OrderId)
                        .Sum(reservation => reservation.Quantity);
                if (reserved < group.Count())
                {
                    return Result.Failure(InventoryErrors.ReservationNotFound);
                }
            }

            Result<ProductionMaterialWorkResult> material = production.AddMaterialWork(
                command.OrderId,
                command.BaseWork,
                command.Tick);
            if (material.IsFailure)
            {
                return Result.Failure(material.Error!);
            }

            foreach (ItemId itemId in material.Value.ConsumedItems)
            {
                Result consumed = command.RequireResidentCarriedMaterial
                    ? inventory.ConsumeReservedProductionUnit(
                        command.OrderId,
                        workerId,
                        itemId,
                        command.Tick)
                    : inventory.ConsumeNextReserved(
                        command.OrderId,
                        itemId,
                        quantity: 1,
                        command.Tick);
                if (consumed.IsFailure)
                {
                    throw new InvalidOperationException(
                        $"Prevalidated material step could not consume '{itemId}': "
                        + consumed.Error);
                }
            }
        }
        else
        {
            ProductionWorkContext context = ProductionWorkContext.ForRecipe(
                order.Recipe,
                workerAgent.CreateSnapshot(command.Tick),
                command.ConditionEfficiencyBasisPoints);
            int effectiveWork = ProductionEfficiency.CalculateEffectiveWork(
                command.BaseWork,
                context);
            Result applied = production.AddWork(command.OrderId, effectiveWork, command.Tick);
            if (applied.IsFailure)
            {
                return applied;
            }
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
        if (inventory is not null && _inventoryRepository is not null)
        {
            _inventoryRepository.Save(inventory);
            _eventSink.Append(inventory.DequeueUncommittedEvents());
        }
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static int CountResidentReservations(
        InventorySnapshot inventory,
        EntityId orderId,
        EntityId residentId,
        ItemId itemId)
    {
        return inventory.Stacks
            .Where(stack => stack.ItemId == itemId
                && stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner
                && stack.Location.OwnerId == residentId)
            .SelectMany(stack => stack.Reservations)
            .Where(reservation => reservation.JobId == orderId)
            .Sum(reservation => reservation.Quantity);
    }
}

}
