using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class FactionStateTests
{
    private static readonly FactionId Player = new FactionId("faction.player");
    private static readonly FactionId Visitors = new FactionId("faction.visitors");
    private static readonly EntityId VisitorId = EntityId.Parse(
        "a1000000000000000000000000000001");

    [Fact]
    public void Territory_violation_changes_diplomacy_through_factions_owner()
    {
        FactionState factions = CreateState(initialScore: 0);
        CellId protectedCell = new CellId(4, 4);
        Assert.True(factions.AssignMember(VisitorId, Visitors).IsSuccess);
        Assert.True(factions.ClaimTerritory(Player, new[] { protectedCell }, tick: 1).IsSuccess);

        Assert.True(factions.RecordTerritoryEntry(VisitorId, protectedCell, tick: 2).IsSuccess);

        FactionRelationSnapshot relation = factions.GetRelation(Player, Visitors);
        Assert.Equal(-2_000, relation.Score);
        Assert.Equal(FactionRelationKind.Neutral, relation.Kind);
        TerritoryViolated violation = Assert.Single(
            factions.PeekUncommittedEvents().OfType<TerritoryViolated>());
        Assert.Equal(VisitorId, violation.IntruderId);
        FactionRelationChanged changed = Assert.Single(
            factions.PeekUncommittedEvents().OfType<FactionRelationChanged>());
        Assert.Equal("territory_violation", changed.ReasonCode);
    }

    [Fact]
    public void Allied_member_can_enter_territory_without_penalty()
    {
        FactionState factions = CreateState(initialScore: 9_000);
        CellId protectedCell = new CellId(4, 4);
        factions.AssignMember(VisitorId, Visitors);
        factions.ClaimTerritory(Player, new[] { protectedCell }, tick: 1);
        factions.DequeueUncommittedEvents();

        Assert.True(factions.RecordTerritoryEntry(VisitorId, protectedCell, tick: 2).IsSuccess);

        Assert.Equal(9_000, factions.GetRelation(Player, Visitors).Score);
        Assert.Empty(factions.PeekUncommittedEvents());
    }

    [Fact]
    public void Territory_batch_is_atomic_when_cell_has_other_owner()
    {
        FactionState factions = CreateState(initialScore: 0);
        CellId occupied = new CellId(1, 1);
        CellId free = new CellId(2, 1);
        Assert.True(factions.ClaimTerritory(Player, new[] { occupied }, tick: 1).IsSuccess);

        Result result = factions.ClaimTerritory(
            Visitors,
            new[] { free, occupied },
            tick: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(FactionErrors.TerritoryOccupied, result.Error);
        Assert.Null(factions.GetTerritoryOwner(free));
        Assert.Equal(Player, factions.GetTerritoryOwner(occupied));
    }

    private static FactionState CreateState(int initialScore)
    {
        return new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(Player, "Player", initialScore),
                new FactionDefinition(Visitors, "Visitors", initialScore),
            }),
            new FactionDiplomacyPolicy(
                hostileThreshold: -5_000,
                friendlyThreshold: 3_000,
                alliedThreshold: 8_000,
                territoryViolationPenalty: 2_000));
    }
}
}
