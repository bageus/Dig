using Dig.Application.Farming;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmLogisticsReservationsTests
{
    [Fact]
    public void Incoming_reservation_reduces_unreserved_demand_and_blocks_duplicate_work()
    {
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        EntityId farmId = EntityId.New();
        EntityId firstJob = EntityId.New();
        EntityId duplicateJob = EntityId.New();

        Assert.True(reservations.TryReserveIncoming(
            firstJob,
            farmId,
            FarmDeliveryKind.MushroomFeed,
            demandedQuantity: 1,
            quantity: 1));

        Assert.Equal(
            0,
            reservations.GetUnreservedIncoming(
                farmId,
                FarmDeliveryKind.MushroomFeed,
                demandedQuantity: 1));
        Assert.False(reservations.TryReserveIncoming(
            duplicateJob,
            farmId,
            FarmDeliveryKind.MushroomFeed,
            demandedQuantity: 1,
            quantity: 1));
    }

    [Fact]
    public void Releasing_failed_job_makes_farm_demand_available_again()
    {
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        EntityId farmId = EntityId.New();
        EntityId jobId = EntityId.New();

        Assert.True(reservations.TryReserveIncoming(
            jobId,
            farmId,
            FarmDeliveryKind.Hamster,
            demandedQuantity: 2,
            quantity: 2));
        Assert.True(reservations.Release(jobId));

        Assert.Equal(
            2,
            reservations.GetUnreservedIncoming(
                farmId,
                FarmDeliveryKind.Hamster,
                demandedQuantity: 2));
    }

    [Fact]
    public void Outgoing_reservations_keep_breeding_surplus_from_being_queued_twice()
    {
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        EntityId farmId = EntityId.New();
        EntityId firstJob = EntityId.New();
        EntityId secondJob = EntityId.New();

        Assert.True(reservations.TryReserveOutgoing(
            firstJob,
            farmId,
            FarmDeliveryKind.Grub,
            collectableQuantity: 3,
            quantity: 2));

        Assert.Equal(
            1,
            reservations.GetUnreservedOutgoing(
                farmId,
                FarmDeliveryKind.Grub,
                collectableQuantity: 3));
        Assert.False(reservations.TryReserveOutgoing(
            secondJob,
            farmId,
            FarmDeliveryKind.Grub,
            collectableQuantity: 3,
            quantity: 2));
        Assert.True(reservations.TryReserveOutgoing(
            secondJob,
            farmId,
            FarmDeliveryKind.Grub,
            collectableQuantity: 3,
            quantity: 1));
    }

    [Fact]
    public void Removing_farm_releases_all_its_incoming_and_outgoing_jobs()
    {
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        EntityId farmId = EntityId.New();

        Assert.True(reservations.TryReserveIncoming(
            EntityId.New(),
            farmId,
            FarmDeliveryKind.MushroomSeed,
            demandedQuantity: 1,
            quantity: 1));
        Assert.True(reservations.TryReserveOutgoing(
            EntityId.New(),
            farmId,
            FarmDeliveryKind.Hamster,
            collectableQuantity: 1,
            quantity: 1));

        Assert.Equal(2, reservations.ReleaseForFarm(farmId));
        Assert.Empty(reservations.GetAll());
    }
}

}
