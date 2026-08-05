using System;
using System.Collections.Generic;
using Dig.Application.Ecology;
using Dig.Application.WorldObjects;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private Func<EntityId, long, Result>? _disengageResidentCombat;
        private Func<EntityId, bool>? _cancelResidentManualMovement;

        internal void BindDirectCommandCombatDisengage(
            Func<EntityId, long, Result> disengage)
        {
            _disengageResidentCombat = disengage
                ?? throw new ArgumentNullException(nameof(disengage));
        }

        internal void BindDirectCommandManualMovementCancellation(
            Func<EntityId, bool> cancelManualMovement)
        {
            _cancelResidentManualMovement = cancelManualMovement
                ?? throw new ArgumentNullException(nameof(cancelManualMovement));
        }

        internal Result PrepareResidentsForDirectCommand(
            IReadOnlyList<string> residentIds,
            long tick)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            JobSystem jobs = _jobRepository.Get();
            for (int residentIndex = 0; residentIndex < residentIds.Count; residentIndex++)
            {
                EntityId residentId = EntityId.Parse(residentIds[residentIndex]);
                Result disengaged = _disengageResidentCombat == null
                    ? Result.Success()
                    : _disengageResidentCombat(residentId, tick);
                if (disengaged.IsFailure)
                {
                    return disengaged;
                }

                _cancelResidentManualMovement?.Invoke(residentId);

                Result interrupted = InterruptFoodMealForDirectCommand(residentId, tick);
                if (interrupted.IsFailure)
                {
                    return interrupted;
                }

                CancelManualQuarterExcavation(residentId.ToString());
                JobSnapshot[] assigned = CollectAssignedActiveJobs(jobs, residentId);
                for (int jobIndex = 0; jobIndex < assigned.Length; jobIndex++)
                {
                    JobSnapshot job = assigned[jobIndex];
                    Result released = job.Definition switch
                    {
                        WorldItemPickupJobDefinition =>
                            CancelPickupForDirectCommand(jobs, job, tick),
                        MushroomChopJobDefinition =>
                            CancelMushroomForDirectCommand(job, tick),
                        BarrelAttackJobDefinition =>
                            CancelBarrelForDirectCommand(job, tick),
                        ProductionPackageUseJobDefinition =>
                            CancelProductionPackageUseForDirectCommand(job, tick),
                        ProductionWorkJobDefinition production =>
                            InterruptProductionForDirectCommand(
                                job,
                                production,
                                residentId,
                                tick),
                        BuildingSupplyJobDefinition =>
                            CancelBuildingSupplyForDirectCommand(
                                job,
                                residentId,
                                tick),
                        BuildingBoxAssemblyJobDefinition =>
                            CancelBuildingBoxForDirectCommand(job, tick),
                        BuildingBoxPickupJobDefinition relocation when relocation.IsRelocation =>
                            CancelBuildingBoxForDirectCommand(job, tick),
                        _ => ReleaseDigWorkForDirectCommand(job, tick),
                    };
                    if (released.IsFailure)
                    {
                        return released;
                    }

                    RemoveAllRoutePlans(job.Id);
                }
            }

            _jobRepository.Save(jobs);
            return Result.Success();
        }

        private Result InterruptFoodMealForDirectCommand(EntityId residentId, long tick)
        {
            AgentState? resident = _productionAgents?.Get(residentId);
            if (resident == null || !resident.HasActiveFoodMeal)
            {
                return Result.Success();
            }

            Result interrupted = resident.InterruptFoodMeal(
                "direct_command_replaced",
                tick);
            if (interrupted.IsFailure)
            {
                return interrupted;
            }

            _productionAgents!.Save(resident);
            _journal.Append(resident.DequeueUncommittedEvents());
            return Result.Success();
        }

        private static JobSnapshot[] CollectAssignedActiveJobs(
            JobSystem jobs,
            EntityId residentId)
        {
            List<JobSnapshot> assigned = new List<JobSnapshot>();
            foreach (JobSnapshot job in jobs.GetAll())
            {
                if (!job.IsTerminal
                    && job.AssignedAgentId == residentId)
                {
                    assigned.Add(job);
                }
            }

            return assigned.ToArray();
        }

        private Result CancelPickupForDirectCommand(
            JobSystem jobs,
            JobSnapshot job,
            long tick)
        {
            if (job.Definition is not WorldItemPickupJobDefinition pickup)
            {
                return Result.Failure(WorldItemPickupErrors.JobTypeMismatch);
            }

            InMemoryInventoryRepository? repository = ResolveWorldItemRepository(
                pickup.StackId);
            if (repository == null)
            {
                return Result.Failure(WorldItemPickupErrors.StackMissing);
            }

            Result cancelled = jobs.Cancel(
                job.Id,
                new JobBlockReason(
                    "world_item_pickup_direct_command",
                    "Pickup was cancelled by a direct resident command."),
                tick);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            InventoryState inventory = repository.Get();
            inventory.ReleaseReservations(job.Id, tick);
            repository.Save(inventory);
            _journal.Append(inventory.DequeueUncommittedEvents());
            return Result.Success();
        }

        private Result CancelMushroomForDirectCommand(JobSnapshot job, long tick)
        {
            return _cancelMushroomChop == null
                ? Result.Success()
                : _cancelMushroomChop.Handle(new CancelMushroomChopCommand(
                    job.Id,
                    "mushroom_direct_command_replaced",
                    tick));
        }

        private Result CancelBarrelForDirectCommand(JobSnapshot job, long tick)
        {
            return _cancelBarrelAttack == null
                ? Result.Success()
                : _cancelBarrelAttack.Handle(new CancelBarrelAttackCommand(
                    job.Id,
                    "barrel_direct_command_replaced",
                    tick));
        }

        private Result InterruptProductionForDirectCommand(
            JobSnapshot job,
            ProductionWorkJobDefinition production,
            EntityId residentId,
            long tick)
        {
            return _interruptProduction == null
                ? Result.Success()
                : _interruptProduction.Handle(new InterruptProductionOrderCommand(
                    production.OrderId,
                    job.Id,
                    "production_worker_forced_move",
                    tick,
                    ResolveResidentRecoveryCell(residentId)));
        }

        private Result CancelBuildingSupplyForDirectCommand(
            JobSnapshot job,
            EntityId residentId,
            long tick)
        {
            return _cancelBuildingSupply == null
                ? Result.Success()
                : _cancelBuildingSupply.Handle(new CancelBuildingSupplyCommand(
                    job.Id,
                    "building_supply_direct_command_replaced",
                    tick,
                    ResolveResidentRecoveryCell(residentId)));
        }

        private CellId? ResolveResidentRecoveryCell(EntityId residentId)
        {
            return _productionAgents?.Get(residentId)?.Position;
        }

        private Result CancelProductionPackageUseForDirectCommand(
            JobSnapshot job,
            long tick)
        {
            return _cancelProductionPackageUse == null
                ? Result.Success()
                : _cancelProductionPackageUse.Handle(
                    new CancelProductionPackageUseCommand(
                        job.Id,
                        "production_package_use_direct_command_replaced",
                        tick));
        }

        private Result ReleaseDigWorkForDirectCommand(JobSnapshot job, long tick)
        {
            if (_releaseAssignment == null)
            {
                return Result.Success();
            }

            return _releaseAssignment.Handle(
                new ReleaseJobAssignmentCommand(job.Id, tick));
        }
    }
}
