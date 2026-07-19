using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryTravelCostJobCandidateProviderTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId FastResidentId = Id(2);
    private static readonly EntityId LoadedResidentId = Id(3);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");

    [Fact]
    public void Loaded_cargo_increases_job_distance_cost_before_scoring()
    {
        InventoryState inventory = CreateInventory();
        InMemoryJobCandidateProvider inner = new InMemoryJobCandidateProvider();
        JobSnapshot job = CreateJob();
        inner.SetCandidates(JobId, new[]
        {
            new JobCandidate(FastResidentId, 5_000, 12, true),
            new JobCandidate(LoadedResidentId, 5_000, 12, true),
        });
        InventoryTravelCostJobCandidateProvider provider =
            new InventoryTravelCostJobCandidateProvider(
                inner,
                new InMemoryInventoryRepository(inventory));

        JobCandidate[] candidates = provider.GetCandidates(job, tick: 1)
            .OrderBy(candidate => candidate.AgentId.ToString())
            .ToArray();

        Assert.Equal(12, candidates[0].DistanceCost);
        Assert.Equal(16, candidates[1].DistanceCost);
        Assert.True(
            JobCandidateEvaluator.Score(job, candidates[0])
            > JobCandidateEvaluator.Score(job, candidates[1]));
    }

    [Fact]
    public void Zero_distance_cost_remains_zero()
    {
        InventoryState inventory = CreateInventory();
        InMemoryJobCandidateProvider inner = new InMemoryJobCandidateProvider();
        JobSnapshot job = CreateJob();
        inner.SetCandidates(JobId, new[]
        {
            new JobCandidate(LoadedResidentId, 5_000, 0, true),
        });

        JobCandidate candidate = Assert.Single(
            new InventoryTravelCostJobCandidateProvider(
                inner,
                new InMemoryInventoryRepository(inventory))
            .GetCandidates(job, tick: 1));

        Assert.Equal(0, candidate.DistanceCost);
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            new ItemDefinition(
                BasketId,
                "Basket",
                1,
                false,
                new[] { raw },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Cargo,
                    tier: 1,
                    addedSlots: 4,
                    acceptedCategories: new[] { raw },
                    moveSpeedMultiplierWhenOccupied: 0.75d,
                    visualAttachmentId: "visual.basket")),
        }));
        Assert.True(inventory.AddStack(
            Id(10),
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                LoadedResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(11),
            OreId,
            1,
            ItemLocation.InResidentSlot(
                LoadedResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static JobSnapshot CreateJob()
    {
        return new JobSnapshot(
            new DigJobDefinition(
                JobId,
                new DigJobTarget(new CellId(4, 4)),
                priority: 500,
                createdTick: 0,
                retryPolicy: JobRetryPolicy.Default),
            JobStatus.Available,
            JobStageKind.None,
            assignedAgentId: null,
            retryCount: 0,
            nextRetryTick: 0,
            version: 1,
            reason: null);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}