using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Presentation.Buildings;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal IReadOnlyList<BuildingBoxRelocationPlanViewModel>
            LoadBuildingBoxRelocationPlans()
        {
            if (_buildingInventoryRepository == null)
            {
                return Array.Empty<BuildingBoxRelocationPlanViewModel>();
            }

            InventoryState inventory = _buildingInventoryRepository.Get();
            return _jobRepository.Get().GetAll()
                .Where(job => !job.IsTerminal
                    && job.Definition is BuildingBoxPickupJobDefinition relocation
                    && relocation.IsRelocation)
                .Select(job => CreateRelocationPlan(job, inventory))
                .Where(plan => plan != null)
                .Select(plan => plan!)
                .OrderBy(plan => plan.JobId.ToString(), StringComparer.Ordinal)
                .ToArray();
        }

        private static BuildingBoxRelocationPlanViewModel? CreateRelocationPlan(
            JobSnapshot job,
            InventoryState inventory)
        {
            BuildingBoxPickupJobDefinition relocation =
                (BuildingBoxPickupJobDefinition)job.Definition;
            ItemStackSnapshot? stack = inventory.GetStack(relocation.StackId);
            return stack == null || !relocation.DestinationCell.HasValue
                ? null
                : new BuildingBoxRelocationPlanViewModel(
                    job.Id,
                    stack.StackId,
                    stack.ItemId,
                    relocation.DestinationCell.Value);
        }
    }
}
