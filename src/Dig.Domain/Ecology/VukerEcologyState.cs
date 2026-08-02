using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed partial class VukerEcologyState : AggregateRoot
{
    private readonly Dictionary<EntityId, Individual> _individuals =
        new Dictionary<EntityId, Individual>();
    private readonly Dictionary<VukerPairId, Pair> _pairs =
        new Dictionary<VukerPairId, Pair>();

    public VukerEcologyState(ulong worldSeed)
    {
        WorldSeed = worldSeed;
    }

    public ulong WorldSeed { get; }
    public long CurrentTick { get; private set; }
    public long NextPairSequence { get; private set; }
    public long Version { get; private set; }

    public VukerIndividualSnapshot? GetIndividual(EntityId id)
    {
        return _individuals.TryGetValue(id, out Individual? value)
            ? value.ToSnapshot()
            : null;
    }

    public VukerPairSnapshot? GetPair(VukerPairId id)
    {
        return _pairs.TryGetValue(id, out Pair? value)
            ? value.ToSnapshot()
            : null;
    }

    public IReadOnlyList<VukerIndividualSnapshot> GetIndividuals()
    {
        return _individuals.Values
            .Select(value => value.ToSnapshot())
            .OrderBy(value => value.Region)
            .ThenBy(value => value.EntityId.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<VukerPairSnapshot> GetPairs()
    {
        return _pairs.Values
            .Select(value => value.ToSnapshot())
            .OrderBy(value => value.PairId)
            .ToArray();
    }

    public VukerEcologySnapshot CaptureSnapshot()
    {
        return new VukerEcologySnapshot(
            WorldSeed,
            CurrentTick,
            NextPairSequence,
            Version,
            GetIndividuals(),
            GetPairs());
    }

    public Result RegisterAdult(
        EntityId id,
        VukerRegionKey region,
        CellId position,
        VukerDisposition disposition,
        long tick)
    {
        ValidateTick(tick);
        if (_individuals.ContainsKey(id))
        {
            return Result.Failure(VukerEcologyErrors.AlreadyRegistered);
        }

        Individual value = new Individual
        {
            Id = id,
            Lifecycle = VukerLifecycleStage.Adult,
            Disposition = disposition,
            Region = region,
            Position = position,
            IsAlive = true,
            BirthTick = tick,
            MaturityTick = tick,
        };
        _individuals.Add(id, value);
        Touch(value);
        Raise(new VukerRegistered(tick, id, value.Lifecycle, value.Disposition, region));
        return Result.Success();
    }

    public Result SynchronizeActor(
        EntityId id,
        VukerRegionKey region,
        CellId position,
        bool isAlive,
        long tick)
    {
        ValidateTick(tick);
        if (!_individuals.TryGetValue(id, out Individual? value))
        {
            return Result.Failure(VukerEcologyErrors.IndividualNotFound);
        }

        bool changed = value.Region != region
            || value.Position != position
            || value.IsAlive != isAlive;
        if (!changed)
        {
            return Result.Success();
        }

        value.Region = region;
        value.Position = position;
        value.IsAlive = isAlive;
        if (!isAlive)
        {
            value.KidnapReservedBy = null;
            BreakPair(value, tick, "parent_dead");
        }
        else if (value.ActivePairId.HasValue)
        {
            Pair pair = _pairs[value.ActivePairId.Value];
            Individual partner = _individuals[OtherParent(pair, id)];
            if (!partner.IsAlive || partner.Region != value.Region)
            {
                BreakPair(value, tick, "pair_region_changed");
            }
        }

        Touch(value);
        return Result.Success();
    }

    public IReadOnlyList<VukerPairSnapshot> Advance(long tick)
    {
        ValidateTick(tick);
        if (tick < CurrentTick)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        CurrentTick = tick;
        MatureChildren(tick);
        ValidateActivePairs(tick);
        FormPairs(tick);
        return _pairs.Values
            .Where(pair => pair.IsDue(tick))
            .OrderBy(pair => pair.Region)
            .ThenBy(pair => pair.Id)
            .Select(pair => pair.ToSnapshot())
            .ToArray();
    }

    public Result ReserveKidnap(EntityId childId, EntityId residentId, long tick)
    {
        ValidateTick(tick);
        if (!_individuals.TryGetValue(childId, out Individual? child))
        {
            return Result.Failure(VukerEcologyErrors.IndividualNotFound);
        }

        if (!child.IsAlive
            || child.Lifecycle != VukerLifecycleStage.Child
            || child.Disposition != VukerDisposition.Wild)
        {
            return Result.Failure(VukerEcologyErrors.KidnapUnavailable);
        }

        if (child.KidnapReservedBy.HasValue
            && child.KidnapReservedBy.Value != residentId)
        {
            return Result.Failure(VukerEcologyErrors.KidnapReservationConflict);
        }

        if (child.KidnapReservedBy == residentId)
        {
            return Result.Success();
        }

        child.KidnapReservedBy = residentId;
        Touch(child);
        Raise(new VukerKidnapReserved(tick, childId, residentId));
        return Result.Success();
    }

    public Result CancelKidnap(EntityId childId, EntityId residentId, long tick, string reasonCode)
    {
        ValidateTick(tick);
        if (!_individuals.TryGetValue(childId, out Individual? child))
        {
            return Result.Failure(VukerEcologyErrors.IndividualNotFound);
        }

        if (!child.KidnapReservedBy.HasValue)
        {
            return Result.Success();
        }

        if (child.KidnapReservedBy.Value != residentId)
        {
            return Result.Failure(VukerEcologyErrors.KidnapReservationConflict);
        }

        child.KidnapReservedBy = null;
        Touch(child);
        Raise(new VukerKidnapCancelled(tick, childId, residentId, reasonCode));
        return Result.Success();
    }

    public Result CommitTame(EntityId childId, EntityId residentId, long tick)
    {
        ValidateTick(tick);
        if (!_individuals.TryGetValue(childId, out Individual? child))
        {
            return Result.Failure(VukerEcologyErrors.IndividualNotFound);
        }

        if (!child.IsAlive
            || child.Lifecycle != VukerLifecycleStage.Child
            || child.Disposition != VukerDisposition.Wild
            || child.KidnapReservedBy != residentId)
        {
            return Result.Failure(VukerEcologyErrors.KidnapUnavailable);
        }

        child.Disposition = VukerDisposition.Tamed;
        child.KidnapReservedBy = null;
        child.TamedByResidentId = residentId;
        BreakPair(child, tick, "individual_tamed");
        Touch(child);
        Raise(new VukerTamed(tick, childId, residentId));
        return Result.Success();
    }

    public bool IsCombatEligible(EntityId id)
    {
        return _individuals.TryGetValue(id, out Individual? value)
            && value.IsAlive
            && value.Lifecycle == VukerLifecycleStage.Adult;
    }

    public bool IsTamed(EntityId id)
    {
        return _individuals.TryGetValue(id, out Individual? value)
            && value.IsAlive
            && value.Disposition == VukerDisposition.Tamed;
    }

    public bool IsWildChild(EntityId id)
    {
        return _individuals.TryGetValue(id, out Individual? value)
            && value.IsAlive
            && value.Lifecycle == VukerLifecycleStage.Child
            && value.Disposition == VukerDisposition.Wild;
    }

    public int CountLiving(VukerRegionKey region)
    {
        return _individuals.Values.Count(value => value.IsAlive && value.Region == region);
    }

    public static Result<VukerEcologyState> Restore(VukerEcologySnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        VukerEcologyState state = new VukerEcologyState(snapshot.WorldSeed)
        {
            CurrentTick = snapshot.CurrentTick,
            NextPairSequence = snapshot.NextPairSequence,
            Version = snapshot.Version,
        };
        foreach (VukerIndividualSnapshot saved in snapshot.Individuals)
        {
            state._individuals.Add(saved.EntityId, Individual.FromSnapshot(saved));
        }

        foreach (VukerPairSnapshot saved in snapshot.Pairs)
        {
            state._pairs.Add(saved.PairId, Pair.FromSnapshot(saved));
        }

        return Result<VukerEcologyState>.Success(state);
    }

}

}
