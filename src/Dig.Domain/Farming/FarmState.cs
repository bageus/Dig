using System;
using System.Collections.Generic;

namespace Dig.Domain.Farming
{

public sealed partial class FarmState
{
    private bool _mushroomSeedEstablished;
    private int _mushroomSlotsOccupied;
    private int _residualMushrooms;
    private int _hamsterCount;
    private int _grubCount;
    private int _feedCount;
    private int _escapingHamsterCount;
    private int _escapingGrubCount;
    private long _nextReproductionTick = -1;
    private long _nextFeedConsumptionTick = -1;
    private long _nextEscapeTick = -1;

    public FarmState(FarmMode initialMode = FarmMode.Mushrooms)
    {
        ValidateMode(initialMode, nameof(initialMode));
        Mode = initialMode;
    }

    public FarmMode Mode { get; private set; }

    public bool MushroomSeedEstablished => _mushroomSeedEstablished;
    public int MushroomSlotsOccupied => _mushroomSlotsOccupied;
    public int ResidualMushrooms => _residualMushrooms;
    public int HamsterCount => _hamsterCount;
    public int GrubCount => _grubCount;
    public int FeedCount => _feedCount;
    public int EscapingHamsterCount => _escapingHamsterCount;
    public int EscapingGrubCount => _escapingGrubCount;

    public int AvailableHamsters => Math.Max(
        0,
        _hamsterCount - FarmOperationPolicy.HamsterBreederReserve);

    public int AvailableGrubs => Math.Max(
        0,
        _grubCount - FarmOperationPolicy.GrubBreederReserve);

