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
public sealed class CompleteTunnelManualReinforcementHandler
    : ICommandHandler<CompleteTunnelManualReinforcementCommand, Result>
{
    public const int SkillGrantUnits = 70;

    private readonly ITunnelInfrastructureRepository _tunnels;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;
    private readonly IAgentSkillGrantService _skills;

    public CompleteTunnelManualReinforcementHandler(
        ITunnelInfrastructureRepository tunnels,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events,
        IAgentSkillGrantService skills)
    {
        _tunnels = tunnels ?? throw new ArgumentNullException(nameof(tunnels));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
    }

    public Result Handle(CompleteTunnelManualReinforcementCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not TunnelManualReinforcementJobDefinition definition)
        {
            return Result.Failure(TunnelManualReinforcementErrors.JobMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize
            || job.AssignedAgentId != definition.ResidentId)
        {
            return Result.Failure(TunnelManualReinforcementErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventory.Get();
        ItemStackSnapshot? stack = inventory.GetStack(definition.SourceStackId);
        Result source = CreateTunnelManualReinforcementHandler.ValidateSource(
            stack,
            definition.ResidentId,
            definition.Kind);
        if (source.IsFailure
            || inventory.GetReservedQuantity(definition.SourceStackId, job.Id) < 1)
        {
            return Result.Failure(TunnelManualReinforcementErrors.SourceUnavailable);
        }

        Result<TunnelManualReinforcementPlan> planned =
            TunnelManualReinforcementPlanner.Resolve(
                _tunnels.Get().CaptureSnapshot(),
                stack!.ItemId,
                definition.TargetCell);
        if (planned.IsFailure
            || planned.Value.SegmentId != definition.SegmentId
            || planned.Value.Kind != definition.Kind)
        {
            return Result.Failure(
                planned.IsFailure
                    ? planned.Error!
                    : TunnelManualReinforcementErrors.TargetUnavailable);
        }

        AgentSkillId skill = definition.Kind ==
            TunnelManualReinforcementKind.WoodenSupport
                ? AgentSkillCatalog.Woodworking
                : AgentSkillCatalog.Stonework;
        var bundle = new SkillGrantBundle(
            definition.ResidentId,
            SkillGrantSourceKind.JobCompleted,
            $"tunnel-manual:{job.Id}",
            command.Tick,
            new[] { new SkillGrant(skill, SkillGrantUnits) });
        Result skillValidation = _skills.Validate(bundle);
        if (skillValidation.IsFailure)
        {
            return skillValidation;
        }

        Result consumed = inventory.ConsumeReserved(
            job.Id,
            definition.SourceStackId,
            quantity: 1,
            command.Tick);
        if (consumed.IsFailure)
        {
            return consumed;
        }

        TunnelInfrastructureState tunnels = _tunnels.Get();
        Result committed = definition.Kind switch
        {
            TunnelManualReinforcementKind.WoodenSupport =>
                tunnels.RegisterCompletedWoodenSupport(
                    definition.SegmentId,
                    definition.TargetCell,
                    command.Tick),
            TunnelManualReinforcementKind.StoneFloorTrim =>
                tunnels.RegisterCompletedStoneFloorTrim(
                    definition.SegmentId,
                    definition.TargetCell,
                    command.Tick),
            TunnelManualReinforcementKind.JunctionStoneTrim =>
                tunnels.RegisterCompletedJunctionStoneTrim(
                    definition.TargetCell,
                    command.Tick),
            _ => Result.Failure(TunnelManualReinforcementErrors.TargetUnavailable),
        };
        if (committed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated manual tunnel reinforcement could not commit.");
        }

        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated manual tunnel reinforcement job could not complete.");
        }

        Result<SkillRedistributionReport> applied = _skills.ApplyConfirmed(bundle);
        if (applied.IsFailure)
        {
            throw new InvalidOperationException(
                $"Manual tunnel reinforcement skill grant failed: {applied.Error}");
        }

        _inventory.Save(inventory);
        _tunnels.Save(tunnels);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(tunnels.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class CancelTunnelManualReinforcementHandler
    : ICommandHandler<CancelTunnelManualReinforcementCommand, Result>
{
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CancelTunnelManualReinforcementHandler(
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(CancelTunnelManualReinforcementCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not TunnelManualReinforcementJobDefinition)
        {
            return Result.Failure(TunnelManualReinforcementErrors.JobMismatch);
        }

        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason(
                command.ReasonCode,
                "Manual tunnel reinforcement was cancelled before material commit."),
            command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        InventoryState inventory = _inventory.Get();
        inventory.ReleaseReservations(command.JobId, command.Tick);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
