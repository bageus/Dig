using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result<PackableBuildingExecutionRegistry>
        RestorePackableBuildingExecutions(
            PackableBuildingExecutionsSaveData? data,
            JobSystem jobs,
            BuildingsState buildings)
    {
        Result<PackableBuildingExecutionRegistry> decoded =
            PackableBuildingExecutionSaveDataAdapter.Decode(
                data ?? new PackableBuildingExecutionsSaveData());
        if (decoded.IsFailure)
        {
            return decoded;
        }

        PackableBuildingExecutionRegistry registry = decoded.Value;
        foreach (PackableBuildingExecutionSnapshot execution in registry.CreateSnapshot())
        {
            BuildingSnapshot? building = buildings.Get(execution.PackageId);
            JobSnapshot? job = jobs.Get(execution.OperationId);
            if (building == null
                || job == null
                || building.Definition.Id != execution.DefinitionId
                || !MatchesAuthoritativeProgress(execution, building, job))
            {
                return Result<PackableBuildingExecutionRegistry>.Failure(
                    SaveErrors.InvalidDocument);
            }

            Result recovered = RecoverWorker(registry, execution, job);
            if (recovered.IsFailure)
            {
                return Result<PackableBuildingExecutionRegistry>.Failure(
                    SaveErrors.InvalidDocument);
            }
        }

        return Result<PackableBuildingExecutionRegistry>.Success(registry);
    }

    private static bool MatchesAuthoritativeProgress(
        PackableBuildingExecutionSnapshot execution,
        BuildingSnapshot building,
        JobSnapshot job)
    {
        if (execution.Operation == PackableBuildingOperationKind.Unpack)
        {
            return job.Definition is BuildingBoxAssemblyJobDefinition assembly
                && assembly.BuildingId == execution.PackageId
                && building.BoxPlan?.JobId == execution.OperationId
                && building.CompletedWork == execution.CompletedIterations;
        }

        return execution.Operation == PackableBuildingOperationKind.Pack
            && job.Definition is BuildingBoxPackingJobDefinition packing
            && packing.BuildingId == execution.PackageId
            && building.PackingPlan?.JobId == execution.OperationId
            && building.PackingPlan.CompletedWork == execution.CompletedIterations;
    }

    private static Result RecoverWorker(
        PackableBuildingExecutionRegistry registry,
        PackableBuildingExecutionSnapshot execution,
        JobSnapshot job)
    {
        if (execution.Status != PackableBuildingExecutionStatus.Active)
        {
            return Result.Success();
        }

        if (job.Status == JobStatus.Cancelled || job.Status == JobStatus.Failed)
        {
            return registry.Cancel(execution.OperationId);
        }

        bool jobIsActive = job.Status == JobStatus.Claimed
            || job.Status == JobStatus.InProgress;
        if (!jobIsActive || !job.AssignedAgentId.HasValue)
        {
            return registry.Interrupt(execution.OperationId);
        }

        if (job.AssignedAgentId.Value == execution.ActiveWorkerId)
        {
            return Result.Success();
        }

        Result interrupted = registry.Interrupt(execution.OperationId);
        return interrupted.IsFailure
            ? interrupted
            : registry.StartOrResume(
                execution.OperationId,
                job.AssignedAgentId.Value);
    }
}

}