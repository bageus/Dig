using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Buildings
{

public enum BuildingBoxPackingExecutionStepKind
{
    None = 0,
    StartJob = 1,
    AdvanceStage = 2,
    AddWork = 3,
    CompletePacking = 4,
}

public static class BuildingBoxPackingExecutionPolicy
{
    public static Result<BuildingBoxPackingExecutionStepKind> Evaluate(
        JobSnapshot? job,
        BuildingSnapshot? building,
        CellId workerCell)
    {
        if (!BuildingBoxPackingJobValidation.Matches(job, building))
        {
            return Result<BuildingBoxPackingExecutionStepKind>.Failure(
                BuildingBoxPackingErrors.JobTypeMismatch);
        }

        if (job!.IsTerminal
            || (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress))
        {
            return Success(BuildingBoxPackingExecutionStepKind.None);
        }

        BuildingPackingPlanSnapshot packing = building!.PackingPlan!;
        if (packing.CommitState != BuildingPackingCommitState.Active)
        {
            return Result<BuildingBoxPackingExecutionStepKind>.Failure(
                BuildingBoxPackingErrors.PackingPlanMissing);
        }

        BuildingBoxPackingJobDefinition definition =
            (BuildingBoxPackingJobDefinition)job.Definition;
        if (workerCell.X != definition.WorkPosition.X
            || workerCell.Y != definition.WorkPosition.Y)
        {
            return Success(BuildingBoxPackingExecutionStepKind.None);
        }

        if (job.Status == JobStatus.Claimed)
        {
            return Success(BuildingBoxPackingExecutionStepKind.StartJob);
        }

        return job.Stage switch
        {
            JobStageKind.TravelToDestination =>
                Success(BuildingBoxPackingExecutionStepKind.AdvanceStage),
            JobStageKind.PerformWork when packing.CompletedWork
                < building.Definition.BoxPolicy!.PackingWork =>
                Success(BuildingBoxPackingExecutionStepKind.AddWork),
            JobStageKind.PerformWork =>
                Success(BuildingBoxPackingExecutionStepKind.AdvanceStage),
            JobStageKind.Finalize =>
                Success(BuildingBoxPackingExecutionStepKind.CompletePacking),
            _ => Result<BuildingBoxPackingExecutionStepKind>.Failure(
                BuildingBoxPackingErrors.InvalidJobStage),
        };
    }

    private static Result<BuildingBoxPackingExecutionStepKind> Success(
        BuildingBoxPackingExecutionStepKind step)
    {
        return Result<BuildingBoxPackingExecutionStepKind>.Success(step);
    }
}
}
