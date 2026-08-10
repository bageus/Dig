using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private Result CancelBuildingBoxForDirectCommand(JobSnapshot job, long tick)
        {
            return job.Definition switch
            {
                BuildingBoxAssemblyJobDefinition assembly =>
                    CancelBuildingBoxAssemblyForDirectCommand(job, assembly, tick),
                BuildingBoxPickupJobDefinition relocation when relocation.IsRelocation =>
                    CancelBuildingBoxRelocationForDirectCommand(job, tick),
                _ => Result.Success(),
            };
        }

        private Result CancelBuildingBoxAssemblyForDirectCommand(
            JobSnapshot job,
            BuildingBoxAssemblyJobDefinition assembly,
            long tick)
        {
            if (_buildingsRepository == null || _buildingInventoryRepository == null)
            {
                return Result.Failure(BuildingsNotInitialized);
            }

            BuildingSnapshot? building = _buildingsRepository.Get().Get(assembly.BuildingId);
            if (building?.BoxPlan == null)
            {
                return Result.Success();
            }

            if (building.BoxPlan.CommitState == BuildingBoxCommitState.AtSite)
            {
                PackableBuildingExecutionState? execution =
                    _packableBuildingExecutions?.Get(job.Id);
                if (execution != null)
                {
                    Result interrupted = _packableBuildingExecutions!.Interrupt(job.Id);
                    if (interrupted.IsFailure)
                    {
                        return interrupted;
                    }
                }

                return ReleaseDigWorkForDirectCommand(job, tick);
            }

            CancelBuildingBoxPlanHandler cancel = new CancelBuildingBoxPlanHandler(
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                _worldSession.Journal);
            Result result = cancel.Handle(new CancelBuildingBoxPlanCommand(
                assembly.BuildingId,
                "building_box_direct_movement_replaced",
                tick));
            if (result.IsSuccess)
            {
                PackableBuildingExecutionState? execution =
                    _packableBuildingExecutions?.Get(job.Id);
                if (execution != null)
                {
                    _packableBuildingExecutions!.Cancel(job.Id);
                }

                _buildingBoxAssemblyRoutes.Remove(job.Id);
            }

            return result;
        }

        private Result CancelBuildingBoxRelocationForDirectCommand(
            JobSnapshot job,
            long tick)
        {
            if (_buildingInventoryRepository == null)
            {
                return Result.Failure(BuildingsNotInitialized);
            }

            JobSystem jobs = _jobRepository.Get();
            InventoryState inventory = _buildingInventoryRepository.Get();
            Result cancelled = jobs.Cancel(
                job.Id,
                new JobBlockReason(
                    "building_box_direct_movement_replaced",
                    "BuildingBox relocation was cancelled by a direct resident movement command."),
                tick);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            inventory.ReleaseReservations(job.Id, tick);
            _buildingInventoryRepository.Save(inventory);
            _jobRepository.Save(jobs);
            _worldSession.Journal.Append(inventory.DequeueUncommittedEvents());
            _worldSession.Journal.Append(jobs.DequeueUncommittedEvents());
            _buildingBoxPickupRoutes.Remove(job.Id);
            return Result.Success();
        }
    }
}
