using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class HaulingResidentTransitTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId SourceStackId = Id(2);
    private static readonly EntityId JobId = Id(3);
    private static readonly EntityId StorageId = Id(4);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");

    [Fact]
    public void Acquisition_occupies_cargo_and_deposit_restores_full_speed()
    {
        Harness harness = new Harness(existingCargoQuantity: 0, haulQuantity: 8);
        harness.AssignAndStart();
        Assert.Equal(1d, harness.Inventory.GetResidentMoveSpeedMultiplier(ResidentId));

        Result acquired = harness.Acquire(Id(20), tick: 3);

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(JobId));
        Assert.Equal(8, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0)));
        Assert.Equal(0.75d, harness.Inventory.GetResidentMoveSpeedMultiplier(ResidentId));
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 4).IsSuccess);

        Result completed = harness.Complete(Id(21), tick: 5);

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(1d, harness.Inventory.GetResidentMoveSpeedMultiplier(ResidentId));
        Assert.Equal(8, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InStorage(StorageId)));
        Assert.Equal(10, harness.Inventory.GetTotal(OreId));
    }

    [Fact]
    public void Partial_existing_stack_capacity_is_used_before_new_cargo_slot()
    {
        Harness harness = new Harness(existingCargoQuantity: 95, haulQuantity: 8);
        harness.AssignAndStart();

        Result acquired = harness.Acquire(Id(20), tick: 3);

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        ItemStackSnapshot[] cargo = harness.Inventory.CreateSnapshot().Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(stack => stack.Location.HasOwner
                && stack.Location.OwnerId == ResidentId)
            .Where(stack => stack.Location.ResidentCompartment
                == ResidentInventoryCompartment.Cargo)
            .OrderBy(stack => stack.Location.ResidentSlotIndex)
            .ToArray();
        Assert.Equal(2, cargo.Length);
        Assert.Equal(100, cargo[0].Quantity);
        Assert.Equal(5, cargo[0].ReservedQuantity);
        Assert.Equal(3, cargo[1].Quantity);
        Assert.Equal(3, cargo[1].ReservedQuantity);
        Assert.Equal(2, harness.Inventory.GetStack(SourceStackId)!.Quantity);

        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        Assert.True(harness.Complete(Id(21), tick: 5).IsSuccess);
        Assert.Equal(105, harness.Inventory.GetTotal(OreId));
        Assert.Equal(95, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0)));
        Assert.Equal(8, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InStorage(StorageId)));
    }

    private sealed class Harness
    {
        public Harness(int existingCargoQuantity, int haulQuantity)
        {
            HaulQuantity = haulQuantity;
            ItemCategoryId raw = new ItemCategoryId("raw");
            Inventory = new InventoryState(new ItemCatalog(new[]
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
            Assert.True(Inventory.AddStack(
                Id(10),
                BasketId,
                1,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    0),
                tick: 0).IsSuccess);
            if (existingCargoQuantity > 0)
            {
                Assert.True(Inventory.AddStack(
                    Id(11),
                    OreId,
                    existingCargoQuantity,
                    ItemLocation.InResidentSlot(
                        ResidentId,
                        ResidentInventoryCompartment.Cargo,
                        0),
                    tick: 0).IsSuccess);
            }

            Assert.True(Inventory.AddStack(
                SourceStackId,
                OreId,
                10,
                ItemLocation.InWorld(new CellId(2, 2)),
                tick: 0).IsSuccess);
            Storage = new StorageState();
            Assert.True(Storage.AddZone(new StorageZoneDefinition(
                StorageId,
                "Storage",
                priority: 500,
                capacity: 200,
                StorageFilter.All())).IsSuccess);
            Jobs = new JobSystem();
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            StorageRepository = new InMemoryStorageRepository(Storage);
            JobRepository = new InMemoryJobRepository(Jobs);
            Journal = new InMemoryExecutionJournal();
            Assert.True(new CreateHaulingJobHandler(
                InventoryRepository,
                StorageRepository,
                JobRepository,
                Journal).Handle(new CreateHaulingJobCommand(
                    JobId,
                    SourceStackId,
                    haulQuantity,
                    StorageId,
                    priority: 500,
                    tick: 1)).IsSuccess);
        }

        public int HaulQuantity { get; }
        public InventoryState Inventory { get; }
        public StorageState Storage { get; }
        public JobSystem Jobs { get; }
        public InMemoryInventoryRepository InventoryRepository { get; }
        public InMemoryStorageRepository StorageRepository { get; }
        public InMemoryJobRepository JobRepository { get; }
        public InMemoryExecutionJournal Journal { get; }

        public void AssignAndStart()
        {
            InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
            candidates.SetCandidates(JobId, new[]
            {
                new JobCandidate(ResidentId, 5_000, 1, true),
            });
            JobAssignmentReport report = new AssignAvailableJobsHandler(
                JobRepository,
                candidates,
                Journal,
                haulingResidentSlotClaims: new HaulingResidentSlotClaimService(
                    InventoryRepository,
                    Journal)).Handle(new AssignAvailableJobsCommand(tick: 2));
            Assert.Single(report.Assignments);
            Assert.True(Jobs.Start(JobId, tick: 2).IsSuccess);
        }

        public Result Acquire(EntityId destinationStackId, long tick)
        {
            return new AcquireHaulingItemHandler(
                InventoryRepository,
                JobRepository,
                Journal).Handle(new AcquireHaulingItemCommand(
                    JobId,
                    destinationStackId,
                    tick));
        }

        public Result Complete(EntityId destinationStackId, long tick)
        {
            return new CompleteHaulingJobHandler(
                InventoryRepository,
                StorageRepository,
                JobRepository,
                Journal,
                AgentSkillGrantTestFactory.Create(ResidentId, Journal))
                .Handle(new CompleteHaulingJobCommand(
                    JobId,
                    destinationStackId,
                    tick));
        }
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
