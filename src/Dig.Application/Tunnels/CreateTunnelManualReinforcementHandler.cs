using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class CreateTunnelManualReinforcementHandler
    : ICommandHandler<CreateTunnelManualReinforcementCommand, Result>
{
    private readonly ITunnelInfrastructureRepository _tunnels;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CreateTunnelManualReinforcementHandler(
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

    public Result Handle(CreateTunnelManualReinforcementCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        ItemStackSnapshot? stack = inventory.GetStack(command.SourceStackId);
        Result source = ValidateSource(stack, command.ResidentId, command.Plan.Kind);
        if (source.IsFailure)
        {
            return source;
        }

        Result<TunnelManualReinforcementPlan> current =
            TunnelManualReinforcementPlanner.Resolve(
                _tunnels.Get().CaptureSnapshot(),
                stack!.ItemId,
                command.Plan.TargetCell);
        if (current.IsFailure
            || current.Value.SegmentId != command.Plan.SegmentId
            || current.Value.Kind != command.Plan.Kind)
        {
            return Result.Failure(
                current.IsFailure
                    ? current.Error!
                    : TunnelManualReinforcementErrors.TargetUnavailable);
        }

        JobSnapshot? conflicting = jobs.GetAll().FirstOrDefault(job =>
            !job.IsTerminal
            && TargetCell(job.Definition) == command.Plan.TargetCell);
        if (conflicting != null
            && conflicting.AssignedAgentId.HasValue
            && conflicting.AssignedAgentId.Value != command.ResidentId)
        {
            return Result.Failure(TunnelManualReinforcementErrors.TargetReserved);
        }

        if (conflicting != null)
        {
            Result cancelled = jobs.Cancel(
                conflicting.Id,
                new JobBlockReason(
                    "tunnel.manual_reinforcement.replaced_automatic",
                    "A validated direct reinforcement order replaced the pending automatic target."),
                command.Tick);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            inventory.ReleaseReservations(conflicting.Id, command.Tick);
        }

        Result reserved = inventory.ReserveQuantity(
            command.SourceStackId,
            command.JobId,
            quantity: 1,
            command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        var definition = new TunnelManualReinforcementJobDefinition(
            command.JobId,
            command.ResidentId,
            command.SourceStackId,
            command.Plan.SegmentId,
            command.Plan.Kind,
            command.Plan.TargetCell,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsSuccess)
        {
            added = jobs.MakeAvailable(command.JobId, command.Tick);
        }

        if (added.IsSuccess)
        {
            added = jobs.Claim(command.JobId, command.ResidentId, command.Tick);
        }

        if (added.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return added;
        }

        Save(inventory, jobs);
        return Result.Success();
    }

    public static Result ValidateSource(
        ItemStackSnapshot? stack,
        EntityId residentId,
        TunnelManualReinforcementKind kind)
    {
        ItemId required = kind == TunnelManualReinforcementKind.WoodenSupport
            ? new ItemId("material.mushroom_leg")
            : new ItemId("material.stone");
        if (stack == null
            || stack.ItemId != required
            || stack.Location.Kind != ItemLocationKind.AgentInventory
            || !DropResidentInventoryStackHandler.IsOwnedByResident(
                stack.Location,
                residentId)
            || stack.AvailableQuantity < 1)
        {
            return Result.Failure(TunnelManualReinforcementErrors.SourceUnavailable);
        }

        return Result.Success();
    }

    private static CellId? TargetCell(JobDefinition definition)
    {
        return definition switch
        {
            TunnelAutomaticWorkJobDefinition automatic => automatic.TargetCell,
            TunnelManualReinforcementJobDefinition manual => manual.TargetCell,
            _ => (CellId?)null,
        };
    }

    private void Save(InventoryState inventory, JobSystem jobs)
    {
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }
}

}
