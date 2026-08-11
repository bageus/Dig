using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result EnsureAutomaticFoodPlan(AgentState agent, long tick)
    {
        if (agent.CreateSnapshot(tick).ScheduledActivity == ScheduleActivity.Work
            || HasActiveResidentFoodJob(agent.Id))
        {
            return Result.Success();
        }

        ItemStackSnapshot? carried = FindCarriedFood(agent.Id);
        if (carried != null)
        {
            return UseResidentInventoryActionWithSlotGuard(
                agent.Id.ToString(),
                carried.StackId.ToString(),
                tick,
                directCommand: false);
        }

        AutomaticFoodSource? source = FindAutomaticFoodSource(agent);
        if (!source.HasValue)
        {
            return Result.Success();
        }

        Result released = ReleaseAssignmentsForAgents(
            new HashSet<EntityId> { agent.Id },
            tick);
        if (released.IsFailure)
        {
            return released;
        }

        if (source.Value.Package)
        {
            return StartAutomaticProductionPackageUse(
                source.Value.StackId,
                agent.Id,
                agent.Position,
                tick);
        }

        return CreateWorldItemPickup(
            source.Value.StackId.ToString(),
            agent.Id.ToString(),
            source.Value.Cell,
            tick,
            eatAfterPickup: true,
            automatic: true);
    }

    private bool HasAutomaticFoodSource(AgentSnapshot agent)
    {
        if (agent.ScheduledActivity == ScheduleActivity.Work)
        {
            return false;
        }

        AgentState? state = _productionAgents?.Get(agent.Id);
        return FindCarriedFood(agent.Id) != null
            || (state != null && FindAutomaticFoodSource(state).HasValue);
    }

    private ItemStackSnapshot? FindCarriedFood(EntityId agentId)
    {
        return LoadFoodStacks()
            .Where(value => value.Location.Kind == ItemLocationKind.AgentInventory
                && value.Location.HasOwner
                && value.Location.OwnerId == agentId
                && value.AvailableQuantity > 0)
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private AutomaticFoodSource? FindAutomaticFoodSource(AgentState agent)
    {
        if (!TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
        {
            return null;
        }

        AutomaticFoodSource? selected = null;
        foreach (ItemStackSnapshot stack in LoadFoodStacks()
            .Where(value => value.Location.Kind == ItemLocationKind.World
                && value.Location.HasCell
                && value.AvailableQuantity > 0))
        {
            if (!BuildingSupplyReachability.IsConnected(
                navigation,
                agent.Position,
                stack.Location.CellId))
            {
                continue;
            }

            AutomaticFoodSource candidate = new AutomaticFoodSource(
                stack.StackId,
                stack.Location.CellId,
                package: false);
            selected = SelectCloserFoodSource(agent.Position, selected, candidate);
        }

        if (selected.HasValue)
        {
            return selected;
        }

        foreach (ProductionOutputPackageSnapshot package in _productionRepository!.Get()
            .GetOutputPackages()
            .Where(value => value.Kind == ProductionOutputPackageKind.Food))
        {
            ItemStackSnapshot? stack = _buildingInventoryRepository!.Get()
                .GetStack(package.StackId);
            if (stack?.Location.Kind != ItemLocationKind.World
                || !stack.Location.HasCell
                || !CanDirectUseProductionPackage(
                    package.StackId,
                    agent.Position,
                    out _))
            {
                continue;
            }

            AutomaticFoodSource candidate = new AutomaticFoodSource(
                package.StackId,
                stack.Location.CellId,
                package: true);
            selected = SelectCloserFoodSource(agent.Position, selected, candidate);
        }

        return selected;
    }

    private IEnumerable<ItemStackSnapshot> LoadFoodStacks()
    {
        IEnumerable<ItemStackSnapshot> From(InMemoryInventoryRepository repository)
        {
            InventoryState inventory = repository.Get();
            return inventory.CreateSnapshot().Stacks.Where(stack =>
                inventory.Catalog.Get(stack.ItemId).FoodUse != null);
        }

        IEnumerable<ItemStackSnapshot> building = _buildingInventoryRepository == null
            ? Array.Empty<ItemStackSnapshot>()
            : From(_buildingInventoryRepository);
        return building.Concat(From(_inventoryRepository))
            .GroupBy(value => value.StackId)
            .Select(value => value.First());
    }

    private bool HasActiveResidentFoodJob(EntityId agentId)
    {
        return _jobRepository.Get().GetAll().Any(job =>
            IsActive(job)
            && job.AssignedAgentId == agentId
            && (job.Definition is ProductionPackageUseJobDefinition
                || (job.Definition is WorldItemPickupJobDefinition pickup
                    && pickup.CompletionAction
                        == WorldItemPickupCompletionAction.UseConsumable)));
    }


    private static AutomaticFoodSource SelectCloserFoodSource(
        CellId origin,
        AutomaticFoodSource? current,
        AutomaticFoodSource candidate)
    {
        int distance = Manhattan(origin, candidate.Cell);
        if (!current.HasValue)
        {
            return candidate;
        }

        int currentDistance = Manhattan(origin, current.Value.Cell);
        if (distance != currentDistance)
        {
            return distance < currentDistance ? candidate : current.Value;
        }

        return string.Compare(
            candidate.StackId.ToString(),
            current.Value.StackId.ToString(),
            StringComparison.Ordinal) < 0
                ? candidate
                : current.Value;
    }

    private static int Manhattan(CellId left, CellId right)
    {
        return Math.Abs(left.X - right.X)
            + Math.Abs(left.Y - right.Y)
            + Math.Abs(left.Z - right.Z);
    }


    private readonly struct AutomaticFoodSource
    {
        public AutomaticFoodSource(EntityId stackId, CellId cell, bool package)
        {
            StackId = stackId;
            Cell = cell;
            Package = package;
        }

        public EntityId StackId { get; }
        public CellId Cell { get; }
        public bool Package { get; }
    }
}

}
