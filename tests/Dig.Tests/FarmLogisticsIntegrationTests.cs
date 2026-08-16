using Dig.Application.Farming;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmLogisticsIntegrationTests
{
    [Fact]
    public void Synchronization_creates_physical_delivery_and_does_not_duplicate_it()
    {
        EntityId farmId = Id(1);
        EntityId sourceId = Id(2);
        EntityId secondSourceId = Id(5);
        EntityId firstJobId = Id(3);
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        farms.Save(farmId, new FarmState(FarmMode.Hamsters));
        ItemCatalog catalog = new ItemCatalog(LivingMaterialContent.CreateItems());
        InventoryState inventory = new InventoryState(catalog);
        Assert.True(inventory.AddUnit(
            sourceId,
            LivingMaterialContent.HamsterItemId,
            ItemLocation.InWorld(new CellId(4, 4)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddUnit(
            secondSourceId,
            LivingMaterialContent.HamsterItemId,
            ItemLocation.InWorld(new CellId(5, 4)),
            tick: 0).IsSuccess);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobRepository =
            new InMemoryJobRepository(new JobSystem());
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        SynchronizeFarmLogisticsHandler handler = new SynchronizeFarmLogisticsHandler(
            farms,
            inventoryRepository,
            jobRepository,
            FarmItemCatalog.Default,
            reservations,
            new FixedIds(firstJobId, Id(4), Id(6)),
            new InMemoryExecutionJournal());

        Result<FarmLogisticsSynchronizationReport> first = handler.Handle(
            new SynchronizeFarmLogisticsCommand(
                new[] { new CellId(4, 4), new CellId(5, 4) }, 650, 8, tick: 1));
        Result<FarmLogisticsSynchronizationReport> second = handler.Handle(
            new SynchronizeFarmLogisticsCommand(
                new[] { new CellId(4, 4), new CellId(5, 4) }, 650, 8, tick: 2));

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(2, first.Value.Created.Count);
        Assert.All(first.Value.Created, plan => Assert.Equal(1, plan.Quantity));
        Assert.Empty(second.Value.Created);
        JobSnapshot job = jobRepository.Get().Get(firstJobId)!;
        HaulJobDefinition haul = Assert.IsType<HaulJobDefinition>(job.Definition);
        Assert.Equal(ItemLocation.InBuilding(farmId), haul.Destination);
        Assert.Equal(1, inventoryRepository.Get().GetStack(sourceId)!.ReservedQuantity);
        Assert.Equal(1, inventoryRepository.Get().GetStack(secondSourceId)!.ReservedQuantity);
        Assert.Equal(2, reservations.GetReserved(
            farmId,
            FarmDeliveryKind.Hamster,
            FarmLogisticsDirection.Incoming));
    }

    [Fact]
    public void Output_synchronization_preserves_breeders_and_creates_one_physical_job()
    {
        EntityId farmId = Id(10);
        EntityId outputStackId = Id(11);
        EntityId outputJobId = Id(12);
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 2, tick: 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 1, tick: 0);
        farm.Advance(FarmOperationPolicy.HamsterReproductionTicks);
        Assert.Equal(3, farm.HamsterCount);
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        farms.Save(farmId, farm);
        InventoryState inventory = new InventoryState(
            new ItemCatalog(LivingMaterialContent.CreateItems()));
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobRepository =
            new InMemoryJobRepository(new JobSystem());
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        SynchronizeFarmOutputsHandler handler = new SynchronizeFarmOutputsHandler(
            farms,
            inventoryRepository,
            jobRepository,
            FarmItemCatalog.Default,
            reservations,
            new FixedIds(outputStackId, outputJobId, Id(13)),
            new InMemoryExecutionJournal());
        FarmLogisticsSite site = new FarmLogisticsSite(
            farmId,
            new CellId(7, 7),
            new CellId(8, 7));

        Result<FarmLogisticsSynchronizationReport> first = handler.Handle(
            new SynchronizeFarmOutputsCommand(new[] { site }, 650, 8, tick: 1));
        Result<FarmLogisticsSynchronizationReport> second = handler.Handle(
            new SynchronizeFarmOutputsCommand(new[] { site }, 650, 8, tick: 2));

        Assert.True(first.IsSuccess, first.Error?.ToString());
        FarmLogisticsJobPlan plan = Assert.Single(first.Value.Created);
        Assert.Equal(outputStackId, plan.SourceStackId);
        Assert.Equal(2, farms.Get(farmId)!.HamsterCount);
        ItemStackSnapshot output = inventoryRepository.Get().GetStack(outputStackId)!;
        Assert.Equal(ItemLocation.InBuilding(farmId), output.Location);
        Assert.Equal(1, output.ReservedQuantity);
        HaulJobDefinition haul = Assert.IsType<HaulJobDefinition>(
            jobRepository.Get().Get(outputJobId)!.Definition);
        Assert.Equal(ItemLocation.InWorld(site.OutputCell), haul.Destination);
        Assert.Empty(second.Value.Created);
        Assert.Equal(2, farms.Get(farmId)!.HamsterCount);
    }

    [Fact]
    public void Mode_switch_cancels_obsolete_delivery_and_releases_item_reservation()
    {
        EntityId farmId = Id(20);
        EntityId capStackId = Id(21);
        EntityId deliveryJobId = Id(22);
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        FarmState farm = new FarmState(FarmMode.Mushrooms);
        farms.Save(farmId, farm);
        InventoryState inventory = new InventoryState(
            new ItemCatalog(LivingMaterialContent.CreateItems().Concat(new[]
            {
                new ItemDefinition(
                    CampfireProductionContent.MushroomCapItemId,
                    "Mushroom cap",
                    maximumStackSize: 100,
                    isTool: false),
            })));
        Assert.True(inventory.AddUnit(
            capStackId,
            CampfireProductionContent.MushroomCapItemId,
            ItemLocation.InWorld(new CellId(2, 2)),
            tick: 0).IsSuccess);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobRepository =
            new InMemoryJobRepository(new JobSystem());
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        SynchronizeFarmLogisticsHandler handler = new SynchronizeFarmLogisticsHandler(
            farms,
            inventoryRepository,
            jobRepository,
            FarmItemCatalog.Default,
            reservations,
            new FixedIds(deliveryJobId, Id(23)),
            new InMemoryExecutionJournal());

        Result<FarmLogisticsSynchronizationReport> planned = handler.Handle(
            new SynchronizeFarmLogisticsCommand(
                new[] { new CellId(2, 2) }, 650, 8, tick: 1));
        Assert.Single(planned.Value.Created);
        Assert.Equal(1, inventoryRepository.Get().GetStack(capStackId)!.ReservedQuantity);

        farm.SwitchMode(FarmMode.Hamsters, tick: 2);
        farms.Save(farmId, farm);
        Result<FarmLogisticsSynchronizationReport> reconciled = handler.Handle(
            new SynchronizeFarmLogisticsCommand(
                new[] { new CellId(2, 2) }, 650, 8, tick: 3));

        Assert.True(reconciled.IsSuccess, reconciled.Error?.ToString());
        Assert.Equal(1, reconciled.Value.ReleasedReservations);
        Assert.Equal(JobStatus.Cancelled, jobRepository.Get().Get(deliveryJobId)!.Status);
        Assert.Equal(0, inventoryRepository.Get().GetStack(capStackId)!.ReservedQuantity);
        Assert.Empty(reservations.GetAll());
    }

    [Fact]
    public void Removed_farm_cancels_delivery_before_releasing_its_reservation()
    {
        EntityId farmId = Id(30);
        EntityId capStackId = Id(31);
        EntityId deliveryJobId = Id(32);
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        farms.Save(farmId, new FarmState(FarmMode.Mushrooms));
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                CampfireProductionContent.MushroomCapItemId,
                "Mushroom cap",
                maximumStackSize: 100,
                isTool: false),
        }));
        Assert.True(inventory.AddUnit(
            capStackId,
            CampfireProductionContent.MushroomCapItemId,
            ItemLocation.InWorld(new CellId(3, 3)),
            tick: 0).IsSuccess);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobRepository =
            new InMemoryJobRepository(new JobSystem());
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        SynchronizeFarmLogisticsHandler handler = new SynchronizeFarmLogisticsHandler(
            farms,
            inventoryRepository,
            jobRepository,
            FarmItemCatalog.Default,
            reservations,
            new FixedIds(deliveryJobId, Id(33)),
            new InMemoryExecutionJournal());
        SynchronizeFarmLogisticsCommand command = new SynchronizeFarmLogisticsCommand(
            new[] { new CellId(3, 3) }, 650, 8, tick: 1);
        Assert.Single(handler.Handle(command).Value.Created);

        farms.Remove(farmId);
        Result<FarmLogisticsSynchronizationReport> reconciled = handler.Handle(
            new SynchronizeFarmLogisticsCommand(
                new[] { new CellId(3, 3) }, 650, 8, tick: 2));

        Assert.True(reconciled.IsSuccess, reconciled.Error?.ToString());
        Assert.Equal(1, reconciled.Value.ReleasedReservations);
        Assert.Equal(JobStatus.Cancelled, jobRepository.Get().Get(deliveryJobId)!.Status);
        Assert.Equal(0, inventoryRepository.Get().GetStack(capStackId)!.ReservedQuantity);
        Assert.Empty(reservations.GetAll());
    }

    private static EntityId Id(int value) => EntityId.Parse(value.ToString("x32"));

    private sealed class FixedIds : IFarmLogisticsJobIdSource
    {
        private readonly EntityId[] _ids;
        private int _index;

        public FixedIds(params EntityId[] ids)
        {
            _ids = ids;
        }

        public EntityId NextJobId() => _ids[_index++];

        public EntityId NextStackId() => _ids[_index++];
    }
}

}
