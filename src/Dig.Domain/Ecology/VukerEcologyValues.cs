using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public enum VukerLifecycleStage
{
    Child = 0,
    Adult = 1,
}

public enum VukerDisposition
{
    Wild = 0,
    Tamed = 1,
}

public readonly struct VukerRegionKey : IEquatable<VukerRegionKey>, IComparable<VukerRegionKey>
{
    public VukerRegionKey(CellId root)
    {
        Root = root;
    }

    public CellId Root { get; }

    public int CompareTo(VukerRegionKey other) => Root.CompareTo(other.Root);
    public bool Equals(VukerRegionKey other) => Root == other.Root;
    public override bool Equals(object? obj) => obj is VukerRegionKey other && Equals(other);
    public override int GetHashCode() => Root.GetHashCode();
    public override string ToString() => Root.ToString();
    public static bool operator ==(VukerRegionKey left, VukerRegionKey right) => left.Equals(right);
    public static bool operator !=(VukerRegionKey left, VukerRegionKey right) => !left.Equals(right);
}

public readonly struct VukerPairId : IEquatable<VukerPairId>, IComparable<VukerPairId>
{
    private readonly string _value;

    public VukerPairId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Vuker pair id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);
    public int CompareTo(VukerPairId other) => string.Compare(_value, other._value, StringComparison.Ordinal);
    public bool Equals(VukerPairId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is VukerPairId other && Equals(other);
    public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
    public override string ToString() => _value ?? string.Empty;
    public static bool operator ==(VukerPairId left, VukerPairId right) => left.Equals(right);
    public static bool operator !=(VukerPairId left, VukerPairId right) => !left.Equals(right);
}

public static class VukerEcologyProfile
{
    public const int ReproductionCooldownDays = 7;
    public const int ChildGrowthDays = 3;
    public const int MaximumSuccessfulCyclesPerPair = 3;
    public const int PopulationCapPerRegion = 10;
    public const int ReproductionCooldownTicks =
        GameTimeCadence.TicksPerDay * ReproductionCooldownDays;
    public const int ChildGrowthTicks =
        GameTimeCadence.TicksPerDay * ChildGrowthDays;
}

public sealed class VukerIndividualSnapshot
{
    public VukerIndividualSnapshot(
        EntityId entityId,
        VukerLifecycleStage lifecycle,
        VukerDisposition disposition,
        VukerRegionKey region,
        CellId position,
        bool isAlive,
        long birthTick,
        long maturityTick,
        EntityId? kidnapReservedBy,
        EntityId? tamedByResidentId,
        VukerPairId? activePairId,
        long version)
    {
        EntityId = entityId;
        Lifecycle = lifecycle;
        Disposition = disposition;
        Region = region;
        Position = position;
        IsAlive = isAlive;
        BirthTick = birthTick;
        MaturityTick = maturityTick;
        KidnapReservedBy = kidnapReservedBy;
        TamedByResidentId = tamedByResidentId;
        ActivePairId = activePairId;
        Version = version;
    }

    public EntityId EntityId { get; }
    public VukerLifecycleStage Lifecycle { get; }
    public VukerDisposition Disposition { get; }
    public VukerRegionKey Region { get; }
    public CellId Position { get; }
    public bool IsAlive { get; }
    public long BirthTick { get; }
    public long MaturityTick { get; }
    public EntityId? KidnapReservedBy { get; }
    public EntityId? TamedByResidentId { get; }
    public VukerPairId? ActivePairId { get; }
    public long Version { get; }
    public bool IsCombatEligible => IsAlive && Lifecycle == VukerLifecycleStage.Adult;
    public bool IsWildChild => IsAlive
        && Lifecycle == VukerLifecycleStage.Child
        && Disposition == VukerDisposition.Wild;
}

public sealed class VukerPairSnapshot
{
    public VukerPairSnapshot(
        VukerPairId pairId,
        EntityId firstParentId,
        EntityId secondParentId,
        VukerRegionKey region,
        int successfulCycles,
        long nextBirthTick,
        bool isActive,
        string? terminalReason,
        string? blockedReason,
        long version)
    {
        PairId = pairId;
        FirstParentId = firstParentId;
        SecondParentId = secondParentId;
        Region = region;
        SuccessfulCycles = successfulCycles;
        NextBirthTick = nextBirthTick;
        IsActive = isActive;
        TerminalReason = terminalReason;
        BlockedReason = blockedReason;
        Version = version;
    }

    public VukerPairId PairId { get; }
    public EntityId FirstParentId { get; }
    public EntityId SecondParentId { get; }
    public VukerRegionKey Region { get; }
    public int SuccessfulCycles { get; }
    public long NextBirthTick { get; }
    public bool IsActive { get; }
    public string? TerminalReason { get; }
    public string? BlockedReason { get; }
    public long Version { get; }
    public bool IsDue(long tick) => IsActive
        && SuccessfulCycles < VukerEcologyProfile.MaximumSuccessfulCyclesPerPair
        && tick >= NextBirthTick;
}

public sealed class VukerEcologySnapshot
{
    public VukerEcologySnapshot(
        ulong worldSeed,
        long currentTick,
        long nextPairSequence,
        long version,
        IReadOnlyCollection<VukerIndividualSnapshot> individuals,
        IReadOnlyCollection<VukerPairSnapshot> pairs)
    {
        WorldSeed = worldSeed;
        CurrentTick = currentTick;
        NextPairSequence = nextPairSequence;
        Version = version;
        Individuals = new ReadOnlyCollection<VukerIndividualSnapshot>(
            individuals.OrderBy(value => value.EntityId.ToString(), StringComparer.Ordinal).ToArray());
        Pairs = new ReadOnlyCollection<VukerPairSnapshot>(
            pairs.OrderBy(value => value.PairId).ToArray());
    }

