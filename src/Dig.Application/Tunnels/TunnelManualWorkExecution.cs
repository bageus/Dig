using System;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public sealed class ValidateTunnelManualPlacementHandler
    : IQueryHandler<
        ValidateTunnelManualPlacementQuery,
        Result<TunnelManualPlacementPlan>>
{
    private readonly ITunnelInfrastructureRepository _tunnels;
    private readonly IInventoryRepository _inventory;

    public ValidateTunnelManualPlacementHandler(
        ITunnelInfrastructureRepository tunnels,
        IInventoryRepository inventory)
    {
        _tunnels = tunnels ?? throw new ArgumentNullException(nameof(tunnels));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public Result<TunnelManualPlacementPlan> Handle(
        ValidateTunnelManualPlacementQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        ItemStackSnapshot? stack = _inventory.Get().GetStack(query.SourceStackId);
        Result source = ValidateSource(stack, query.ResidentId, query.SourceStackId);
        if (source.IsFailure)
        {
            return Result<TunnelManualPlacementPlan>.Failure(source.Error!);
        }

        return TunnelManualTargetResolver.Resolve(
            _tunnels.Get().CaptureSnapshot(),
            query.ResidentId,
            query.SourceStackId,
            stack!.ItemId.ToString(),
            query.TargetCell);
    }

    internal static Result ValidateSource(
        ItemStackSnapshot? stack,
        EntityId residentId,
        EntityId sourceStackId)
    {
        if (residentId.IsEmpty
            || sourceStackId.IsEmpty
            || stack == null
            || stack.StackId != sourceStackId
            || stack.Location.Kind != ItemLocationKind.AgentInventory
            || !stack.Location.HasOwner
            || stack.Location.OwnerId != residentId
            || !stack.Location.HasResidentSlot
            || stack.AvailableQuantity < 1)
        {
            return Result.Failure(TunnelManualPlacementErrors.SourceUnavailable);
        }

        return Result.Success();
    }
}

public sealed class CreateTunnelManualWorkHandler
    : ICommandHandler<CreateTunnelManualWorkCommand, Result<EntityId>>
{
    private readonly ITunnelInfrastructureRepository _tunnels;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CreateTunnelManualWorkHandler(
        ITunnelInfrastructureRepository tunnels,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events)
    {
        _tunnels = tunnels ?? throw new ArgumentNullException(nameof(tunnels));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<EntityId> Handle(CreateTunnelManualWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        ItemStackSnapshot? stack = inventory.GetStack(command.SourceStackId);
        Result source = ValidateTunnelManualPlacementHandler.ValidateSource(
            stack,
            command.ResidentId,
            command.SourceStackId);
        if (source.IsFailure)
        {
            return Result<EntityId>.Failure(source.Error!);
        }

        Result<TunnelManualPlacementPlan> planned =
            TunnelManualTargetResolver.Resolve(
                _tunnels.Get().CaptureSnapshot(),
                command.ResidentId,
                command.SourceStackId,
                stack!.ItemId.ToString(),
                command.TargetCell);
        if (planned.IsFailure)
        {
            return Result<EntityId>.Failure(planned.Error!);
        }

        if (jobs.Get(command.JobId) != null)
        {
            return Result<EntityId>.Failure(JobErrors.AlreadyExists);
        }

        TunnelManualPlacementPlan plan = planned.Value;
        var definition = new TunnelManualWorkJobDefinition(
            command.JobId,
            command.ResidentId,
            command.SourceStackId,
            plan.SegmentId,
            plan.Kind,
            command.TargetCell,
            command.Tick,
            JobRetryPolicy.Default);
        Result reserved = inventory.ReserveQuantity(
            command.SourceStackId,
            command.JobId,
            quantity: 1,
            command.Tick);
        if (reserved.IsFailure)
        {
            return Result<EntityId>.Failure(reserved.Error!);
        }

        Result added = jobs.Add(definition);
        Result activated = added.IsSuccess
            ? Activate(jobs, definition, command.Tick)
            : added;
        if (activated.IsFailure)
        {
            if (jobs.Get(command.JobId) is JobSnapshot addedJob && !addedJob.IsTerminal)
            {
                jobs.Cancel(
                    command.JobId,
                    new JobBlockReason(
                        "tunnel.manual.create_failed",
                        activated.Error!.Message),
                    command.Tick);
            }

            inventory.ReleaseReservations(command.JobId, command.Tick);
            SaveAndPublish(inventory, jobs);
            return Result<EntityId>.Failure(activated.Error!);
        }

        SaveAndPublish(inventory, jobs);
        return Result<EntityId>.Success(command.JobId);
    }

    private static Result Activate(
        JobSystem jobs,
        TunnelManualWorkJobDefinition definition,
        long tick)
    {
        Result available = jobs.MakeAvailable(definition.Id, tick);
        return available.IsFailure
            ? available
            : jobs.Claim(definition.Id, definition.OwnerResidentId, tick);
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CancelTunnelManualWorkHandler
    : ICommandHandler<CancelTunnelManualWorkCommand, Result>
{
    private static readonly JobBlockReason CancelledReason = new JobBlockReason(
        "tunnel.manual.interrupted",
        "Owner-resident manual tunnel work was interrupted before commit.");

    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CancelTunnelManualWorkHandler(
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(CancelTunnelManualWorkCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not TunnelManualWorkJobDefinition)
        {
            return Result.Failure(TunnelManualPlacementErrors.JobMismatch);
        }

        if (job.IsTerminal)
        {
            return job.Status == JobStatus.Cancelled
                ? Result.Success()
                : Result.Failure(JobErrors.InvalidStatus);
        }

        InventoryState inventory = _inventory.Get();
        Result cancelled = jobs.Cancel(command.JobId, CancelledReason, command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        inventory.ReleaseReservations(command.JobId, command.Tick);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
