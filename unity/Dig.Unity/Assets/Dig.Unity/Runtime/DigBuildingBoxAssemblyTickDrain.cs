using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private const int MaximumImmediateBuildingBoxAssemblyStepsPerTick = 16;

        private static readonly DomainError BuildingBoxAssemblyDrainLimitExceeded =
            new DomainError(
                "building_box.assembly.drain_limit_exceeded",
                "BuildingBox assembly exceeded the bounded immediate transition limit.");

        private Result AdvanceBuildingBoxAssemblyJob(
            EntityId jobId,
            AgentViewModel agent,
            long tick)
        {
            CellId workerCell = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            for (int index = 0;
                index < MaximumImmediateBuildingBoxAssemblyStepsPerTick;
                index++)
            {
                JobSnapshot? currentJob = _jobRepository.Get().Get(jobId);
                if (currentJob == null || currentJob.IsTerminal)
                {
                    _buildingBoxAssemblyRoutes.Remove(jobId);
                    return Result.Success();
                }

                if (currentJob.Definition is not BuildingBoxAssemblyJobDefinition assembly)
                {
                    return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
                }

                BuildingSnapshot? building = _buildingsRepository!.Get().Get(
                    assembly.BuildingId);
                ItemStackSnapshot? sourceBox = _buildingInventoryRepository!.Get().GetStack(
                    assembly.SourceStackId);
                Result<BuildingBoxAssemblyExecutionStepKind> evaluated =
                    BuildingBoxAssemblyExecutionPolicy.Evaluate(
                        currentJob,
                        building,
                        sourceBox,
                        workerCell);
                if (evaluated.IsFailure)
                {
                    return Result.Failure(evaluated.Error!);
                }

                BuildingBoxAssemblyExecutionStepKind step = evaluated.Value;
                if (step == BuildingBoxAssemblyExecutionStepKind.None)
                {
                    return Result.Success();
                }

                int completedWorkBefore = building?.CompletedWork ?? 0;
                Result executed = ExecuteBuildingBoxAssemblyStep(
                    step,
                    assembly,
                    building!,
                    currentJob.AssignedAgentId!.Value,
                    workerCell,
                    tick);
                if (executed.IsFailure)
                {
                    return executed;
                }

                if (step == BuildingBoxAssemblyExecutionStepKind.CompleteAssembly)
                {
                    _buildingBoxAssemblyRoutes.Remove(jobId);
                    return Result.Success();
                }

                if (step == BuildingBoxAssemblyExecutionStepKind.AddWork)
                {
                    BuildingSnapshot? afterWork = _buildingsRepository.Get().Get(
                        assembly.BuildingId);
                    if (afterWork?.CompletedWork == completedWorkBefore)
                    {
                        // A non-demo duration may still be counting down. Leave the job
                        // active and continue on a later simulation tick without spinning.
                        return Result.Success();
                    }
                }
            }

            return Result.Failure(BuildingBoxAssemblyDrainLimitExceeded);
        }
    }
}
