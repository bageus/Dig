using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class MiningOutputHaulingConservationTests
{
    private static readonly EntityId SourceUnitId = Id(1);
    private static readonly EntityId JobId = Id(2);
    private static readonly EntityId ResidentId = Id(3);
    private static readonly EntityId StorageId = Id(4);
    private static readonly EntityId AcquireScratchId = Id(5);
    private static readonly EntityId DepositScratchId = Id(6);
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly CellId SourceCell = new CellId(2, 2, 1);

    [Fact]
    public void Quantity_one_mining_output_keeps_identity_and_quantity_through_hauling()
    {
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                Stone,
                "Stone",
                maximumStackSize: 20,
                isTool: false,
                new[] { new ItemCategoryId("raw") }),
        }));
        Assert.True(inventory.AddUnit(
            SourceUnitId,
            Stone,
            ItemLocation.InWorld(SourceCell),
            tick: 0).IsSuccess);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveOutput(), new[] { SourceUnitId });

        StorageState storage = new StorageState();
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            StorageId,
            "Stone stockpile",
            priority: 900,
            capacity: 20,
            new StorageFilter(
                acceptsAll: false,
                allowedItems: new[] { Stone }))).IsSuccess);
        JobSystem jobs = new JobSystem();
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryStorageRepository storageRepository =
            new InMemoryStorageRepository(storage);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository(jobs);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        Assert.True(new CreateHaulingJobHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            journal).Handle(new CreateHaulingJobCommand(
                JobId,
                SourceUnitId,
                quantity: 1,
                destinationStorageId: StorageId,
                priority: 600,
                tick: 1)).IsSuccess);

        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(ResidentId, skillLevel: 1_000, distanceCost: 1, isAvailable: true),
        });
        JobAssignmentReport assigned = new AssignAvailableJobsHandler(
            jobRepository,
            candidates,
            journal,
            haulingResidentSlotClaims: new HaulingResidentSlotClaimService(
                inventoryRepository,
                journal)).Handle(new AssignAvailableJobsCommand(tick: 2));
        Assert.Single(assigned.Assignments);
        Assert.True(jobs.Start(JobId, tick: 2).IsSuccess);
        Assert.True(new AcquireHaulingItemHandler(
            inventoryRepository,
            jobRepository,
            journal).Handle(new AcquireHaulingItemCommand(
                JobId,
                AcquireScratchId,
                tick: 3)).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 4).IsSuccess);

        Result completed = new CompleteHaulingJobHandler(
            inventoryRepository,
            storageRepository,
            jobRepository,
            journal,
            AgentSkillGrantTestFactory.Create(ResidentId, journal)).Handle(
                new CompleteHaulingJobCommand(
                    JobId,
                    DepositScratchId,
                    tick: 5));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        ItemStackSnapshot stored = inventory.GetStack(SourceUnitId)!;
        Assert.Equal(1, stored.Quantity);
        Assert.Equal(ItemLocation.InStorage(StorageId), stored.Location);
        Assert.Null(inventory.GetStack(AcquireScratchId));
        Assert.Null(inventory.GetStack(DepositScratchId));
        Assert.Equal(1, inventory.GetTotal(Stone));
        Assert.True(new MiningOutputIntegrityDiagnostics()
            .Inspect(commits, inventory).IsValid);
        Assert.True(new MiningOutputSaveCoordinator()
            .Capture(commits, inventory).IsSuccess);
    }

    private static MiningOutputPlan ResolveOutput()
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 11,
            generatorVersion: 2,
            SourceCell,
            new MaterialDefinition(
                new MaterialId("terrain.stone_rock"),
                "Stone rock",
                isSolid: true,
                hardness: 10,
                isMineable: true,
                outputProfile: new TerrainOutputProfile(
                    "terrain-output.stone",
                    version: 1,
                    new[]
                    {
                        new TerrainOutputEntry(Stone, 1_000, 1, 1),
                    })),
            new TerrainDepositState());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
