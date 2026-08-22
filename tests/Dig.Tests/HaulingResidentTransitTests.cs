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
    private static readonly ItemId FillerId = new ItemId("material.filler");

    [Fact]
    public void Acquisition_uses_one_cargo_slot_per_unit_and_deposit_restores_full_speed()
    {
        Harness harness = new Harness(existingCargoUnits: 0, haulQuantity: 4);
        harness.AssignAndStart();
        Assert.Equal(1d, harness.Inventory.GetResidentMoveSpeedMultiplier(ResidentId));

        Result acquired = harness.Acquire(Id(20), tick: 3);

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        Assert.Empty(harness.Inventory.GetResidentSlotClaims(JobId));
        ItemStackSnapshot[] cargo = ResidentStacks(harness.Inventory)
            .Where(stack => stack.ItemId == OreId)
            .Where(stack => stack.Location.ResidentCompartment
                == ResidentInventoryCompartment.Cargo)
            .ToArray();
        Assert.Equal(4, cargo.Length);
        Assert.All(cargo, stack =>
        {
            Assert.Equal(1, stack.Quantity);
            Assert.Equal(1, stack.ReservedQuantity);
        });
        Assert.Equal(0.75d, harness.Inventory.GetResidentMoveSpeedMultiplier(ResidentId));
        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 4).IsSuccess);

        Result completed = harness.Complete(Id(21), tick: 5);

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(0.75d, harness.Inventory.GetResidentMoveSpeedMultiplier(ResidentId));
        Assert.Equal(4, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InStorage(StorageId)));
        Assert.Equal(14, harness.Inventory.GetTotal(OreId));
    }

    [Fact]
    public void Free_main_slots_are_used_before_free_cargo_slots()
    {
        Harness harness = new Harness(
            existingCargoUnits: 1,
            haulQuantity: 4,
            fillMain: false);
        harness.AssignAndStart();

        Result acquired = harness.Acquire(Id(20), tick: 3);

        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        ItemStackSnapshot[] residentOre = ResidentStacks(harness.Inventory)
            .Where(stack => stack.ItemId == OreId)
            .ToArray();
        Assert.DoesNotContain(
            residentOre,
            stack => stack.Location.ResidentCompartment
                == ResidentInventoryCompartment.Cargo);
        ItemStackSnapshot[] main = residentOre
            .Where(stack => stack.Location.ResidentCompartment
                == ResidentInventoryCompartment.Main)
            .OrderBy(stack => stack.Location.ResidentSlotIndex)
            .ToArray();
        Assert.Equal(5, main.Length);
        Assert.Equal(0, main[0].ReservedQuantity);
        Assert.All(main.Skip(1), stack =>
        {
            Assert.Equal(1, stack.Quantity);
            Assert.Equal(1, stack.ReservedQuantity);
        });
        Assert.Equal(6, harness.Inventory.GetStack(SourceStackId)!.Quantity);

        Assert.True(harness.Jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        Assert.True(harness.Complete(Id(21), tick: 5).IsSuccess);
        Assert.Equal(15, harness.Inventory.GetTotal(OreId));
        Assert.Equal(1, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                1)));
        Assert.Equal(4, harness.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InStorage(StorageId)));
    }

    private static ItemStackSnapshot[] ResidentStacks(InventoryState inventory)
    {
        return inventory.CreateSnapshot().Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(stack => stack.Location.HasOwner
                && stack.Location.OwnerId == ResidentId)
            .OrderBy(stack => stack.Location)
            .ThenBy(stack => stack.StackId.ToString(), System.StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class Harness
    {
        public Harness(
            int existingCargoUnits,
            int haulQuantity,
            bool fillMain = true)
        {
            HaulQuantity = haulQuantity;
            ItemCategoryId raw = new ItemCategoryId("raw");
            Inventory = new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
                new ItemDefinition(FillerId, "Filler", 1, false, new[] { raw }),
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
            if (fillMain)
            {
                for (int slot = 1; slot < 6; slot++)
                {
                    Assert.True(Inventory.AddStack(
                        Id(30 + slot),
                        FillerId,
                        1,
                        ItemLocation.InResidentSlot(
                            ResidentId,
                            ResidentInventoryCompartment.Main,
                            slot),
                        tick: 0).IsSuccess);
                }
            }

            for (int slot = 0; slot < existingCargoUnits; slot++)
            {
                Assert.True(Inventory.AddStack(
                    Id(100 + slot),
                    OreId,
                    1,
                    ItemLocation.InResidentSlot(
                        ResidentId,
                        ResidentInventoryCompartment.Cargo,
                        slot),
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
