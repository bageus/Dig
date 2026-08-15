using System.Linq;
using Dig.Domain.Farming;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmStateTests
{
    [Fact]
    public void Mushroom_mode_requests_one_seed_and_maintains_three_growth_slots()
    {
        FarmState farm = new FarmState();

        FarmDeliveryDemand demand = Assert.Single(farm.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.MushroomSeed, demand.Kind);
        Assert.Equal(1, demand.Quantity);

        farm.Deliver(FarmDeliveryKind.MushroomSeed, 1, tick: 0);
        Assert.Equal(3, farm.MushroomSlotsOccupied);
        Assert.Empty(farm.GetDeliveryDemands());
        Assert.True(farm.HarvestMushroom());
        Assert.Equal(2, farm.MushroomSlotsOccupied);

        FarmAdvanceResult advance = farm.Advance(1);
        Assert.Equal(1, advance.MushroomsRegrown);
        Assert.Equal(3, farm.MushroomSlotsOccupied);
    }

    [Fact]
    public void Switching_from_mushrooms_detaches_existing_plants_and_stops_regrowth()
    {
        FarmState farm = new FarmState();
        farm.Deliver(FarmDeliveryKind.MushroomSeed, 1, 0);
        farm.HarvestMushroom();

        FarmModeTransition transition = farm.SwitchMode(FarmMode.Hamsters, 1);

        Assert.Equal(2, transition.DetachedMushrooms);
        Assert.Equal(0, farm.MushroomSlotsOccupied);
        Assert.Equal(2, farm.ResidualMushrooms);
        Assert.Equal(0, farm.Advance(2).MushroomsRegrown);
        Assert.True(farm.HarvestMushroom());
        Assert.Equal(1, farm.ResidualMushrooms);
    }

    [Fact]
    public void Hamster_mode_protects_two_breeders_and_reproduces_every_two_hours_when_fed()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        Assert.Equal(
            new[] { FarmDeliveryKind.Hamster, FarmDeliveryKind.MushroomFeed },
            farm.GetDeliveryDemands().Select(value => value.Kind).ToArray());

        farm.Deliver(FarmDeliveryKind.Hamster, 2, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 0);
        Assert.Equal(0, farm.AvailableHamsters);

        Assert.Equal(0, farm.Advance(GameTimeCadence.TicksFromHours(1)).HamstersBorn);
        Assert.Equal(1, farm.Advance(GameTimeCadence.TicksFromHours(2)).HamstersBorn);
        Assert.Equal(3, farm.HamsterCount);
        Assert.Equal(1, farm.AvailableHamsters);
        Assert.True(farm.CollectHamster());
        Assert.Equal(2, farm.HamsterCount);
        Assert.False(farm.CollectHamster());
    }

    [Fact]
    public void Grub_mode_reproduces_hourly_but_never_exceeds_eight()
    {
        FarmState farm = new FarmState(FarmMode.Grubs);
        farm.Deliver(FarmDeliveryKind.Grub, 1, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 0);

        long tick = 0;
        for (int index = 0; index < 10; index++)
        {
            tick += GameTimeCadence.TicksFromHours(1);
            farm.Advance(tick);
        }

        Assert.Equal(FarmOperationPolicy.AnimalCapacity, farm.GrubCount);
        Assert.Equal(
            FarmOperationPolicy.AnimalCapacity - FarmOperationPolicy.GrubBreederReserve,
            farm.AvailableGrubs);
    }

    [Fact]
    public void Feed_is_capped_at_two_consumed_each_half_day_and_shortage_blocks_reproduction()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 2, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 5, 0);
        Assert.Equal(2, farm.FeedCount);

        long halfDay = FarmOperationPolicy.FeedConsumptionTicks;
        FarmAdvanceResult first = farm.Advance(halfDay);
        Assert.Equal(1, first.FeedConsumed);
        Assert.Equal(1, farm.FeedCount);
        Assert.Contains(
            farm.GetDeliveryDemands(),
            value => value.Kind == FarmDeliveryKind.MushroomFeed && value.Quantity == 1);

        farm.Advance(halfDay * 2);
        Assert.Equal(0, farm.FeedCount);
        int population = farm.HamsterCount;
        FarmAdvanceResult starved = farm.Advance(halfDay * 2 + GameTimeCadence.TicksFromHours(2));
        Assert.Equal(0, starved.HamstersBorn);
        Assert.Equal(population, farm.HamsterCount);
    }

    [Fact]
    public void Switching_animal_mode_releases_stock_immediately_but_animals_escape_gradually()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 4, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 0);

        FarmModeTransition transition = farm.SwitchMode(FarmMode.Grubs, 3);

        Assert.Equal(4, transition.ReleasedHamsters);
        Assert.Equal(2, transition.ReleasedFeed);
        Assert.Equal(0, farm.HamsterCount);
        Assert.Equal(4, farm.EscapingHamsterCount);
        Assert.Equal(0, farm.FeedCount);
        Assert.False(farm.CollectHamster());

        FarmDeliveryDemand[] demands = farm.GetDeliveryDemands().ToArray();
        Assert.Contains(demands, value => value.Kind == FarmDeliveryKind.Grub && value.Quantity == 1);
        Assert.Contains(demands, value => value.Kind == FarmDeliveryKind.MushroomFeed && value.Quantity == 2);

        FarmAdvanceResult firstEscape = farm.Advance(4);
        Assert.Equal(1, firstEscape.HamstersEscaped);
        Assert.Equal(3, farm.EscapingHamsterCount);

        FarmAdvanceResult remainingEscape = farm.Advance(7);
        Assert.Equal(3, remainingEscape.HamstersEscaped);
        Assert.Equal(0, farm.EscapingHamsterCount);
    }

    [Fact]
    public void Escaping_animals_survive_snapshot_round_trip()
    {
        FarmState farm = new FarmState(FarmMode.Grubs);
        farm.Deliver(FarmDeliveryKind.Grub, 3, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 0);
        farm.SwitchMode(FarmMode.Mushrooms, 10);

        FarmState restored = FarmState.Restore(farm.CreateSnapshot());

        Assert.Equal(3, restored.EscapingGrubCount);
        Assert.Equal(1, restored.Advance(11).GrubsEscaped);
        Assert.Equal(2, restored.EscapingGrubCount);
    }

    [Fact]
    public void Snapshot_round_trip_preserves_operational_state()
    {
        FarmState farm = new FarmState(FarmMode.Grubs);
        farm.Deliver(FarmDeliveryKind.Grub, 1, 10);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 10);
        farm.Advance(10 + GameTimeCadence.TicksFromHours(1));

        FarmState restored = FarmState.Restore(farm.CreateSnapshot());

        Assert.Equal(farm.Mode, restored.Mode);
        Assert.Equal(farm.GrubCount, restored.GrubCount);
        Assert.Equal(farm.FeedCount, restored.FeedCount);
        Assert.Equal(farm.AvailableGrubs, restored.AvailableGrubs);
    }
}

}
