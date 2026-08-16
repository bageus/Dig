using Dig.Application.Farming;
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
    }
}

}
