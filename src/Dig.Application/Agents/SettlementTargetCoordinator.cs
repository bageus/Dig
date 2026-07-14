using Dig.Application.Inventory;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Agents;

internal sealed class SettlementTargetCoordinator
{
    private static readonly ItemCategoryId FoodCategory = new ItemCategoryId("food");
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IBuildingFacilitiesRepository _facilitiesRepository;

    public SettlementTargetCoordinator(
        IInventoryRepository inventoryRepository,
        IBuildingFacilitiesRepository facilitiesRepository)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _facilitiesRepository = facilitiesRepository
            ?? throw new ArgumentNullException(nameof(facilitiesRepository));
    }

    public AgentDecisionContext CreateContext(
        AgentSnapshot agent,
        AgentDecisionContext external)
    {
        if (agent is null || external is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        InventoryState inventory = _inventoryRepository.Get();
        BuildingFacilitiesState facilities = _facilitiesRepository.Get();
        return new AgentDecisionContext(
            foodAvailable: HasFood(inventory, agent.Id),
            bedAvailable: facilities.FindAvailable(
                BuildingFacilityKind.Bed,
                agent.Id).Count > 0,
            workAvailable: external.WorkAvailable,
            restAvailable: facilities.FindAvailable(
                BuildingFacilityKind.Leisure,
                agent.Id).Count > 0,
            escapeRouteAvailable: external.EscapeRouteAvailable,
            threatLevel: external.ThreatLevel);
    }

    public string? ValidateExistingTarget(AgentState agent, long tick)
    {
        AgentActionSnapshot? action = agent.CreateSnapshot(tick).ActiveAction;
        if (!action.HasValue || !action.Value.Target.HasValue)
        {
            return null;
        }

        AgentActivityTarget target = action.Value.Target.Value;
        if (IsValidReservation(agent.Id, target))
        {
            return null;
        }

        Release(agent.Id, tick);
        Require(agent.BlockTargetedAction("target_reservation_missing", tick));
        return "target_reservation_missing";
    }

    public Result<AgentActivityTarget> Acquire(
        EntityId agentId,
        AgentIntentKind intentKind,
        long tick)
    {
        Release(agentId, tick);
        return intentKind switch
        {
            AgentIntentKind.Eat => AcquireFood(agentId, tick),
            AgentIntentKind.Sleep => AcquireFacility(
                agentId,
                BuildingFacilityKind.Bed,
                AgentActivityTargetKind.Bed,
                tick),
            AgentIntentKind.Rest => AcquireFacility(
                agentId,
                BuildingFacilityKind.Leisure,
                AgentActivityTargetKind.Leisure,
                tick),
            _ => throw new ArgumentOutOfRangeException(nameof(intentKind)),
        };
    }

    public Result Complete(EntityId agentId, AgentActivityTarget target, long tick)
    {
        if (!IsValidReservation(agentId, target))
        {
            return Result.Failure(AgentSettlementErrors.TargetReservationMissing);
        }

        if (target.Kind == AgentActivityTargetKind.Food)
        {
            InventoryState inventory = _inventoryRepository.Get();
            Result consumed = inventory.ConsumeReserved(
                agentId,
                target.EntityId,
                quantity: 1,
                tick);
            _inventoryRepository.Save(inventory);
            return consumed;
        }

        BuildingFacilitiesState facilities = _facilitiesRepository.Get();
        facilities.ReleaseForAgent(agentId, tick);
        _facilitiesRepository.Save(facilities);
        return Result.Success();
    }

    public void Release(EntityId agentId, long tick)
    {
        InventoryState inventory = _inventoryRepository.Get();
        BuildingFacilitiesState facilities = _facilitiesRepository.Get();
        inventory.ReleaseReservations(agentId, tick);
        facilities.ReleaseForAgent(agentId, tick);
        _inventoryRepository.Save(inventory);
        _facilitiesRepository.Save(facilities);
    }

    public bool IsValidReservation(EntityId agentId, AgentActivityTarget target)
    {
        if (target.Kind == AgentActivityTargetKind.Food)
        {
            ItemStackSnapshot? stack = _inventoryRepository.Get().GetStack(target.EntityId);
            return stack is not null
                && stack.Reservations.Any(value => value.JobId == agentId && value.Quantity >= 1);
        }

        BuildingFacilitySnapshot? facility = _facilitiesRepository.Get().Get(target.EntityId);
        BuildingFacilityKind expected = target.Kind == AgentActivityTargetKind.Bed
            ? BuildingFacilityKind.Bed
            : BuildingFacilityKind.Leisure;
        return facility is not null
            && facility.Definition.Kind == expected
            && facility.ReservedAgentId == agentId;
    }

    public string? GetUnavailableNeedReason(
        AgentSnapshot agent,
        AgentDecisionContext context,
        AgentBehaviorPolicy policy)
    {
        if (agent.Needs.Nutrition.IsAtOrBelow(policy.Needs.CriticalThreshold)
            && !context.FoodAvailable)
        {
            return "food_unavailable";
        }

        if (agent.Needs.Alertness.IsAtOrBelow(policy.Needs.CriticalThreshold)
            && !context.BedAvailable)
        {
            return "bed_unavailable";
        }

        if (agent.ScheduledActivity == ScheduleActivity.Rest && !context.RestAvailable)
        {
            return "leisure_unavailable";
        }

        return null;
    }

    private static bool HasFood(InventoryState inventory, EntityId agentId)
    {
        return inventory.CreateSnapshot().Stacks.Any(value =>
            inventory.Catalog.Get(value.ItemId).HasCategory(FoodCategory)
            && (value.AvailableQuantity > 0
                || value.Reservations.Any(reservation =>
                    reservation.JobId == agentId && reservation.Quantity > 0)));
    }

    private Result<AgentActivityTarget> AcquireFood(EntityId agentId, long tick)
    {
        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? stack = inventory.CreateSnapshot().Stacks
            .Where(value => value.AvailableQuantity > 0)
            .Where(value => inventory.Catalog.Get(value.ItemId).HasCategory(FoodCategory))
            .OrderBy(value => value.Location)
            .ThenBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (stack is null)
        {
            return Result<AgentActivityTarget>.Failure(AgentSettlementErrors.FoodUnavailable);
        }

        Result reserved = inventory.ReserveQuantity(
            stack.StackId,
            agentId,
            quantity: 1,
            tick);
        if (reserved.IsFailure)
        {
            return Result<AgentActivityTarget>.Failure(reserved.Error!);
        }

        _inventoryRepository.Save(inventory);
        return Result<AgentActivityTarget>.Success(new AgentActivityTarget(
            AgentActivityTargetKind.Food,
            stack.StackId));
    }

    private Result<AgentActivityTarget> AcquireFacility(
        EntityId agentId,
        BuildingFacilityKind facilityKind,
        AgentActivityTargetKind targetKind,
        long tick)
    {
        BuildingFacilitiesState facilities = _facilitiesRepository.Get();
        BuildingFacilitySnapshot? facility = facilities.FindAvailable(facilityKind, agentId)
            .FirstOrDefault();
        if (facility is null)
        {
            DomainError error = facilityKind == BuildingFacilityKind.Bed
                ? AgentSettlementErrors.BedUnavailable
                : AgentSettlementErrors.LeisureUnavailable;
            return Result<AgentActivityTarget>.Failure(error);
        }

        Result reserved = facilities.Reserve(facility.Definition.Id, agentId, tick);
        if (reserved.IsFailure)
        {
            return Result<AgentActivityTarget>.Failure(reserved.Error!);
        }

        _facilitiesRepository.Save(facilities);
        return Result<AgentActivityTarget>.Success(new AgentActivityTarget(
            targetKind,
            facility.Definition.Id));
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}

public static class AgentSettlementErrors
{
    public static readonly DomainError FoodUnavailable = new DomainError(
        "agents.settlement.food_unavailable",
        "No unreserved food portion is available.");

    public static readonly DomainError BedUnavailable = new DomainError(
        "agents.settlement.bed_unavailable",
        "No unreserved bed is available.");

    public static readonly DomainError LeisureUnavailable = new DomainError(
        "agents.settlement.leisure_unavailable",
        "No unreserved leisure facility is available.");

    public static readonly DomainError TargetReservationMissing = new DomainError(
        "agents.settlement.target_reservation_missing",
        "The resident activity target reservation disappeared.");
}
