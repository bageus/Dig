using System;
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

public sealed class CompleteTunnelManualWorkHandler
    : ICommandHandler<CompleteTunnelManualWorkCommand, Result>
{
    public const int SkillGrantUnits = 70;

    private readonly ITunnelInfrastructureRepository _tunnels;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;
    private readonly IAgentSkillGrantService _skillGrants;

    public CompleteTunnelManualWorkHandler(
        ITunnelInfrastructureRepository tunnels,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events,
        IAgentSkillGrantService skillGrants)
    {
        _tunnels = tunnels ?? throw new ArgumentNullException(nameof(tunnels));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _skillGrants = skillGrants
            ?? throw new ArgumentNullException(nameof(skillGrants));
    }

    public Result Handle(CompleteTunnelManualWorkCommand command)
    {
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not TunnelManualWorkJobDefinition definition)
        {
            return Result.Failure(TunnelManualPlacementErrors.JobMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(TunnelManualPlacementErrors.InvalidJobStage);
        }

        if (job.AssignedAgentId != definition.OwnerResidentId)
        {
            return Result.Failure(TunnelManualPlacementErrors.OwnerMismatch);
        }

        InventoryState inventory = _inventory.Get();
        ItemStackSnapshot? source = inventory.GetStack(definition.SourceStackId);
        Result sourceValidation =
            ValidateTunnelManualPlacementHandler.ValidateSource(
                source,
                definition.OwnerResidentId,
                definition.SourceStackId);
        if (sourceValidation.IsFailure
            || inventory.GetReservedQuantity(
                definition.SourceStackId,
                job.Id) < 1)
        {
            return Result.Failure(TunnelManualPlacementErrors.SourceUnavailable);
        }

        TunnelInfrastructureState tunnels = _tunnels.Get();
        Result<TunnelManualPlacementPlan> target =
            TunnelManualTargetResolver.Resolve(
                tunnels.CaptureSnapshot(),
                definition.OwnerResidentId,
                definition.SourceStackId,
                source!.ItemId.ToString(),
                definition.TargetCell);
        if (target.IsFailure
            || target.Value.SegmentId != definition.SegmentId
            || target.Value.Kind != definition.Kind)
        {
            return Result.Failure(target.IsFailure
                ? target.Error!
                : TunnelManualPlacementErrors.TargetUnavailable);
        }

        SkillGrantBundle skillBundle = CreateSkillBundle(job, definition, command.Tick);
        Result skillValidation = _skillGrants.Validate(skillBundle);
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
            throw new InvalidOperationException(
                "Validated manual tunnel material could not be consumed.");
        }

        Result committed = Commit(tunnels, definition, command.Tick);
        if (committed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated manual tunnel target could not be committed.");
        }

        CancelObsoleteAutomaticSupportJobs(
            tunnels,
            inventory,
            jobs,
            definition,
            command.Tick);

        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated manual tunnel job could not complete.");
        }

        ApplySkill(skillBundle);
        _inventory.Save(inventory);
        _tunnels.Save(tunnels);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(tunnels.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static void CancelObsoleteAutomaticSupportJobs(
        TunnelInfrastructureState tunnels,
        InventoryState inventory,
        JobSystem jobs,
        TunnelManualWorkJobDefinition definition,
        long tick)
    {
        if (definition.Kind != TunnelManualWorkKind.WoodenSupport)
        {
            return;
        }

        CellId? currentTarget = tunnels.GetSegment(definition.SegmentId)
            ?.NextAutomaticSupportTarget?.TargetCell;
        JobSnapshot[] obsolete = jobs.GetAll()
            .Where(candidate => !candidate.IsTerminal
                && candidate.Definition is TunnelAutomaticWorkJobDefinition automatic
                && automatic.Kind == TunnelAutomaticWorkKind.WoodenSupport
                && automatic.SegmentId == definition.SegmentId
                && (!currentTarget.HasValue
                    || automatic.TargetCell != currentTarget.Value))
            .ToArray();
        foreach (JobSnapshot candidate in obsolete)
        {
            Result cancelled = jobs.Cancel(
                candidate.Id,
                new JobBlockReason(
                    "tunnel.manual.anchor_changed",
                    "Manual support changed the rolling structural anchor."),
                tick);
            if (cancelled.IsFailure)
            {
                throw new InvalidOperationException(
                    "Obsolete automatic support job could not be cancelled.");
            }

            inventory.ReleaseReservations(candidate.Id, tick);
        }
    }

    private static Result Commit(
        TunnelInfrastructureState tunnels,
        TunnelManualWorkJobDefinition definition,
        long tick)
    {
        return definition.Kind switch
        {
            TunnelManualWorkKind.WoodenSupport =>
                tunnels.RegisterCompletedWoodenSupport(
                    definition.SegmentId,
                    definition.TargetCell,
                    tick),
            TunnelManualWorkKind.JunctionStoneTrim =>
                tunnels.RegisterCompletedJunctionStoneTrim(
                    definition.TargetCell,
                    tick),
            TunnelManualWorkKind.StoneFloorTrim =>
                tunnels.RegisterCompletedStoneFloorTrim(
                    definition.TargetCell,
                    tick),
            _ => Result.Failure(TunnelManualPlacementErrors.JobMismatch),
        };
    }

    private static SkillGrantBundle CreateSkillBundle(
        JobSnapshot job,
        TunnelManualWorkJobDefinition definition,
        long tick)
    {
        AgentSkillId skill = definition.Kind == TunnelManualWorkKind.WoodenSupport
            ? AgentSkillCatalog.Woodworking
            : AgentSkillCatalog.Stonework;
        return new SkillGrantBundle(
            definition.OwnerResidentId,
            SkillGrantSourceKind.JobCompleted,
            $"tunnel-manual:{job.Id}",
            tick,
            new[] { new SkillGrant(skill, SkillGrantUnits) });
    }

    private void ApplySkill(SkillGrantBundle bundle)
    {
        Result<SkillRedistributionReport> applied =
            _skillGrants.ApplyConfirmed(bundle);
        if (applied.IsFailure)
        {
            throw new InvalidOperationException(
                $"Completed manual tunnel skill grant failed: {applied.Error}");
        }
    }
}

}