    public ulong WorldSeed { get; }
    public long CurrentTick { get; }
    public long NextPairSequence { get; }
    public long Version { get; }
    public IReadOnlyList<VukerIndividualSnapshot> Individuals { get; }
    public IReadOnlyList<VukerPairSnapshot> Pairs { get; }
}

public static class VukerEcologyErrors
{
    public static readonly DomainError IndividualNotFound = new DomainError(
        "ecology.vuker.individual_not_found",
        "The requested Vuker is not registered.");
    public static readonly DomainError PairNotFound = new DomainError(
        "ecology.vuker.pair_not_found",
        "The requested Vuker pair is not registered.");
    public static readonly DomainError AlreadyRegistered = new DomainError(
        "ecology.vuker.already_registered",
        "The Vuker identity is already registered.");
    public static readonly DomainError InvalidLifecycle = new DomainError(
        "ecology.vuker.invalid_lifecycle",
        "The requested Vuker lifecycle transition is not valid.");
    public static readonly DomainError KidnapUnavailable = new DomainError(
        "ecology.vuker.kidnap_unavailable",
        "Only a living unreserved wild child can be kidnapped.");
    public static readonly DomainError KidnapReservationConflict = new DomainError(
        "ecology.vuker.kidnap_reservation_conflict",
        "The Vuker child is already reserved by another resident.");
    public static readonly DomainError PopulationCapReached = new DomainError(
        "ecology.vuker.population_cap_reached",
        "The connected cave region already contains ten living Vukers.");
    public static readonly DomainError BirthNotDue = new DomainError(
        "ecology.vuker.birth_not_due",
        "The Vuker pair is not due for a new child.");
    public static readonly DomainError InvalidSnapshot = new DomainError(
        "ecology.vuker.invalid_snapshot",
        "The Vuker ecology save snapshot is invalid.");
}


public sealed class VukerRegistered : IDomainEvent
{
    public VukerRegistered(long tick, EntityId entityId, VukerLifecycleStage lifecycle, VukerDisposition disposition, VukerRegionKey region)
    { Tick = tick; EntityId = entityId; Lifecycle = lifecycle; Disposition = disposition; Region = region; }
    public long Tick { get; }
    public EntityId EntityId { get; }
    public VukerLifecycleStage Lifecycle { get; }
    public VukerDisposition Disposition { get; }
    public VukerRegionKey Region { get; }
}

public sealed class VukerPairFormed : IDomainEvent
{
    public VukerPairFormed(long tick, VukerPairId pairId, EntityId first, EntityId second, VukerRegionKey region)
    { Tick = tick; PairId = pairId; FirstParentId = first; SecondParentId = second; Region = region; }
    public long Tick { get; }
    public VukerPairId PairId { get; }
    public EntityId FirstParentId { get; }
    public EntityId SecondParentId { get; }
    public VukerRegionKey Region { get; }
}

public sealed class VukerPairBroken : IDomainEvent
{
    public VukerPairBroken(long tick, VukerPairId pairId, string reasonCode)
    { Tick = tick; PairId = pairId; ReasonCode = reasonCode; }
    public long Tick { get; }
    public VukerPairId PairId { get; }
    public string ReasonCode { get; }
}

public sealed class VukerChildBorn : IDomainEvent
{
    public VukerChildBorn(long tick, VukerPairId pairId, EntityId childId, int cycle, CellId position)
    { Tick = tick; PairId = pairId; ChildId = childId; SuccessfulCycle = cycle; Position = position; }
    public long Tick { get; }
    public VukerPairId PairId { get; }
    public EntityId ChildId { get; }
    public int SuccessfulCycle { get; }
    public CellId Position { get; }
}

public sealed class VukerBirthBlocked : IDomainEvent
{
    public VukerBirthBlocked(long tick, VukerPairId pairId, string reasonCode)
    { Tick = tick; PairId = pairId; ReasonCode = reasonCode; }
    public long Tick { get; }
    public VukerPairId PairId { get; }
    public string ReasonCode { get; }
}

public sealed class VukerMatured : IDomainEvent
{
    public VukerMatured(long tick, EntityId entityId, VukerDisposition disposition)
    { Tick = tick; EntityId = entityId; Disposition = disposition; }
    public long Tick { get; }
    public EntityId EntityId { get; }
    public VukerDisposition Disposition { get; }
}

public sealed class VukerKidnapReserved : IDomainEvent
{
    public VukerKidnapReserved(long tick, EntityId childId, EntityId residentId)
    { Tick = tick; ChildId = childId; ResidentId = residentId; }
    public long Tick { get; }
    public EntityId ChildId { get; }
    public EntityId ResidentId { get; }
}

public sealed class VukerKidnapCancelled : IDomainEvent
{
    public VukerKidnapCancelled(long tick, EntityId childId, EntityId residentId, string reasonCode)
    { Tick = tick; ChildId = childId; ResidentId = residentId; ReasonCode = reasonCode; }
    public long Tick { get; }
    public EntityId ChildId { get; }
    public EntityId ResidentId { get; }
    public string ReasonCode { get; }
}

public sealed class VukerTamed : IDomainEvent
{
    public VukerTamed(long tick, EntityId childId, EntityId residentId)
    { Tick = tick; ChildId = childId; ResidentId = residentId; }
    public long Tick { get; }
    public EntityId ChildId { get; }
    public EntityId ResidentId { get; }
}

}
