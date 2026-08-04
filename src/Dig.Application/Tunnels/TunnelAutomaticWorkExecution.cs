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

public sealed class CompleteTunnelAutomaticWorkCommand : ICommand<Result>
{
    public CompleteTunnelAutomaticWorkCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public long Tick { get; }
}

public static class TunnelAutomaticWorkExecutionErrors
{
    public static readonly DomainError JobMismatch = new DomainError(
        "tunnel.automatic_work.job_mismatch",
        "The requested job is not automatic tunnel infrastructure work.");

    public static readonly DomainError ManualPlacementRequired = new DomainError(
        "tunnel.automatic_work.manual_placement_required",
        "Junction stone trim is completed only through resident-owned placement mode.");

    public static readonly DomainError InvalidJobStage = new DomainError(
        "tunnel.automatic_work.invalid_job_stage",
        "Automatic tunnel work must be in its finalization stage.");

    public static readonly DomainError WorkerMissing = new DomainError(
        "tunnel.automatic_work.worker_missing",
        "Automatic tunnel work has no authoritative worker.");

    public static readonly DomainError SourceUnresolved = new DomainError(
        "tunnel.automatic_work.source_unresolved",
        "Automatic tunnel work has no resolved material source.");

    public static readonly DomainError SourceInvalid = new DomainError(
        "tunnel.automatic_work.source_invalid",
        "The reserved automatic tunnel material no longer matches its source contract.");

    public static readonly DomainError TargetObsolete = new DomainError(
        "tunnel.automatic_work.target_obsolete",
        "The authoritative tunnel infrastructure target has changed.");
}

public sealed class CompleteTunnelAutomaticWorkHandler
    : ICommandHandler<CompleteTunnelAutomaticWorkCommand, Result>
{
    public const int SkillGrantUnits = 70;

    private readonly ITunnelInfrastructureRepository _tunnelRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;
    private readonly IAgentSkillGrantService _skillGrants;

    public CompleteTunnelAutomaticWorkHandler(
        ITunnelInfrastructureRepository tunnelRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink,
        IAgentSkillGrantService skillGrants)
    {
        _tunnelRepository = tunnelRepository
            ?? throw new ArgumentNullException(nameof(tunnelRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
        _skillGrants = skillGrants
            ?? throw new ArgumentNullException(nameof(skillGrants));
    }

    public Result Handle(CompleteTunnelAutomaticWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not TunnelAutomaticWorkJobDefinition definition)
        {
            return Result.Failure(TunnelAutomaticWorkExecutionErrors.JobMismatch);
        }

        if (definition.Kind != TunnelAutomaticWorkKind.WoodenSupport)
        {
            return Result.Failure(
                TunnelAutomaticWorkExecutionErrors.ManualPlacementRequired);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(TunnelAutomaticWorkExecutionErrors.InvalidJobStage);
        }

        if (!job.AssignedAgentId.HasValue)
        {
            return Result.Failure(TunnelAutomaticWorkExecutionErrors.WorkerMissing);
        }

        if (!definition.SourceStackId.HasValue || !definition.SourceCell.HasValue)
        {
            return Result.Failure(TunnelAutomaticWorkExecutionErrors.SourceUnresolved);
        }

        TunnelInfrastructureState tunnels = _tunnelRepository.Get();
        if (!IsCurrentTarget(tunnels, definition))
        {
            return Result.Failure(TunnelAutomaticWorkExecutionErrors.TargetObsolete);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? source = inventory.GetStack(definition.SourceStackId.Value);
        if (source is null
            || source.ItemId != definition.RequiredItemId
            || source.Location != ItemLocation.InWorld(definition.SourceCell.Value)
            || inventory.GetReservedQuantity(source.StackId, job.Id) < 1)
        {
            return Result.Failure(TunnelAutomaticWorkExecutionErrors.SourceInvalid);
        }

        SkillGrantBundle skillBundle = CreateSkillBundle(job, command.Tick);
        Result skillValidation = _skillGrants.Validate(skillBundle);
        if (skillValidation.IsFailure)
        {
            return skillValidation;
        }

        Result consumed = inventory.ConsumeReserved(
            job.Id,
            source.StackId,
            quantity: 1,
            command.Tick);
        if (consumed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated automatic tunnel material could not be consumed.");
        }

        Result infrastructureCommitted = tunnels.RegisterCompletedWoodenSupport(
            definition.SegmentId,
            definition.TargetCell,
            command.Tick);
        if (infrastructureCommitted.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated automatic tunnel target could not be committed.");
        }

        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated automatic tunnel job could not complete its final stage.");
        }

        ApplyConfirmedSkillResult(skillBundle);
        _inventoryRepository.Save(inventory);
        _tunnelRepository.Save(tunnels);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(tunnels.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static bool IsCurrentTarget(
        TunnelInfrastructureState tunnels,
        TunnelAutomaticWorkJobDefinition definition)
    {
        HorizontalTunnelSegmentSnapshot? segment =
            tunnels.GetSegment(definition.SegmentId);
        return segment?.NextAutomaticSupportTarget is
            TunnelAutomaticSupportTargetSnapshot target
            && target.TargetCell == definition.TargetCell;
    }

    private static SkillGrantBundle CreateSkillBundle(
        JobSnapshot job,
        long tick)
    {
        return new SkillGrantBundle(
            job.AssignedAgentId!.Value,
            SkillGrantSourceKind.JobCompleted,
            $"tunnel-automatic:{job.Id}",
            tick,
            new[]
            {
                new SkillGrant(
                    AgentSkillCatalog.Woodworking,
                    SkillGrantUnits),
            });
    }

    private void ApplyConfirmedSkillResult(SkillGrantBundle bundle)
    {
        Result<SkillRedistributionReport> applied =
            _skillGrants.ApplyConfirmed(bundle);
        if (applied.IsFailure)
        {
            throw new InvalidOperationException(
                $"Completed automatic tunnel work skill grant failed: {applied.Error}");
        }
    }
}
}
