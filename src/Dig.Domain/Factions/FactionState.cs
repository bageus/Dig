using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Factions
{

public static class FactionErrors
{
    public static readonly DomainError UnknownFaction = new DomainError(
        "factions.unknown",
        "The faction is not registered.");

    public static readonly DomainError UnknownMember = new DomainError(
        "factions.member.unknown",
        "The entity has no faction membership.");

    public static readonly DomainError TerritoryOccupied = new DomainError(
        "factions.territory.occupied",
        "The cell is already claimed by another faction.");
}

public sealed class FactionSnapshot
{
    public FactionSnapshot(
        long version,
        IReadOnlyList<FactionRelationSnapshot> relations,
        IReadOnlyList<TerritorySnapshot> territory,
        IReadOnlyDictionary<EntityId, FactionId> memberships)
    {
        Version = version;
        Relations = relations;
        Territory = territory;
        Memberships = memberships;
    }

    public long Version { get; }
    public IReadOnlyList<FactionRelationSnapshot> Relations { get; }
    public IReadOnlyList<TerritorySnapshot> Territory { get; }
    public IReadOnlyDictionary<EntityId, FactionId> Memberships { get; }
}

public sealed class FactionState : AggregateRoot
{
    private readonly Dictionary<FactionPair, int> _relations =
        new Dictionary<FactionPair, int>();
    private readonly Dictionary<CellId, FactionId> _territory =
        new Dictionary<CellId, FactionId>();
    private readonly Dictionary<EntityId, FactionId> _memberships =
        new Dictionary<EntityId, FactionId>();

    public FactionState(FactionCatalog catalog, FactionDiplomacyPolicy policy)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        for (int left = 0; left < catalog.Definitions.Count; left++)
        {
            for (int right = left + 1; right < catalog.Definitions.Count; right++)
            {
                FactionDefinition first = catalog.Definitions[left];
                FactionDefinition second = catalog.Definitions[right];
                int initialScore = checked(
                    (first.InitialRelationScore + second.InitialRelationScore) / 2);
                _relations.Add(new FactionPair(first.Id, second.Id), initialScore);
            }
        }
    }

    public FactionCatalog Catalog { get; }
    public FactionDiplomacyPolicy Policy { get; }
    public long Version { get; private set; }

    public Result AssignMember(EntityId entityId, FactionId factionId)
    {
        if (entityId.IsEmpty)
        {
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));
        }

        if (!Catalog.Contains(factionId))
        {
            return Result.Failure(FactionErrors.UnknownFaction);
        }

        if (_memberships.TryGetValue(entityId, out FactionId existing)
            && existing == factionId)
        {
            return Result.Success();
        }

        _memberships[entityId] = factionId;
        Version = checked(Version + 1);
        return Result.Success();
    }

    public FactionId? GetMemberFaction(EntityId entityId)
    {
        return _memberships.TryGetValue(entityId, out FactionId factionId)
            ? factionId
            : null;
    }

    public Result SetRelation(
        FactionId first,
        FactionId second,
        int score,
        string reasonCode,
        long tick)
    {
        ValidateTick(tick);
        ValidateScore(score, nameof(score));
        if (!Catalog.Contains(first) || !Catalog.Contains(second))
        {
            return Result.Failure(FactionErrors.UnknownFaction);
        }

        FactionPair pair = new FactionPair(first, second);
        int previous = _relations[pair];
        if (previous == score)
        {
            return Result.Success();
        }

        _relations[pair] = score;
        Version = checked(Version + 1);
        Raise(new FactionRelationChanged(
            tick,
            pair.First,
            pair.Second,
            previous,
            score,
            Policy.Resolve(score),
            reasonCode));
        return Result.Success();
    }

    public FactionRelationSnapshot GetRelation(FactionId first, FactionId second)
    {
        if (first == second)
        {
            return new FactionRelationSnapshot(
                first,
                second,
                score: 10_000,
                FactionRelationKind.Allied);
        }

        if (!Catalog.Contains(first) || !Catalog.Contains(second))
        {
            throw new KeyNotFoundException("Both factions must be registered.");
        }

        FactionPair pair = new FactionPair(first, second);
        int score = _relations[pair];
        return new FactionRelationSnapshot(
            pair.First,
            pair.Second,
            score,
            Policy.Resolve(score));
    }

    public bool AreHostile(FactionId first, FactionId second)
    {
        return GetRelation(first, second).Kind == FactionRelationKind.Hostile;
    }

    public Result ClaimTerritory(FactionId factionId, IEnumerable<CellId> cells, long tick)
    {
        ValidateTick(tick);
        if (!Catalog.Contains(factionId))
        {
            return Result.Failure(FactionErrors.UnknownFaction);
        }

        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        CellId[] ordered = cells.Distinct().OrderBy(cell => cell).ToArray();
        foreach (CellId cell in ordered)
        {
            if (_territory.TryGetValue(cell, out FactionId owner) && owner != factionId)
            {
                return Result.Failure(FactionErrors.TerritoryOccupied);
            }
        }

        foreach (CellId cell in ordered)
        {
            if (_territory.ContainsKey(cell))
            {
                continue;
            }

            _territory.Add(cell, factionId);
            Raise(new TerritoryClaimed(tick, factionId, cell));
        }

        if (ordered.Length > 0)
        {
            Version = checked(Version + 1);
        }

        return Result.Success();
    }

    public FactionId? GetTerritoryOwner(CellId cellId)
    {
        return _territory.TryGetValue(cellId, out FactionId owner) ? owner : null;
    }

    public Result RecordTerritoryEntry(EntityId intruderId, CellId cellId, long tick)
    {
        ValidateTick(tick);
        if (!_memberships.TryGetValue(intruderId, out FactionId intruderFaction))
        {
            return Result.Failure(FactionErrors.UnknownMember);
        }

        if (!_territory.TryGetValue(cellId, out FactionId ownerFaction)
            || ownerFaction == intruderFaction)
        {
            return Result.Success();
        }

        FactionRelationSnapshot relation = GetRelation(ownerFaction, intruderFaction);
        if (relation.Kind == FactionRelationKind.Allied)
        {
            return Result.Success();
        }

        Raise(new TerritoryViolated(
            tick,
            ownerFaction,
            intruderFaction,
            intruderId,
            cellId));
        int nextScore = Math.Max(
            -10_000,
            relation.Score - Policy.TerritoryViolationPenalty);
        return SetRelation(
            ownerFaction,
            intruderFaction,
            nextScore,
            "territory_violation",
            tick);
    }

    public FactionSnapshot CreateSnapshot()
    {
        FactionRelationSnapshot[] relations = _relations
            .OrderBy(item => item.Key)
            .Select(item => new FactionRelationSnapshot(
                item.Key.First,
                item.Key.Second,
                item.Value,
                Policy.Resolve(item.Value)))
            .ToArray();
        TerritorySnapshot[] territory = _territory
            .OrderBy(item => item.Key)
            .Select(item => new TerritorySnapshot(item.Key, item.Value))
            .ToArray();
        Dictionary<EntityId, FactionId> memberships = _memberships
            .OrderBy(item => item.Key.ToString(), StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value);
        return new FactionSnapshot(
            Version,
            new ReadOnlyCollection<FactionRelationSnapshot>(relations),
            new ReadOnlyCollection<TerritorySnapshot>(territory),
            new ReadOnlyDictionary<EntityId, FactionId>(memberships));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private static void ValidateScore(int score, string parameterName)
    {
        if (score < -10_000 || score > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
}
