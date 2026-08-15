using System;
using System.Collections.Generic;

namespace Dig.Domain.Farming
{

public sealed partial class FarmState
{
    private void EnableMushroomGrowth(bool fillSlotsImmediately)
    {
        _mushroomSeedEstablished = true;
        if (fillSlotsImmediately)
        {
            _mushroomSlotsOccupied = FarmOperationPolicy.MushroomGrowthSlots;
        }
    }

    private void ScheduleReproductionIfReady(long tick)
    {
        int reserve = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterBreederReserve
            : FarmOperationPolicy.GrubBreederReserve;
        int population = Mode == FarmMode.Hamsters ? _hamsterCount : _grubCount;
        if (population < reserve || _nextReproductionTick >= 0) return;
        long interval = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterReproductionTicks
            : FarmOperationPolicy.GrubReproductionTicks;
        _nextReproductionTick = checked(tick + interval);
    }

    private void ScheduleFeedingIfReady(long tick)
    {
        int population = Mode == FarmMode.Hamsters ? _hamsterCount : _grubCount;
        if (population <= 0 || _nextFeedConsumptionTick >= 0)
        {
            return;
        }

        long interval = FarmOperationPolicy.FeedConsumptionTicks;
        long remainder = tick % interval;
        _nextFeedConsumptionTick = remainder == 0
            ? tick
            : checked(tick + (interval - remainder));
    }

    private void ScheduleEscapeIfNeeded(long tick)
    {
        if (_nextEscapeTick >= 0
            || (_escapingHamsterCount <= 0 && _escapingGrubCount <= 0))
        {
            return;
        }

        _nextEscapeTick = checked(tick + 1);
    }

    private void AdvanceEscapingAnimals(
        long tick,
        out int hamstersEscaped,
        out int grubsEscaped)
    {
        hamstersEscaped = 0;
        grubsEscaped = 0;
        if (_nextEscapeTick < 0 || tick < _nextEscapeTick)
        {
            return;
        }

        long elapsedTicks = tick - _nextEscapeTick;
        int escapeSlots = elapsedTicks >= int.MaxValue - 1L
            ? int.MaxValue
            : checked((int)elapsedTicks + 1);

        hamstersEscaped = Math.Min(_escapingHamsterCount, escapeSlots);
        _escapingHamsterCount -= hamstersEscaped;
        escapeSlots -= hamstersEscaped;

        grubsEscaped = Math.Min(_escapingGrubCount, escapeSlots);
        _escapingGrubCount -= grubsEscaped;

        int escaped = checked(hamstersEscaped + grubsEscaped);
        if (_escapingHamsterCount <= 0 && _escapingGrubCount <= 0)
        {
            _nextEscapeTick = -1;
        }
        else
        {
            _nextEscapeTick = checked(_nextEscapeTick + escaped);
        }
    }

    private void RequireMode(FarmMode expected)
    {
        if (Mode != expected)
            throw new InvalidOperationException("The delivery does not match the active farm mode.");
    }

    private static void AddIfPositive(
        ICollection<FarmDeliveryDemand> values,
        FarmDeliveryKind kind,
        int quantity)
    {
        if (quantity > 0) values.Add(new FarmDeliveryDemand(kind, quantity));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
    }

    private static void ValidateSnapshot(FarmSnapshot snapshot)
    {
        if (snapshot.MushroomSlotsOccupied < 0
            || snapshot.MushroomSlotsOccupied > FarmOperationPolicy.MushroomGrowthSlots
            || snapshot.ResidualMushrooms < 0
            || snapshot.HamsterCount < 0
            || snapshot.HamsterCount > FarmOperationPolicy.AnimalCapacity
            || snapshot.GrubCount < 0
            || snapshot.GrubCount > FarmOperationPolicy.AnimalCapacity
            || snapshot.FeedCount < 0
            || snapshot.FeedCount > FarmOperationPolicy.FeedCapacity
            || snapshot.EscapingHamsterCount < 0
            || snapshot.EscapingGrubCount < 0
            || snapshot.NextReproductionTick < -1
            || snapshot.NextFeedConsumptionTick < -1
            || snapshot.NextEscapeTick < -1)
        {
            throw new ArgumentException("Farm snapshot contains invalid values.", nameof(snapshot));
        }
    }
}

}
