using Dig.Application.Ecology;
using Dig.Application.Inventory;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal Result InterruptResidentForCombat(AgentState resident, long tick)
    {
        if (resident == null)
        {
            throw new System.ArgumentNullException(nameof(resident));
        }

        JobSystem jobs = _jobRepository.Get();
        InventoryState terrainInventory = _inventoryRepository.Get();

        if (_residentFacilities != null)
        {
            BuildingFacilitiesState facilities = _residentFacilities.Get();
            facilities.ReleaseForAgent(resident.Id, tick);
            _residentFacilities.Save(facilities);
            PublishFacilityEvents(facilities);
        }

        if (resident.HasActiveFoodMeal)
        {
            Result meal = resident.InterruptFoodMeal("combat_preempted", tick);
            if (meal.IsFailure)
            {
                return meal;
            }
        }

        Result action = resident.InterruptActiveAction("combat_preempted", tick);
        if (action.IsFailure)
        {
            return action;
        }

        CancelManualQuarterExcavation(resident.Id.ToString());
        JobSnapshot[] assigned = CollectAssignedActiveJobs(jobs, resident.Id);
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
                    InterruptProductionForDirectCommand(job, production, tick),
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

        _inventoryRepository.Save(terrainInventory);
        _jobRepository.Save(jobs);
        _productionAgents!.Save(resident);
        _journal.Append(resident.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}