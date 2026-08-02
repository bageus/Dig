using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ResidentFreeTimeNeedsPlayModeTests
{
    [Test]
    public void Work_hunger_notifies_without_auto_eat_and_floor_sleep_remains_available()
    {
        EntityId residentId = EntityId.Parse(
            "a1000000000000000000000000000001");
        ItemId meal = new ItemId("food.playmode.meal");
        AgentState resident = new AgentState(
            residentId,
            "Needs resident",
            new AgentNeedsSnapshot(
                new NeedValue(1_600),
                new NeedValue(1_000),
                new NeedValue(8_000),
                new NeedValue(10_000)),
            new DailySchedule(
                8,
                new[]
                {
                    new ScheduleSegment(0, 8, ScheduleActivity.Work),
                }));
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.That(agents.Add(resident).IsSuccess, Is.True);

        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                meal,
                "Meal",
                maximumStackSize: 10,
                isTool: false,
                new[] { new ItemCategoryId("food") }),
        }));
        Assert.That(inventory.AddStack(
            EntityId.Parse("a2000000000000000000000000000002"),
            meal,
            quantity: 1,
            ItemLocation.InWorld(new CellId(1, 0, 0)),
            tick: 0).IsSuccess, Is.True);

        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryBuildingFacilitiesRepository facilitiesRepository =
            new InMemoryBuildingFacilitiesRepository(
                new BuildingFacilitiesState());
        InMemoryExecutionJournal events = new InMemoryExecutionJournal();
        ResidentSettlementSystem system = new ResidentSettlementSystem(
            agents,
            new InMemoryAgentDecisionContextProvider(
                AgentDecisionContext.AllAvailable()),
            inventoryRepository,
            facilitiesRepository,
            events,
            new AgentDecisionSystem(),
            AgentBehaviorPolicy.CreateDefault());
        SimulationState simulation = SimulationState.Create(
            worldSeed: 1,
            tickDuration: TimeSpan.FromMilliseconds(100));

        system.Execute(new SimulationContext(0, simulation));

        AgentSnapshot snapshot = agents.Get(residentId)!.CreateSnapshot(0);
        Assert.That(
            snapshot.ActiveAction!.Value.IntentKind,
            Is.EqualTo(AgentIntentKind.Sleep));
        Assert.That(
            snapshot.ActiveAction.Value.Target!.Value.Kind,
            Is.EqualTo(AgentActivityTargetKind.FloorSleep));
        Assert.That(inventoryRepository.Get().GetTotal(meal), Is.EqualTo(1));
        Assert.That(
            inventoryRepository.Get().CreateSnapshot().Stacks
                .SelectMany(value => value.Reservations),
            Is.Empty);
        Assert.That(
            events.Events.OfType<AgentNeedThresholdCrossed>()
                .Any(value => value.Kind == AgentNeedThresholdKind.Hunger),
            Is.True);
    }
}
}
