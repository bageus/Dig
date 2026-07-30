using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public enum BuildingBoxRelocationExecutionStepKind
{
    None = 0,
    StartJob = 1,
    AcquireBox = 2,
    AdvanceStage = 3,
    CompleteRelocation = 4,
}

public static class BuildingBoxRelocationExecutionPolicy
{
    public static Result<BuildingBoxRelocationExecutionStepKind> Evaluate(
        JobSnapshot? job,
        ItemStackSnapshot? box,
        CellId workerCell)
    {
        if (job?.Definition is not BuildingBoxPickupJobDefinition relocation
            || !relocation.IsRelocation)
        {
            return Failure(BuildingBoxPickupErrors.JobTypeMismatch);
        }

        if (job.IsTerminal
            || (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress))
        {
            return Success(BuildingBoxRelocationExecutionStepKind.None);
        }

        if (!job.AssignedAgentId.HasValue || box is null)
        {
            return Failure(BuildingBoxPickupErrors.BoxUnavailable);
        }

        EntityId workerId = job.AssignedAgentId.Value;
        bool carriedByWorker = DropResidentInventoryStackHandler.IsOwnedByResident(
            box.Location,
            workerId);
        bool atWorldSource = box.Location.Kind == ItemLocationKind.World
            && box.Location.HasCell
            && box.Location.CellId == workerCell
            && workerCell == relocation.SourceCell;
        bool atDestination = relocation.DestinationCell.HasValue
            && IsDepositPosition(workerCell, relocation.DestinationCell.Value);

        if (job.Status == JobStatus.Claimed)
        {
            return carriedByWorker || atWorldSource
                ? Success(BuildingBoxRelocationExecutionStepKind.StartJob)
                : Success(BuildingBoxRelocationExecutionStepKind.None);
        }

        return job.Stage switch
        {
            JobStageKind.TravelToTarget when atWorldSource =>
                Success(BuildingBoxRelocationExecutionStepKind.AdvanceStage),
            JobStageKind.TravelToTarget =>
                Success(BuildingBoxRelocationExecutionStepKind.None),
            JobStageKind.AcquireItem when carriedByWorker =>
                Success(BuildingBoxRelocationExecutionStepKind.AdvanceStage),
            JobStageKind.AcquireItem when atWorldSource =>
                Success(BuildingBoxRelocationExecutionStepKind.AcquireBox),
            JobStageKind.AcquireItem =>
                Success(BuildingBoxRelocationExecutionStepKind.None),
            JobStageKind.TravelToDestination when atDestination =>
                Success(BuildingBoxRelocationExecutionStepKind.AdvanceStage),
            JobStageKind.TravelToDestination =>
                Success(BuildingBoxRelocationExecutionStepKind.None),
            JobStageKind.DepositItem when carriedByWorker && atDestination =>
                Success(BuildingBoxRelocationExecutionStepKind.CompleteRelocation),
            JobStageKind.DepositItem =>
                Success(BuildingBoxRelocationExecutionStepKind.None),
            _ => Failure(BuildingBoxPickupErrors.InvalidJobStage),
        };
    }

    public static bool IsDepositPosition(CellId workerCell, CellId destinationCell)
    {
        if (workerCell.Z != destinationCell.Z)
        {
            return false;
        }

        int distance = Math.Abs(workerCell.X - destinationCell.X)
            + Math.Abs(workerCell.Y - destinationCell.Y);
        return distance <= 1;
    }

    private static Result<BuildingBoxRelocationExecutionStepKind> Success(
        BuildingBoxRelocationExecutionStepKind step)
    {
        return Result<BuildingBoxRelocationExecutionStepKind>.Success(step);
    }

    private static Result<BuildingBoxRelocationExecutionStepKind> Failure(
        DomainError error)
    {
        return Result<BuildingBoxRelocationExecutionStepKind>.Failure(error);
    }
}

}
