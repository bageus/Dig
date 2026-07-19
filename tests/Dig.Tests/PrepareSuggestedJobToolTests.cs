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

public sealed class PrepareSuggestedJobToolTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId AgentId = Id(2);
    private static readonly EntityId HammerStackId = Id(3);
    private static readonly EntityId PickaxeStackId = Id(4);
    private static readonly ItemId HammerItemId = new ItemId("tool.hammer");
    private static readonly ItemId PickaxeItemId = new ItemId("tool.pickaxe");

    [Fact]
    public void Suggested_tool_action_switches_held_reference_and_updates_retained_diagnostic()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository jobs = CreateClaimedJob(reserveSuggestedTool: true);
        InventoryState inventory = CreateInventory();
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        RecordSuggestion(journal, AgentId, score: 1234, tick: 2);
        PrepareSuggestedJobToolHandler handler = new PrepareSuggestedJobToolHandler(
            jobs,
            new InventoryJobToolPreparationService(inventories, journal),
            journal,
            journal);

        Result result = handler.Handle(new PrepareSuggestedJobToolCommand(JobId, tick: 5));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(ItemLocation.InAgent(AgentId), inventory.GetStack(HammerStackId)!.Location);
        Assert.Equal(ItemLocation.InAgent(AgentId), inventory.GetStack(PickaxeStackId)!.Location);
        Assert.Equal(0, inventory.GetStack(HammerStackId)!.HeldQuantity);
        Assert.Equal(1, inventory.GetStack(PickaxeStackId)!.HeldQuantity);
        Assert.Equal(PickaxeStackId, inventory.GetHeldItem(AgentId)!.Value.StackId);
        JobAssignmentReport retained = journal.Find(JobId)!;
        JobAssignment assignment = Assert.Single(retained.Assignments);
        Assert.Equal(5, retained.Tick);
        Assert.Equal(1234, assignment.Score);
        Assert.Equal(JobToolPreparationOutcome.Switched, assignment.ToolPreparation);
        Assert.Equal(PickaxeStackId, assignment.ToolStackId);
        Assert.Equal(JobStatus.Claimed, jobs.Get().Get(JobId)!.Status);
    }

    [Fact]
    public void Missing_tool_reservation_rejects_action_without_inventory_mutation()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository jobs = CreateClaimedJob(reserveSuggestedTool: false);
        InventoryState inventory = CreateInventory();
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        RecordSuggestion(journal, AgentId, score: 900, tick: 2);
        PrepareSuggestedJobToolHandler handler = new PrepareSuggestedJobToolHandler(
            jobs,
            new InventoryJobToolPreparationService(inventories, journal),
            journal,
            journal);

        Result result = handler.Handle(new PrepareSuggestedJobToolCommand(JobId, tick: 5));

        Assert.True(result.IsFailure);
        Assert.Equal(PrepareSuggestedJobToolErrors.ToolReservationMissing, result.Error);
        Assert.Equal(ItemLocation.InAgent(AgentId), inventory.GetStack(HammerStackId)!.Location);
        Assert.Equal(ItemLocation.InAgent(AgentId), inventory.GetStack(PickaxeStackId)!.Location);
        Assert.Equal(1, inventory.GetStack(HammerStackId)!.HeldQuantity);
        Assert.Equal(0, inventory.GetStack(PickaxeStackId)!.HeldQuantity);
        Assert.Equal(HammerStackId, inventory.GetHeldItem(AgentId)!.Value.StackId);
        Assert.Equal(
            JobToolPreparationOutcome.Suggested,
            Assert.Single(journal.Find(JobId)!.Assignments).ToolPreparation);
    }

    [Fact]
    public void Stale_resident_rejects_action_before_inventory_mutation()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository jobs = CreateClaimedJob(reserveSuggestedTool: true);
        InventoryState inventory = CreateInventory();
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        RecordSuggestion(journal, Id(99), score: 700, tick: 2);
        PrepareSuggestedJobToolHandler handler = new PrepareSuggestedJobToolHandler(
            jobs,
            new InventoryJobToolPreparationService(inventories, journal),
            journal,
            journal);

        Result result = handler.Handle(new PrepareSuggestedJobToolCommand(JobId, tick: 5));

        Assert.True(result.IsFailure);
        Assert.Equal(PrepareSuggestedJobToolErrors.SuggestionStale, result.Error);
        Assert.Equal(ItemLocation.InAgent(AgentId), inventory.GetStack(HammerStackId)!.Location);
        Assert.Equal(ItemLocation.InAgent(AgentId), inventory.GetStack(PickaxeStackId)!.Location);
        Assert.Equal(1, inventory.GetStack(HammerStackId)!.HeldQuantity);
        Assert.Equal(0, inventory.GetStack(PickaxeStackId)!.HeldQuantity);
        Assert.Equal(HammerStackId, inventory.GetHeldItem(AgentId)!.Value.StackId);
    }

    private static InMemoryJobRepository CreateClaimedJob(bool reserveSuggestedTool)
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        JobSystem jobs = repository.Get();
        Assert.True(jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(4, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 0).IsSuccess);
        Result claimed = reserveSuggestedTool
            ? jobs.Claim(JobId, AgentId, PickaxeStackId, tick: 1)
            : jobs.Claim(JobId, AgentId, tick: 1);
        Assert.True(claimed.IsSuccess, claimed.Error?.ToString());
        repository.Save(jobs);
        return repository;
    }

    private static InventoryState CreateInventory()
    {
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(HammerItemId, "Hammer", 1, isTool: true),
            new ItemDefinition(PickaxeItemId, "Pickaxe", 1, isTool: true),
        }));
        Assert.True(inventory.AddStack(
            HammerStackId,
            HammerItemId,
            quantity: 1,
            ItemLocation.InAgent(AgentId),
            tick: 0).IsSuccess);
        Assert.True(inventory.EquipTool(HammerStackId, AgentId, tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            PickaxeStackId,
            PickaxeItemId,
            quantity: 1,
            ItemLocation.InAgent(AgentId),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static void RecordSuggestion(
        InMemoryExecutionJournal journal,
        EntityId agentId,
        long score,
        long tick)
    {
        journal.Record(new JobAssignmentReport(
            tick,
            new[]
            {
                new JobAssignment(
                    JobId,
                    agentId,
                    score,
                    JobToolPreparationOutcome.Suggested,
                    PickaxeStackId),
            },
            System.Array.Empty<JobAssignmentFailure>()));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}