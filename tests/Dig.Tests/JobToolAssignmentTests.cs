using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class JobToolAssignmentTests
{
    private static readonly ItemId PickaxeItemId = new ItemId("tool.pickaxe");
    private static readonly ItemId HammerItemId = new ItemId("tool.hammer");
    private static readonly EntityId FirstAgentId = Id(1);
    private static readonly EntityId SecondAgentId = Id(2);
    private static readonly EntityId FirstToolStackId = Id(11);
    private static readonly EntityId SecondToolStackId = Id(12);
    private static readonly EntityId JobId = Id(20);

    [Fact]
    public void Mining_prefers_resident_with_equipped_pickaxe()
    {
        InventoryState inventory = CreateInventory();
        AddAndEquip(inventory, FirstToolStackId, PickaxeItemId, FirstAgentId);
        AddAndEquip(inventory, SecondToolStackId, HammerItemId, SecondAgentId);
        JobToolHarness harness = CreateHarness(inventory, CreateDigJob());
        harness.Candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                FirstAgentId,
                skillLevel: 1_000,
                distanceCost: 20,
                isAvailable: true),
            new JobCandidate(
                SecondAgentId,
                skillLevel: 10_000,
                distanceCost: 0,
                isAvailable: true),
        });

        JobAssignmentReport report = harness.Assign(
            JobToolPreparationMode.Automatic,
            configurePreparation: true);

        JobAssignment assignment = Assert.Single(report.Assignments);
        Assert.Equal(FirstAgentId, assignment.AgentId);
        Assert.Equal(JobToolPreparationOutcome.AlreadyEquipped, assignment.ToolPreparation);
        Assert.Equal(FirstToolStackId, assignment.ToolStackId);
        Assert.Contains(
            harness.Jobs.Get().GetReservations(),
            value => value.Key == ReservationKey.ForTool(FirstToolStackId));
    }

    [Fact]
    public void Construction_prefers_resident_with_equipped_hammer()
    {
        InventoryState inventory = CreateInventory();
        AddAndEquip(inventory, FirstToolStackId, PickaxeItemId, FirstAgentId);
        AddAndEquip(inventory, SecondToolStackId, HammerItemId, SecondAgentId);
        JobToolHarness harness = CreateHarness(inventory, CreateConstructionJob());
        harness.Candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                FirstAgentId,
                skillLevel: 10_000,
                distanceCost: 0,
                isAvailable: true),
            new JobCandidate(
                SecondAgentId,
                skillLevel: 1_000,
                distanceCost: 20,
                isAvailable: true),
        });

        JobAssignment assignment = Assert.Single(harness.Assign(
            JobToolPreparationMode.Automatic,
            configurePreparation: true).Assignments);

        Assert.Equal(SecondAgentId, assignment.AgentId);
        Assert.Equal(JobToolPreparationOutcome.AlreadyEquipped, assignment.ToolPreparation);
        Assert.Equal(SecondToolStackId, assignment.ToolStackId);
    }

    [Fact]
    public void Automatic_mode_switches_tool_before_claiming_job()
    {
        InventoryState inventory = CreateInventory();
        AddAndEquip(inventory, FirstToolStackId, HammerItemId, FirstAgentId);
        AddCarried(inventory, SecondToolStackId, PickaxeItemId, FirstAgentId);
        JobToolHarness harness = CreateHarness(inventory, CreateDigJob());
        harness.Candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                FirstAgentId,
                skillLevel: 5_000,
                distanceCost: 1,
                isAvailable: true),
        });

        JobAssignment assignment = Assert.Single(harness.Assign(
            JobToolPreparationMode.Automatic,
            configurePreparation: true).Assignments);

        Assert.Equal(JobToolPreparationOutcome.Switched, assignment.ToolPreparation);
        Assert.Equal(SecondToolStackId, assignment.ToolStackId);
        Assert.Equal(
            ItemLocation.InAgent(FirstAgentId),
            inventory.GetStack(FirstToolStackId)!.Location);
        Assert.Equal(
            ItemLocation.EquippedBy(FirstAgentId),
            inventory.GetStack(SecondToolStackId)!.Location);
        Assert.Equal(JobStatus.Claimed, harness.Jobs.Get().Get(JobId)!.Status);
    }

    [Fact]
    public void Suggest_mode_reports_switch_without_mutating_inventory()
    {
        InventoryState inventory = CreateInventory();
        AddAndEquip(inventory, FirstToolStackId, HammerItemId, FirstAgentId);
        AddCarried(inventory, SecondToolStackId, PickaxeItemId, FirstAgentId);
        JobToolHarness harness = CreateHarness(inventory, CreateDigJob());
        harness.Candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                FirstAgentId,
                skillLevel: 5_000,
                distanceCost: 1,
                isAvailable: true),
        });

        JobAssignment assignment = Assert.Single(harness.Assign(
            JobToolPreparationMode.Suggest,
            configurePreparation: false).Assignments);

        Assert.Equal(JobToolPreparationOutcome.Suggested, assignment.ToolPreparation);
        Assert.Equal(SecondToolStackId, assignment.ToolStackId);
        Assert.Equal(
            ItemLocation.EquippedBy(FirstAgentId),
            inventory.GetStack(FirstToolStackId)!.Location);
        Assert.Equal(
            ItemLocation.InAgent(FirstAgentId),
            inventory.GetStack(SecondToolStackId)!.Location);
        Assert.Contains(
            harness.Jobs.Get().GetReservations(),
            value => value.Key == ReservationKey.ForTool(SecondToolStackId));
    }

    [Fact]
    public void Reserved_carried_tool_is_not_switched_automatically()
    {
        InventoryState inventory = CreateInventory();
        AddAndEquip(inventory, FirstToolStackId, HammerItemId, FirstAgentId);
        AddCarried(inventory, SecondToolStackId, PickaxeItemId, FirstAgentId);
        Assert.True(inventory.ReserveQuantity(
            SecondToolStackId,
            Id(99),
            quantity: 1,
            tick: 1).IsSuccess);
        JobToolHarness harness = CreateHarness(inventory, CreateDigJob());
        harness.Candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                FirstAgentId,
                skillLevel: 5_000,
                distanceCost: 1,
                isAvailable: true),
        });

        JobAssignment assignment = Assert.Single(harness.Assign(
            JobToolPreparationMode.Automatic,
            configurePreparation: true).Assignments);

        Assert.Equal(JobToolPreparationOutcome.None, assignment.ToolPreparation);
        Assert.Null(assignment.ToolStackId);
        Assert.Equal(
            ItemLocation.EquippedBy(FirstAgentId),
            inventory.GetStack(FirstToolStackId)!.Location);
        Assert.Equal(
            ItemLocation.InAgent(FirstAgentId),
            inventory.GetStack(SecondToolStackId)!.Location);
    }

    [Fact]
    public void Automatic_mode_reports_missing_preparation_service()
    {
        InventoryState inventory = CreateInventory();
        AddAndEquip(inventory, FirstToolStackId, HammerItemId, FirstAgentId);
        AddCarried(inventory, SecondToolStackId, PickaxeItemId, FirstAgentId);
        JobToolHarness harness = CreateHarness(inventory, CreateDigJob());
        harness.Candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                FirstAgentId,
                skillLevel: 5_000,
                distanceCost: 1,
                isAvailable: true),
        });

        JobAssignmentReport report = harness.Assign(
            JobToolPreparationMode.Automatic,
            configurePreparation: false);

        Assert.Empty(report.Assignments);
        Assert.Equal(
            JobErrors.ToolPreparationUnavailable,
            Assert.Single(report.Failures).Error);
        Assert.Equal(JobStatus.Available, harness.Jobs.Get().Get(JobId)!.Status);
    }

    private static JobToolHarness CreateHarness(
        InventoryState inventory,
        JobDefinition definition)
    {
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        Assert.True(jobs.Get().Add(definition).IsSuccess);
        Assert.True(jobs.Get().MakeAvailable(definition.Id, tick: 0).IsSuccess);
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        return new JobToolHarness(
            jobs,
            inventories,
            new InMemoryJobCandidateProvider(),
            new InMemoryExecutionJournal());
    }

    private static DigJobDefinition CreateDigJob()
    {
        return new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(4, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);
    }

    private static BuildingWorkJobDefinition CreateConstructionJob()
    {
        return new BuildingWorkJobDefinition(
            JobId,
            Id(30),
            BuildingWorkKind.Construction,
            new CellId(7, 8),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(PickaxeItemId, "Pickaxe", 1, isTool: true),
            new ItemDefinition(HammerItemId, "Hammer", 1, isTool: true),
        }));
    }

    private static EquipmentRates CreateRates()
    {
        return new EquipmentRates(new[]
        {
            new EquipmentProfile(
                PickaxeItemId,
                EquipmentAppearanceKind.Mining,
                EquipmentWorkKind.Mining,
                workIntervalTicks: 1),
            new EquipmentProfile(
                HammerItemId,
                EquipmentAppearanceKind.Construction,
                EquipmentWorkKind.Construction,
                workIntervalTicks: 1),
        });
    }

    private static void AddAndEquip(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        EntityId agentId)
    {
        AddCarried(inventory, stackId, itemId, agentId);
        Assert.True(inventory.EquipTool(stackId, agentId, tick: 0).IsSuccess);
    }

    private static void AddCarried(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        EntityId agentId)
    {
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            1,
            ItemLocation.InAgent(agentId),
            tick: 0).IsSuccess);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private sealed class JobToolHarness
    {
        public JobToolHarness(
            InMemoryJobRepository jobs,
            InMemoryInventoryRepository inventory,
            InMemoryJobCandidateProvider candidates,
            InMemoryExecutionJournal journal)
        {
            Jobs = jobs;
            Inventory = inventory;
            Candidates = candidates;
            Journal = journal;
        }

        public InMemoryJobRepository Jobs { get; }
        public InMemoryInventoryRepository Inventory { get; }
        public InMemoryJobCandidateProvider Candidates { get; }
        public InMemoryExecutionJournal Journal { get; }

        public JobAssignmentReport Assign(
            JobToolPreparationMode mode,
            bool configurePreparation)
        {
            InventoryAwareJobCandidateProvider provider =
                new InventoryAwareJobCandidateProvider(
                    Candidates,
                    Inventory,
                    CreateRates());
            IJobToolPreparationService? preparation = configurePreparation
                ? new InventoryJobToolPreparationService(Inventory, Journal)
                : null;
            return new AssignAvailableJobsHandler(
                Jobs,
                provider,
                Journal,
                preparation).Handle(new AssignAvailableJobsCommand(
                    tick: 2,
                    toolPreparationMode: mode));
        }
    }
}
}