    public IReadOnlyList<FarmDeliveryDemand> GetDeliveryDemands()
    {
        List<FarmDeliveryDemand> demands = new List<FarmDeliveryDemand>(2);
        switch (Mode)
        {
            case FarmMode.Mushrooms:
                if (!_mushroomSeedEstablished)
                {
                    demands.Add(new FarmDeliveryDemand(FarmDeliveryKind.MushroomSeed, 1));
                }
                break;
            case FarmMode.Hamsters:
            {
                int missingStarters = FarmOperationPolicy.HamsterBreederReserve - _hamsterCount;
                AddIfPositive(demands, FarmDeliveryKind.Hamster, missingStarters);
                if (missingStarters <= 0)
                {
                    AddIfPositive(
                        demands,
                        FarmDeliveryKind.MushroomFeed,
                        FarmOperationPolicy.FeedCapacity - _feedCount);
                }
                break;
            }
            case FarmMode.Grubs:
            {
                int missingStarters = FarmOperationPolicy.GrubBreederReserve - _grubCount;
                AddIfPositive(demands, FarmDeliveryKind.Grub, missingStarters);
                if (missingStarters <= 0)
                {
                    AddIfPositive(
                        demands,
                        FarmDeliveryKind.MushroomFeed,
                        FarmOperationPolicy.FeedCapacity - _feedCount);
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return demands;
    }

    public FarmModeTransition SwitchMode(FarmMode mode, long tick)
    {
        ValidateTick(tick);
        ValidateMode(mode, nameof(mode));
        if (mode == Mode)
        {
            return new FarmModeTransition(Mode, Mode, 0, 0, 0, 0);
        }

        FarmMode previous = Mode;
        int detachedMushrooms = 0;
        int releasedHamsters = 0;
        int releasedGrubs = 0;
        int releasedFeed = _feedCount;
        if (previous == FarmMode.Mushrooms)
        {
            detachedMushrooms = _mushroomSlotsOccupied;
            _residualMushrooms += detachedMushrooms;
            _mushroomSlotsOccupied = 0;
            _mushroomSeedEstablished = false;
        }
        else if (previous == FarmMode.Hamsters)
        {
            releasedHamsters = _hamsterCount;
            _escapingHamsterCount = checked(_escapingHamsterCount + releasedHamsters);
            _hamsterCount = 0;
        }
        else
        {
            releasedGrubs = _grubCount;
            _escapingGrubCount = checked(_escapingGrubCount + releasedGrubs);
            _grubCount = 0;
        }

        _feedCount = 0;
        _nextFeedConsumptionTick = -1;
        _nextReproductionTick = -1;
        Mode = mode;
        ScheduleEscapeIfNeeded(tick);
        return new FarmModeTransition(
            previous,
            mode,
            detachedMushrooms,
            releasedHamsters,
            releasedGrubs,
            releasedFeed);
    }

    public void Deliver(FarmDeliveryKind kind, int quantity, long tick)
    {
        ValidateTick(tick);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        switch (kind)
        {
            case FarmDeliveryKind.MushroomSeed:
                RequireMode(FarmMode.Mushrooms);
                if (_mushroomSeedEstablished || quantity != 1)
                {
                    throw new InvalidOperationException(
                        "The mushroom seed requirement is already satisfied or has an invalid quantity.");
                }
                _mushroomSeedEstablished = true;
                _mushroomSlotsOccupied = Math.Max(
                    0,
                    FarmOperationPolicy.MushroomGrowthSlots - _residualMushrooms);
                break;
            case FarmDeliveryKind.Hamster:
                RequireMode(FarmMode.Hamsters);
                _hamsterCount = Math.Min(
                    FarmOperationPolicy.AnimalCapacity,
                    checked(_hamsterCount + quantity));
                ScheduleReproductionIfReady(tick);
                ScheduleFeedingIfReady(tick);
                break;
            case FarmDeliveryKind.Grub:
                RequireMode(FarmMode.Grubs);
                _grubCount = Math.Min(
                    FarmOperationPolicy.AnimalCapacity,
                    checked(_grubCount + quantity));
                ScheduleReproductionIfReady(tick);
                ScheduleFeedingIfReady(tick);
                break;
            case FarmDeliveryKind.MushroomFeed:
                if (Mode == FarmMode.Mushrooms)
                {
                    throw new InvalidOperationException("Mushroom mode does not use feed stock.");
                }
                _feedCount = Math.Min(
                    FarmOperationPolicy.FeedCapacity,
                    checked(_feedCount + quantity));
                ScheduleFeedingIfReady(tick);
                ScheduleReproductionIfReady(tick);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public bool HarvestMushroom()
    {
        if (_residualMushrooms > 0)
        {
            _residualMushrooms--;
            return true;
        }

        if (Mode == FarmMode.Mushrooms && _mushroomSlotsOccupied > 0)
        {
            _mushroomSlotsOccupied--;
            return true;
        }

        return false;
    }

    public bool CollectHamster()
    {
        if (Mode != FarmMode.Hamsters || AvailableHamsters <= 0) return false;
        _hamsterCount--;
        return true;
    }

    public bool CollectGrub()
    {
        if (Mode != FarmMode.Grubs || AvailableGrubs <= 0) return false;
        _grubCount--;
        return true;
    }

    public FarmAdvanceResult Advance(long tick)
    {
        ValidateTick(tick);
        AdvanceEscapingAnimals(tick, out int hamstersEscaped, out int grubsEscaped);

        int regrown = 0;
        if (Mode == FarmMode.Mushrooms)
        {
            int activeCapacity = Math.Max(
                0,
                FarmOperationPolicy.MushroomGrowthSlots - _residualMushrooms);
            if (_mushroomSeedEstablished
                && _mushroomSlotsOccupied < activeCapacity)
            {
                regrown = activeCapacity - _mushroomSlotsOccupied;
                _mushroomSlotsOccupied = activeCapacity;
            }
            return new FarmAdvanceResult(
                regrown,
                0,
                0,
                0,
                hamstersEscaped,
                grubsEscaped);
        }

        int hamstersBorn = 0;
        int grubsBorn = 0;
        int feedConsumed = 0;
        long reproductionInterval = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterReproductionTicks
            : FarmOperationPolicy.GrubReproductionTicks;
        int reserve = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterBreederReserve
            : FarmOperationPolicy.GrubBreederReserve;

        while (true)
        {
            bool feedDue = _nextFeedConsumptionTick >= 0 && tick >= _nextFeedConsumptionTick;
            bool reproductionDue = _nextReproductionTick >= 0 && tick >= _nextReproductionTick;
            if (!feedDue && !reproductionDue) break;

            if (feedDue && (!reproductionDue || _nextFeedConsumptionTick <= _nextReproductionTick))
            {
                int consumed = _feedCount > 0 ? 1 : 0;
                _feedCount -= consumed;
                feedConsumed = checked(feedConsumed + consumed);
                _nextFeedConsumptionTick = checked(
                    _nextFeedConsumptionTick + FarmOperationPolicy.FeedConsumptionTicks);
                continue;
            }

            int population = Mode == FarmMode.Hamsters ? _hamsterCount : _grubCount;
            if (_feedCount > 0
                && population >= reserve
                && population < FarmOperationPolicy.AnimalCapacity)
            {
                if (Mode == FarmMode.Hamsters)
                {
                    _hamsterCount++;
                    hamstersBorn++;
                }
                else
                {
                    _grubCount++;
                    grubsBorn++;
                }
            }

            _nextReproductionTick = checked(_nextReproductionTick + reproductionInterval);
        }

        return new FarmAdvanceResult(
            0,
            hamstersBorn,
            grubsBorn,
            feedConsumed,
            hamstersEscaped,
            grubsEscaped);
    }

    public FarmSnapshot CreateSnapshot()
    {
        return new FarmSnapshot(
            Mode,
            _mushroomSeedEstablished,
            _mushroomSlotsOccupied,
            _residualMushrooms,
            _hamsterCount,
            _grubCount,
            _feedCount,
            _nextReproductionTick,
            _nextFeedConsumptionTick,
            _escapingHamsterCount,
            _escapingGrubCount,
            _nextEscapeTick);
    }

    public static FarmState Restore(FarmSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        ValidateSnapshot(snapshot);
        return new FarmState(snapshot.Mode)
        {
            _mushroomSeedEstablished = snapshot.MushroomSeedEstablished,
            _mushroomSlotsOccupied = snapshot.MushroomSlotsOccupied,
            _residualMushrooms = snapshot.ResidualMushrooms,
            _hamsterCount = snapshot.HamsterCount,
            _grubCount = snapshot.GrubCount,
            _feedCount = snapshot.FeedCount,
            _nextReproductionTick = snapshot.NextReproductionTick,
            _nextFeedConsumptionTick = snapshot.NextFeedConsumptionTick,
            _escapingHamsterCount = snapshot.EscapingHamsterCount,
            _escapingGrubCount = snapshot.EscapingGrubCount,
            _nextEscapeTick = snapshot.NextEscapeTick,
        };
    }
}

}
