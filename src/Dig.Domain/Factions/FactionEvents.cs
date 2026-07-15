using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Factions
{

public sealed class FactionRelationChanged : IDomainEvent
{
    public FactionRelationChanged(
        long tick,
        FactionId first,
        FactionId second,
        int previousScore,
        int currentScore,
        FactionRelationKind currentKind,
        string reasonCode)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Relation change reason is required.", nameof(reasonCode));
        }

        Tick = tick;
        First = first;
        Second = second;
        PreviousScore = previousScore;
        CurrentScore = currentScore;
        CurrentKind = currentKind;
        ReasonCode = reasonCode.Trim();
    }

    public long Tick { get; }
    public FactionId First { get; }
    public FactionId Second { get; }
    public int PreviousScore { get; }
    public int CurrentScore { get; }
    public FactionRelationKind CurrentKind { get; }
    public string ReasonCode { get; }
}

public sealed class TerritoryClaimed : IDomainEvent
{
    public TerritoryClaimed(long tick, FactionId factionId, CellId cellId)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        FactionId = factionId;
        CellId = cellId;
    }

    public long Tick { get; }
    public FactionId FactionId { get; }
    public CellId CellId { get; }
}

public sealed class TerritoryViolated : IDomainEvent
{
    public TerritoryViolated(
        long tick,
        FactionId ownerFactionId,
        FactionId intruderFactionId,
        EntityId intruderId,
        CellId cellId)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (intruderId.IsEmpty)
        {
            throw new ArgumentException("Intruder id cannot be empty.", nameof(intruderId));
        }

        Tick = tick;
        OwnerFactionId = ownerFactionId;
        IntruderFactionId = intruderFactionId;
        IntruderId = intruderId;
        CellId = cellId;
    }

    public long Tick { get; }
    public FactionId OwnerFactionId { get; }
    public FactionId IntruderFactionId { get; }
    public EntityId IntruderId { get; }
    public CellId CellId { get; }
}
}
