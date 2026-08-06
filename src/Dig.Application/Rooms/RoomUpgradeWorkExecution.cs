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
using Dig.Domain.Rooms;

namespace Dig.Application.Rooms
{

public sealed class CommitRoomUpgradeWorkIntervalHandler
    : ICommandHandler<
        CommitRoomUpgradeWorkIntervalCommand,
        Result<RoomMaterialCommitResult>>
{
    public const int SkillGrantUnits = 50;

    private readonly IRoomInfrastructureRepository _rooms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;
    private readonly IAgentSkillGrantService _skillGrants;

    public CommitRoomUpgradeWorkIntervalHandler(
        IRoomInfrastructureRepository rooms,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events,
        IAgentSkillGrantService skillGrants)
    {
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _skillGrants = skillGrants
            ?? throw new ArgumentNullException(nameof(skillGrants));
    }

    public Result<RoomMaterialCommitResult> Handle(
        CommitRoomUpgradeWorkIntervalCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState rooms = _rooms.Get();
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not RoomUpgradeWorkJobDefinition work
            || !job.AssignedAgentId.HasValue)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomUpgradeExecutionErrors.JobMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomUpgradeExecutionErrors.InvalidStage);
        }

        RoomInfrastructureProjectSnapshot? room = rooms.Get(
            work.RoomInfrastructureId);
        if (room == null
            || !room.ActiveJobIds.Contains(job.Id)
            || !room.TemporaryStockCell.HasValue
            || room.TemporaryStockCell.Value != work.WorkCell)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomUpgradeExecutionErrors.JobMismatch);
        }

        if (room.CompletedMaterialUnits.Contains(command.UnitId))
        {
            return Result<RoomMaterialCommitResult>.Success(
                new RoomMaterialCommitResult(
                    alreadyCommitted: true,
                    improvementCompleted:
                        room.Status == RoomImprovementStatus.Improved));
        }

        RoomMaterialUnitId? next = rooms.GetNextMaterialUnit(
            room.RoomInfrastructureId);
        if (!next.HasValue || next.Value != command.UnitId)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomInfrastructureErrors.InvalidMaterialUnit);
        }

        ItemLocation stock = ItemLocation.InWorld(work.WorkCell);
        ItemStackSnapshot? source = inventory.GetReservedStacksAt(
                job.Id,
                command.UnitId.ItemId,
                stock)
            .FirstOrDefault();
        if (source == null)
        {
            return Result<RoomMaterialCommitResult>.Failure(
                RoomUpgradeExecutionErrors.StockReservationInvalid);
        }

        SkillGrantBundle skillBundle = CreateSkillBundle(
            job,
            command.UnitId,
            command.Tick);
        Result skillValidation = _skillGrants.Validate(skillBundle);
        if (skillValidation.IsFailure)
        {
            return Result<RoomMaterialCommitResult>.Failure(skillValidation.Error!);
        }

        if (room.Status == RoomImprovementStatus.ReadyForWork)
        {
            Result started = rooms.StartImprovementWork(
                room.RoomInfrastructureId,
                job.Id,
                command.Tick);
            if (started.IsFailure)
            {
                return Result<RoomMaterialCommitResult>.Failure(started.Error!);
            }
        }

        Result consumed = inventory.ConsumeReserved(
            job.Id,
            source.StackId,
            quantity: 1,
            command.Tick);
        if (consumed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated room material reservation could not be consumed.");
        }

        Result<RoomMaterialCommitResult> committed = rooms.CommitMaterialUnit(
            room.RoomInfrastructureId,
            job.Id,
            command.UnitId,
            command.Tick);
        if (committed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated room material unit could not be committed.");
        }

        ApplyConfirmedSkill(skillBundle);
        if (committed.Value.ImprovementCompleted)
        {
            Result advanced = jobs.AdvanceStage(job.Id, command.Tick);
            if (advanced.IsFailure)
            {
                throw new InvalidOperationException(
                    "Completed room upgrade could not enter finalization.");
            }
        }

        SaveAndPublish(rooms, inventory, jobs);
        return committed;
    }

    private static SkillGrantBundle CreateSkillBundle(
        JobSnapshot job,
        RoomMaterialUnitId unitId,
        long tick)
    {
        AgentSkillId skill = unitId.ItemId == RoomUpgradeMaterialIds.Stone
            ? AgentSkillCatalog.Stonework
            : unitId.ItemId == RoomUpgradeMaterialIds.MushroomLeg
                ? AgentSkillCatalog.Woodworking
                : unitId.ItemId == RoomUpgradeMaterialIds.Iron
                    ? AgentSkillCatalog.Metallurgy
                    : unitId.ItemId == RoomUpgradeMaterialIds.Crystal
                        ? AgentSkillCatalog.Alchemy
                        : throw new InvalidOperationException(
                            "Unknown room upgrade material skill mapping.");
        return new SkillGrantBundle(
            job.AssignedAgentId!.Value,
            SkillGrantSourceKind.JobCompleted,
            $"room-upgrade:{((RoomUpgradeWorkJobDefinition)job.Definition).RoomInfrastructureId}:{unitId}",
            tick,
            new[] { new SkillGrant(skill, SkillGrantUnits) });
    }

    private void ApplyConfirmedSkill(SkillGrantBundle bundle)
    {
        Result<SkillRedistributionReport> applied =
            _skillGrants.ApplyConfirmed(bundle);
        if (applied.IsFailure)
        {
            throw new InvalidOperationException(
                $"Room material skill grant failed: {applied.Error}");
        }
    }

    private void SaveAndPublish(
        RoomInfrastructureState rooms,
        InventoryState inventory,
        JobSystem jobs)
    {
        _rooms.Save(rooms);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(rooms.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CompleteRoomUpgradeWorkHandler
    : ICommandHandler<CompleteRoomUpgradeWorkCommand, Result>
{
    private readonly IRoomInfrastructureRepository _rooms;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CompleteRoomUpgradeWorkHandler(
        IRoomInfrastructureRepository rooms,
        IJobRepository jobs,
        IEventSink events)
    {
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(CompleteRoomUpgradeWorkCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState rooms = _rooms.Get();
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not RoomUpgradeWorkJobDefinition work)
        {
            return Result.Failure(RoomUpgradeExecutionErrors.JobMismatch);
        }

        RoomInfrastructureProjectSnapshot? room = rooms.Get(
            work.RoomInfrastructureId);
        if (room?.Status != RoomImprovementStatus.Improved
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(RoomUpgradeExecutionErrors.InvalidStage);
        }

        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsSuccess)
        {
            _jobs.Save(jobs);
            _events.Append(jobs.DequeueUncommittedEvents());
        }

        return completed;
    }
}

}
