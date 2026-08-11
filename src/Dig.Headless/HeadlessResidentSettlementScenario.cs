using System.Linq;
using System;
using Dig.Application.Agents;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless
{

internal static class HeadlessResidentSettlementScenario
{
    private const int MaximumSettlementTicks = 256;

    public static int Run(
        SimulationState state,
        InMemoryExecutionJournal journal,
        long startTick)
    {
        ItemId meal = new ItemId("headless.meal");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                meal,
                "Headless meal",
                maximumStackSize: 10,
                isTool: false,
                new[] { new ItemCategoryId("food") }),
        }));
        EntityId mealStack = Require(state.Entities.RegisterNew());
        Require(inventory.AddStack(
            mealStack,
            meal,
            quantity: 2,
            location: ItemLocation.InWorld(new CellId(1, 5, 0)),
            tick: startTick));

        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        EntityId firstAgent = Require(state.Entities.RegisterNew());
        EntityId secondAgent = Require(state.Entities.RegisterNew());
        Require(agents.Add(CreateAgent(firstAgent, "Settlement Dwarf A")));
        Require(agents.Add(CreateAgent(secondAgent, "Settlement Dwarf B")));

        BuildingFacilitiesState facilities = new BuildingFacilitiesState();
        EntityId home = Require(state.Entities.RegisterNew());
        AddFacility(
            facilities,
            Require(state.Entities.RegisterNew()),
            home,
            BuildingFacilityKind.Bed,
            new CellId(2, 5, 0));
        AddFacility(
            facilities,
            Require(state.Entities.RegisterNew()),
            home,
            BuildingFacilityKind.Bed,
            new CellId(3, 5, 0));
        AddFacility(
            facilities,
            Require(state.Entities.RegisterNew()),
            home,
            BuildingFacilityKind.Leisure,
            new CellId(2, 6, 0));
        AddFacility(
            facilities,
            Require(state.Entities.RegisterNew()),
            home,
            BuildingFacilityKind.Leisure,
            new CellId(3, 6, 0));

        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryBuildingFacilitiesRepository facilitiesRepository =
            new InMemoryBuildingFacilitiesRepository(facilities);
        ResidentSettlementSystem settlement = new ResidentSettlementSystem(
            agents,
            new InMemoryAgentDecisionContextProvider(new AgentDecisionContext(
                foodAvailable: true,
                bedAvailable: true,
                workAvailable: true,
                restAvailable: true,
                escapeRouteAvailable: true,
                threatLevel: 0)),
            inventoryRepository,
            facilitiesRepository,
            journal,
            new AgentDecisionSystem(),
            AgentBehaviorPolicy.CreateDefault());

        long finalTick = startTick;
        for (int offset = 0; offset < MaximumSettlementTicks; offset++)
        {
            finalTick = checked(startTick + offset);
            settlement.Execute(new SimulationContext(finalTick, state));
            if (HasCompletedNeedsWorkflow(
                journal,
                inventory,
                facilities,
                firstAgent,
                secondAgent,
                meal))
            {
                break;
            }
        }

        AgentSnapshot first = agents.Get(firstAgent)!.CreateSnapshot(finalTick);
        AgentSnapshot second = agents.Get(secondAgent)!.CreateSnapshot(finalTick);
        int eatingCompleted = CountCompleted(
            journal,
            firstAgent,
            secondAgent,
            AgentIntentKind.Eat);
        int sleepingCompleted = CountCompleted(
            journal,
            firstAgent,
            secondAgent,
            AgentIntentKind.Sleep);
        if (inventory.GetTotal(meal) != 0
            || eatingCompleted != 2
            || sleepingCompleted != 2
            || facilities.GetReservations().Count != 0
            || first.Needs.Nutrition.Points <= 0
            || second.Needs.Nutrition.Points <= 0
            || first.Needs.Alertness.Points <= 0
            || second.Needs.Alertness.Points <= 0)
        {
            throw new InvalidOperationException(
                "Headless residents did not complete deterministic food and sleep cycles. "
                + $"tick={finalTick}, food={inventory.GetTotal(meal)}, "
                + $"eat={eatingCompleted}, sleep={sleepingCompleted}, "
                + $"facilityReservations={facilities.GetReservations().Count}, "
                + $"firstNutrition={first.Needs.Nutrition.Points}, "
                + $"secondNutrition={second.Needs.Nutrition.Points}, "
                + $"firstAlertness={first.Needs.Alertness.Points}, "
                + $"secondAlertness={second.Needs.Alertness.Points}.");
        }

        HeadlessCombatScenario.Run(journal, checked(finalTick + 1));
        return 2;
    }

    private static bool HasCompletedNeedsWorkflow(
        InMemoryExecutionJournal journal,
        InventoryState inventory,
        BuildingFacilitiesState facilities,
        EntityId firstAgent,
        EntityId secondAgent,
        ItemId meal)
    {
        return inventory.GetTotal(meal) == 0
            && CountCompleted(
                journal,
                firstAgent,
                secondAgent,
                AgentIntentKind.Eat) == 2
            && CountCompleted(
                journal,
                firstAgent,
                secondAgent,
                AgentIntentKind.Sleep) == 2
            && facilities.GetReservations().Count == 0;
    }

    private static int CountCompleted(
        InMemoryExecutionJournal journal,
        EntityId firstAgent,
        EntityId secondAgent,
        AgentIntentKind intent)
    {
        return journal.Events.OfType<AgentActionCompleted>()
            .Count(value => (value.AgentId == firstAgent || value.AgentId == secondAgent)
                && value.IntentKind == intent);
    }

    private static AgentState CreateAgent(EntityId id, string name)
    {
        DailySchedule schedule = new DailySchedule(
            ticksPerDay: 24,
            new[] { new ScheduleSegment(0, 24, ScheduleActivity.Rest) });
        return new AgentState(
            id,
            name,
            new AgentNeedsSnapshot(
                new NeedValue(500),
                new NeedValue(500),
                new NeedValue(1_000),
                new NeedValue(10_000)),
            schedule,
            new[] { new AgentSkillValue(new AgentSkillId("general.work"), 4_000) });
    }

    private static void AddFacility(
        BuildingFacilitiesState facilities,
        EntityId facilityId,
        EntityId buildingId,
        BuildingFacilityKind kind,
        CellId position)
    {
        Require(facilities.Add(new BuildingFacilityDefinition(
            facilityId,
            buildingId,
            kind,
            position)));
    }

    private static T Require<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }

        return result.Value;
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
}
