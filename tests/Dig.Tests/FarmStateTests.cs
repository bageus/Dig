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
        Assert.Equal(1, farm.Advance(1).MushroomsRegrown);
        Assert.Equal(3, farm.MushroomSlotsOccupied);
    }

    [Fact]
    public void Switching_from_mushrooms_keeps_existing_plants_but_requires_a_new_seed_when_returning()
    {
        FarmState farm = new FarmState();
        farm.Deliver(FarmDeliveryKind.MushroomSeed, 1, 0);
        farm.HarvestMushroom();

        FarmModeTransition transition = farm.SwitchMode(FarmMode.Hamsters, 1);

        Assert.Equal(2, transition.DetachedMushrooms);
        Assert.Equal(0, farm.MushroomSlotsOccupied);
        Assert.Equal(2, farm.ResidualMushrooms);
        Assert.True(farm.HarvestMushroom());
        Assert.Equal(1, farm.ResidualMushrooms);

        farm.SwitchMode(FarmMode.Mushrooms, 2);
        FarmDeliveryDemand seed = Assert.Single(farm.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.MushroomSeed, seed.Kind);
        Assert.Equal(0, farm.Advance(3).MushroomsRegrown);
    }

    [Fact]
    public void Hamster_mode_requests_two_starters_then_opens_two_slot_feeder()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);

        FarmDeliveryDemand starter = Assert.Single(farm.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.Hamster, starter.Kind);
        Assert.Equal(2, starter.Quantity);

        farm.Deliver(FarmDeliveryKind.Hamster, 2, 0);

        FarmDeliveryDemand feed = Assert.Single(farm.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.MushroomFeed, feed.Kind);
        Assert.Equal(2, feed.Quantity);
        Assert.Equal(0, farm.AvailableHamsters);
    }

    [Fact]
    public void Hamster_mode_adds_one_adult_every_two_hours_when_fed_and_protects_two_breeders()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 2, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 0);

        Assert.Equal(0, farm.Advance(GameTimeCadence.TicksFromHours(1)).HamstersBorn);
        Assert.Equal(1, farm.Advance(GameTimeCadence.TicksFromHours(2)).HamstersBorn);
        Assert.Equal(3, farm.HamsterCount);
        Assert.Equal(1, farm.AvailableHamsters);

        Assert.Equal(1, farm.Advance(GameTimeCadence.TicksFromHours(4)).HamstersBorn);
        Assert.Equal(4, farm.HamsterCount);
        Assert.True(farm.CollectHamster());
        Assert.True(farm.CollectHamster());
        Assert.False(farm.CollectHamster());
        Assert.Equal(2, farm.HamsterCount);
    }

    [Fact]
    public void Grub_mode_adds_one_adult_hourly_until_capacity_and_protects_one_breeder()
    {
        FarmState farm = new FarmState(FarmMode.Grubs);
        FarmDeliveryDemand starter = Assert.Single(farm.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.Grub, starter.Kind);
        Assert.Equal(1, starter.Quantity);

        farm.Deliver(FarmDeliveryKind.Grub, 1, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 0);

        Assert.Equal(1, farm.Advance(GameTimeCadence.TicksFromHours(1)).GrubsBorn);
        Assert.Equal(2, farm.GrubCount);
        Assert.Equal(1, farm.Advance(GameTimeCadence.TicksFromHours(2)).GrubsBorn);
        Assert.Equal(3, farm.GrubCount);

        long tick = GameTimeCadence.TicksFromHours(2);
        for (int index = 0; index < 10; index++)
        {
            tick += GameTimeCadence.TicksFromHours(1);
            farm.Advance(tick);
        }

        Assert.Equal(FarmOperationPolicy.AnimalCapacity, farm.GrubCount);
        Assert.Equal(7, farm.AvailableGrubs);
    }

    [Fact]
    public void Reproduction_is_blocked_until_food_is_available()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 2, 0);

        FarmAdvanceResult starved = farm.Advance(FarmOperationPolicy.HamsterReproductionTicks * 2);
        Assert.Equal(0, starved.HamstersBorn);
        Assert.Equal(2, farm.HamsterCount);

        long feedTick = FarmOperationPolicy.HamsterReproductionTicks * 2;
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 1, feedTick);
        Assert.Equal(
            1,
            farm.Advance(feedTick + FarmOperationPolicy.HamsterReproductionTicks).HamstersBorn);
    }

    [Fact]
    public void Feed_is_capped_at_two_and_consumed_once_per_half_day()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 2, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 5, 0);
        Assert.Equal(2, farm.FeedCount);

        long halfDay = FarmOperationPolicy.FeedConsumptionTicks;
        Assert.Equal(0, farm.Advance(halfDay - 1).FeedConsumed);
        FarmAdvanceResult first = farm.Advance(halfDay);
        Assert.Equal(1, first.FeedConsumed);
        Assert.Equal(1, farm.FeedCount);
        Assert.Contains(
            farm.GetDeliveryDemands(),
            value => value.Kind == FarmDeliveryKind.MushroomFeed && value.Quantity == 1);

        FarmAdvanceResult second = farm.Advance(halfDay * 2);
        Assert.Equal(1, second.FeedConsumed);
        Assert.Equal(0, farm.FeedCount);
    }

    [Fact]
    public void Switching_animal_mode_releases_logical_stock_and_animals_escape_gradually()
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

        FarmDeliveryDemand starter = Assert.Single(farm.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.Grub, starter.Kind);
        Assert.Equal(1, starter.Quantity);

        FarmAdvanceResult firstEscape = farm.Advance(4);
        Assert.Equal(1, firstEscape.HamstersEscaped);
        Assert.Equal(3, farm.EscapingHamsterCount);

        FarmAdvanceResult remainingEscape = farm.Advance(7);
        Assert.Equal(3, remainingEscape.HamstersEscaped);
        Assert.Equal(0, farm.EscapingHamsterCount);
    }

    [Fact]
    public void Snapshot_round_trip_preserves_operational_and_escape_state()
    {
        FarmState farm = new FarmState(FarmMode.Grubs);
        farm.Deliver(FarmDeliveryKind.Grub, 3, 10);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, 10);
        farm.SwitchMode(FarmMode.Mushrooms, 20);

        FarmState restored = FarmState.Restore(farm.CreateSnapshot());

        Assert.Equal(farm.Mode, restored.Mode);
        Assert.Equal(3, restored.EscapingGrubCount);
        Assert.Equal(1, restored.Advance(21).GrubsEscaped);
        Assert.Equal(2, restored.EscapingGrubCount);
        FarmDeliveryDemand seed = Assert.Single(restored.GetDeliveryDemands());
        Assert.Equal(FarmDeliveryKind.MushroomSeed, seed.Kind);
    }
}

}
