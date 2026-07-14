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
using Xunit;

namespace Dig.Tests
{

internal sealed class ResidentSettlementHarness
{
    public static readonly ItemId Meal = new ItemId("food.meal");
    private readonly SimulationState _simulation = SimulationState.Create(
        worldSeed: 77,
        tickDuration: TimeSpan.FromMilliseconds(100));

    public ResidentSettlementHarness()
    {
        ItemCatalog catalog = new ItemCatalog(new[]
        {
            new ItemDefinition(
                Meal,
                "Meal",
                maximumStackSize: 20,
                isTool: false,
                new[] { new ItemCategoryId("food") }),
        });
        Inventory = new InventoryState(catalog);
        Facilities = new BuildingFacilitiesState();
        Agents = new InMemoryAgentRepository();
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        FacilitiesRepository = new InMemoryBuildingFacilitiesRepository(Facilities);
        Journal = new InMemoryExecutionJournal();
        ExternalContexts = new InMemoryAgentDecisionContextProvider(
            new AgentDecisionContext(
                foodAvailable: true,
                bedAvailable: true,
                workAvailable: true,
                restAvailable: true,
                escapeRouteAvailable: true,
                threatLevel: 0));
        Policy = AgentBehaviorPolicy.CreateDefault();
        System = new ResidentSettlementSystem(
            Agents,
            ExternalContexts,
            InventoryRepository,
            FacilitiesRepository,
            Journal,
            new AgentDecisionSystem(),
            Policy);
    }

    public InventoryState Inventory { get; }

    public BuildingFacilitiesState Facilities { get; }

    public InMemoryAgentRepository Agents { get; }

    public InMemoryInventoryRepository InventoryRepository { get; }

    public InMemoryBuildingFacilitiesRepository FacilitiesRepository { get; }

    public InMemoryExecutionJournal Journal { get; }

    public InMemoryAgentDecisionContextProvider ExternalContexts { get; }

    public AgentBehaviorPolicy Policy { get; }

    public ResidentSettlementSystem System { get; }

    public EntityId AddAgent(
        string id,
        int nutrition,
        int alertness,
        int mood,
        ScheduleActivity scheduleActivity = ScheduleActivity.Work)
    {
        EntityId agentId = EntityId.Parse(id);
        DailySchedule schedule = new DailySchedule(
            ticksPerDay: 8,
            new[] { new ScheduleSegment(0, 8, scheduleActivity) });
        AgentState agent = new AgentState(
            agentId,
            id,
            new AgentNeedsSnapshot(
                new NeedValue(nutrition),
                new NeedValue(alertness),
                new NeedValue(mood),
                new NeedValue(10_000)),
            schedule,
            new[] { new AgentSkillValue(new AgentSkillId("general.work"), 4_000) });
        Assert.True(Agents.Add(agent).IsSuccess);
        return agentId;
    }

    public EntityId AddFood(string id, int quantity)
    {
        EntityId stackId = EntityId.Parse(id);
        Assert.True(Inventory.AddStack(
            stackId,
            Meal,
            quantity,
            ItemLocation.InWorld(new CellId(2, 2)),
            tick: 0).IsSuccess);
        return stackId;
    }

    public EntityId AddFacility(
        string id,
        BuildingFacilityKind kind,
        int x)
    {
        EntityId facilityId = EntityId.Parse(id);
        EntityId buildingId = EntityId.Parse(
            kind == BuildingFacilityKind.Bed
                ? "93000000000000000000000000000001"
                : "93000000000000000000000000000002");
        Assert.True(Facilities.Add(new BuildingFacilityDefinition(
            facilityId,
            buildingId,
            kind,
            new CellId(x, 3))).IsSuccess);
        return facilityId;
    }

    public void Execute(long tick)
    {
        System.Execute(new SimulationContext(tick, _simulation));
    }

    public AgentSnapshot Snapshot(EntityId agentId, long tick)
    {
        return Agents.Get(agentId)!.CreateSnapshot(tick);
    }
}
}
