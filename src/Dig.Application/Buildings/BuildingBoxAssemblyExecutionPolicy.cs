using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Buildings
{

public enum BuildingBoxAssemblyExecutionStepKind
{
    None = 0,
    StartJob = 1,
    AcquireBox = 2,
    AdvanceStage = 3,
    CommitBoxToSite = 4,
    AddWork = 5,
    CompleteAssembly = 6,
}

public static class BuildingBoxAssemblyExecutionPolicy
{
    public static Result<BuildingBoxAssemblyExecutionStepKind> Evaluate(
        JobSnapshot? job,
        BuildingSnapshot? building,
        ItemStackSnapshot? sourceBox,
        CellId workerCell)
    {
        if (!BuildingBoxJobValidation.Matches(job, building))
        {
            return Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        if (job!.IsTerminal
            || (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress))
        {
            return Success(BuildingBoxAssemblyExecutionStepKind.None);
        }

        if (!job.AssignedAgentId.HasValue || sourceBox is null)
        {
            return Failure(BuildingBoxErrors.SourceStackMissing);
        }

        BuildingBoxAssemblyJobDefinition definition =
            (BuildingBoxAssemblyJobDefinition)job.Definition;
        EntityId workerId = job.AssignedAgentId.Value;
        bool carriedByWorker = sourceBox.Location == ItemLocation.InAgent(workerId);
        bool atWorldSource = sourceBox.Location.Kind == ItemLocationKind.World
            && sourceBox.Location.HasCell
            && sourceBox.Location.CellId == workerCell;
        bool atWork = workerCell == definition.WorkPosition;

        if (job.Status == JobStatus.Claimed)
        {
            return carriedByWorker || atWorldSource
                ? Success(BuildingBoxAssemblyExecutionStepKind.StartJob)
                : Success(BuildingBoxAssemblyExecutionStepKind.None);
        }

        BuildingBoxPlanSnapshot plan = building!.BoxPlan!;
        return job.Stage switch
        {
            JobStageKind.AcquireItem when carriedByWorker =>
                Success(BuildingBoxAssemblyExecutionStepKind.AdvanceStage),
            JobStageKind.AcquireItem when atWorldSource =>
                Success(BuildingBoxAssemblyExecutionStepKind.AcquireBox),
            JobStageKind.AcquireItem =>
                Success(BuildingBoxAssemblyExecutionStepKind.None),
            JobStageKind.TravelToDestination when atWork =>
                Success(BuildingBoxAssemblyExecutionStepKind.AdvanceStage),
            JobStageKind.TravelToDestination =>
                Success(BuildingBoxAssemblyExecutionStepKind.None),
            JobStageKind.DepositItem
                when plan.CommitState == BuildingBoxCommitState.Reserved
                    && carriedByWorker
                    && atWork =>
                Success(BuildingBoxAssemblyExecutionStepKind.CommitBoxToSite),
            JobStageKind.DepositItem
                when plan.CommitState == BuildingBoxCommitState.AtSite =>
                Success(BuildingBoxAssemblyExecutionStepKind.AdvanceStage),
            JobStageKind.DepositItem =>
                Success(BuildingBoxAssemblyExecutionStepKind.None),
            JobStageKind.PerformWork
                when plan.CommitState == BuildingBoxCommitState.AtSite
                    && atWork
                    && building.CompletedWork < building.Definition.RequiredWork =>
                Success(BuildingBoxAssemblyExecutionStepKind.AddWork),
            JobStageKind.PerformWork
                when plan.CommitState == BuildingBoxCommitState.AtSite && atWork =>
                Success(BuildingBoxAssemblyExecutionStepKind.AdvanceStage),
            JobStageKind.PerformWork =>
                Success(BuildingBoxAssemblyExecutionStepKind.None),
            JobStageKind.Finalize
                when plan.CommitState == BuildingBoxCommitState.AtSite && atWork =>
                Success(BuildingBoxAssemblyExecutionStepKind.CompleteAssembly),
            JobStageKind.Finalize =>
                Success(BuildingBoxAssemblyExecutionStepKind.None),
            _ => Failure(BuildingBoxErrors.InvalidJobStage),
        };
    }

    private static Result<BuildingBoxAssemblyExecutionStepKind> Success(
        BuildingBoxAssemblyExecutionStepKind step)
    {
        return Result<BuildingBoxAssemblyExecutionStepKind>.Success(step);
    }

    private static Result<BuildingBoxAssemblyExecutionStepKind> Failure(DomainError error)
    {
        return Result<BuildingBoxAssemblyExecutionStepKind>.Failure(error);
    }
}
}
