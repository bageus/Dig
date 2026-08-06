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

public sealed class CompleteRoomUpgradeDeliveryHandler
    : ICommandHandler<CompleteRoomUpgradeDeliveryCommand, Result>
{
    private readonly IRoomInfrastructureRepository _rooms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;
    private readonly IAgentSkillGrantService _skillGrants;

    public CompleteRoomUpgradeDeliveryHandler(
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

    public Result Handle(CompleteRoomUpgradeDeliveryCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState rooms = _rooms.Get();
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not HaulJobDefinition hauling
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(RoomUpgradeExecutionErrors.JobMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem)
        {
            return Result.Failure(RoomUpgradeExecutionErrors.InvalidStage);
        }

        RoomInfrastructureProjectSnapshot? room = rooms.GetByActiveJob(job.Id);
        if (room == null
            || !room.TemporaryStockCell.HasValue
            || hauling.Destination != ItemLocation.InWorld(room.TemporaryStockCell.Value))
        {
            return Result.Failure(RoomUpgradeExecutionErrors.JobMismatch);
        }

        EntityId? workJobId = FindWorkJob(room, jobs);
        if (!workJobId.HasValue)
        {
            return Result.Failure(RoomUpgradeExecutionErrors.WorkJobMissing);
        }

        RoomMaterialLedgerSnapshot? material = room.Materials
            .FirstOrDefault(value => value.ItemId == hauling.ItemId);
        if (material == null
            || hauling.Quantity > material.Required - material.Delivered)
        {
            return Result.Failure(RoomInfrastructureErrors.DeliveryExceedsRequirement);
        }

        SkillGrantBundle skillBundle = CreateHaulingSkillBundle(
            job,
            hauling,
            command.Tick);
        Result skillValidation = _skillGrants.Validate(skillBundle);
        if (skillValidation.IsFailure)
        {
            return skillValidation;
        }

        Result moved = inventory.DepositReservedResidentItems(
            job.Id,
            job.AssignedAgentId.Value,
            hauling.ItemId,
            hauling.Quantity,
            hauling.Destination,
            command.DepositedStackId,
            command.Tick);
        if (moved.IsFailure && moved.Error == InventoryErrors.ReservationNotFound)
        {
            moved = inventory.MoveReserved(
                hauling.SourceStackId,
                job.Id,
                hauling.Quantity,
                hauling.Destination,
                command.DepositedStackId,
                command.Tick);
        }

        if (moved.IsFailure)
        {
            return moved;
        }

        EntityId depositedStack = ResolveDepositedStack(
            inventory,
            hauling,
            command.DepositedStackId);
        Result reserved = inventory.ReserveQuantity(
            depositedStack,
            workJobId.Value,
            hauling.Quantity,
            command.Tick);
        if (reserved.IsFailure)
        {
            throw new InvalidOperationException(
                "Completed room delivery could not reserve its deposited stock.");
        }

        Result recorded = rooms.RecordDelivery(
            room.RoomInfrastructureId,
            job.Id,
            hauling.ItemId,
            hauling.Quantity,
            command.Tick);
        if (recorded.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated room delivery could not update its material ledger.");
        }

        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated room delivery could not complete its job lifecycle.");
        }

        inventory.ReleaseResidentSlotClaims(job.Id, command.Tick);
        RoomInfrastructureProjectSnapshot updated = rooms.Get(
            room.RoomInfrastructureId)!;
        JobSnapshot work = jobs.Get(workJobId.Value)!;
        if (updated.Status == RoomImprovementStatus.ReadyForWork
            && work.Status == JobStatus.Created)
        {
            Result available = jobs.MakeAvailable(work.Id, command.Tick);
            if (available.IsFailure)
            {
                throw new InvalidOperationException(
                    "Ready room upgrade work could not become available.");
            }
        }

        ApplyConfirmedSkill(skillBundle);
        SaveAndPublish(rooms, inventory, jobs);
        return Result.Success();
    }

    private static EntityId? FindWorkJob(
        RoomInfrastructureProjectSnapshot room,
        JobSystem jobs)
    {
        EntityId[] ids = room.ActiveJobIds
            .Where(id => jobs.Get(id)?.Definition is RoomUpgradeWorkJobDefinition)
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        return ids.Length == 1 ? ids[0] : (EntityId?)null;
    }

    private static EntityId ResolveDepositedStack(
        InventoryState inventory,
        HaulJobDefinition hauling,
        EntityId requestedStackId)
    {
        ItemStackSnapshot? requested = requestedStackId.IsEmpty
            ? null
            : inventory.GetStack(requestedStackId);
        if (Matches(requested, hauling))
        {
            return requested!.StackId;
        }

        ItemStackSnapshot? source = inventory.GetStack(hauling.SourceStackId);
        if (Matches(source, hauling))
        {
            return source!.StackId;
        }

        throw new InvalidOperationException(
            "Completed room delivery did not produce an identifiable destination stack.");
    }

    private static bool Matches(
        ItemStackSnapshot? stack,
        HaulJobDefinition hauling)
    {
        return stack != null
            && stack.ItemId == hauling.ItemId
            && stack.Location == hauling.Destination
            && stack.AvailableQuantity >= hauling.Quantity;
    }

    private static SkillGrantBundle CreateHaulingSkillBundle(
        JobSnapshot job,
        HaulJobDefinition hauling,
        long tick)
    {
        return new SkillGrantBundle(
            job.AssignedAgentId!.Value,
            SkillGrantSourceKind.JobCompleted,
            job.Id.ToString(),
            tick,
            hauling.SkillGrantProfile.Multiply(1));
    }

    private void ApplyConfirmedSkill(SkillGrantBundle bundle)
    {
        Result<SkillRedistributionReport> applied =
            _skillGrants.ApplyConfirmed(bundle);
        if (applied.IsFailure)
        {
            throw new InvalidOperationException(
                $"Completed room delivery skill grant failed: {applied.Error}");
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

}
