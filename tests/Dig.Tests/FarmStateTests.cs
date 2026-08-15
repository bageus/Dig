using System.Linq;
using Dig.Domain.Farming;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmStateTests
{
    [Fact]
    public void Mushroom_mode_starts_without_seed_delivery_and_maintains_three_growth_slots()
    {
        FarmState farm = new FarmState();

        Assert.Empty(farm.GetDeliveryDemands());
        Assert.True(farm.MushroomSeedEstablished);
        Assert.Equal(3, farm.MushroomSlotsOccupied);

        Assert.True(farm.HarvestMushroom());
        Assert.Equal(2, farm.MushroomSlotsOccupied);

        FarmAdvanceResult advance = farm.Advance(1);
        Assert.Equal(1, advance.MushroomsRegrown);
        Assert.Equal(3, farm.MushroomSlotsOccupied);
    }

    [Fact]
    public void Switching_from_mushrooms_keeps_existing_plants_and_stops_new_regrowth()
    {
        FarmState farm = new FarmState();
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
    public void Switching_back_to_mushrooms_resumes_growth_without_delivery()
    {
        FarmState farm = new FarmState();
        farm.SwitchMode(FarmMode.Hamsters, 1);
        farm.SwitchMode(FarmMode.Mushrooms, 2);

        Assert.Empty(farm.GetDeliveryDemands());
        Assert.Equal(0, farm.MushroomSlotsOccupied);

        FarmAdvanceResult advance = farm.Advance(3);

        Assert.Equal(FarmOperationPolicy.MushroomGrowthSlots, advance.MushroomsRegrown);
        Assert.Equal(FarmOperationPolicy.MushroomGrowthSlots, farm.MushroomSlotsOccupied);
    }

    [Fact]
    public void One_hamster_starts_breeding_and_each_hamster_reproduces_every_two_hours()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        FarmDeliveryDemand[] demands = farm.GetDeliveryDemands().ToArray();
        Assert.Contains(
            demands,
            value => value.Kind == FarmDeliveryKind.Hamster && value.Quantity == 1);
        Assert.Contains(
            demands,
            value => value.Kind == FarmDeliveryKind.MushroomFeed
                && value.Quantity == FarmOperationPolicy.FeedCapacity);

        farm.Deliver(FarmDeliveryKind.Hamster, 1, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, FarmOperationPolicy.FeedCapacity, 0);
        Assert.Equal(0, farm.AvailableHamsters);

        Assert.Equal(0, farm.Advance(GameTimeCadence.TicksFromHours(1)).HamstersBorn);
        Assert.Equal(1, farm.Advance(GameTimeCadence.TicksFromHours(2)).HamstersBorn);
        Assert.Equal(2, farm.HamsterCount);
        Assert.Equal(1, farm.AvailableHamsters);

        Assert.Equal(2, farm.Advance(GameTimeCadence.TicksFromHours(4)).HamstersBorn);
        Assert.Equal(4, farm.HamsterCount);

        Assert.True(farm.CollectHamster());
        Assert.Equal(3, farm.HamsterCount);
    }

    [Fact]
    public void One_grub_doubles_population_hourly_until_capacity()
    {
        FarmState farm = new FarmState(FarmMode.Grubs);
        farm.Deliver(FarmDeliveryKind.Grub, 1, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, FarmOperationPolicy.FeedCapacity, 0);

        FarmAdvanceResult first = farm.Advance(GameTimeCadence.TicksFromHours(1));
        Assert.Equal(1, first.GrubsBorn);
        Assert.Equal(2, farm.GrubCount);

        FarmAdvanceResult second = farm.Advance(GameTimeCadence.TicksFromHours(2));
        Assert.Equal(2, second.GrubsBorn);
        Assert.Equal(4, farm.GrubCount);

        FarmAdvanceResult third = farm.Advance(GameTimeCadence.TicksFromHours(3));
        Assert.Equal(4, third.GrubsBorn);
        Assert.Equal(FarmOperationPolicy.AnimalCapacity, farm.GrubCount);
        Assert.Equal(
            FarmOperationPolicy.AnimalCapacity - FarmOperationPolicy.GrubBreederReserve,
            farm.AvailableGrubs);
    }

    [Fact]
    public void Every_animal_consumes_one_mushroom_cap_each_half_day()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, FarmOperationPolicy.AnimalCapacity, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, FarmOperationPolicy.FeedCapacity, 0);
        Assert.Equal(FarmOperationPolicy.AnimalCapacity, farm.FeedCount);

        FarmAdvanceResult halfDay = farm.Advance(FarmOperationPolicy.FeedConsumptionTicks);

        Assert.Equal(FarmOperationPolicy.AnimalCapacity, halfDay.FeedConsumed);
        Assert.Equal(0, farm.FeedCount);
        Assert.Contains(
            farm.GetDeliveryDemands(),
            value => value.Kind == FarmDeliveryKind.MushroomFeed
                && value.Quantity == FarmOperationPolicy.FeedCapacity);
    }

    [Fact]
    public void Reproduction_continues_when_feed_is_missing_because_starvation_rules_are_not_part_of_731()
    {
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 1, 0);

        FarmAdvanceResult advance = farm.Advance(FarmOperationPolicy.HamsterReproductionTicks);

        Assert.Equal(1, advance.HamstersBorn);
        Assert.Equal(2, farm.HamsterCount);
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
        Assert.Contains(
            demands,
            value => value.Kind == FarmDeliveryKind.MushroomFeed
                && value.Quantity == FarmOperationPolicy.FeedCapacity);

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

    [Fact]
    public void Legacy_mushroom_snapshot_migrates_to_mode_driven_growth()
    {
        FarmSnapshot legacy = new FarmSnapshot(
            FarmMode.Mushrooms,
            mushroomSeedEstablished: false,
            mushroomSlotsOccupied: 0,
            residualMushrooms: 0,
            hamsterCount: 0,
            grubCount: 0,
            feedCount: 0,
            nextReproductionTick: -1,
            nextFeedConsumptionTick: -1);

        FarmState restored = FarmState.Restore(legacy);

        Assert.True(restored.MushroomSeedEstablished);
        Assert.Equal(FarmOperationPolicy.MushroomGrowthSlots, restored.Advance(1).MushroomsRegrown);
    }
}

}
