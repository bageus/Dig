using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public enum CombatIntentKind
{
    Defend = 0,
    Approach = 1,
    Attack = 2,
    Retreat = 3,
}

public sealed class ThreatCandidate
{
    public ThreatCandidate(
        EntityId entityId,
        FactionId factionId,
        CellId position,
        int combatStrength,
        bool isAlive)
    {
        if (entityId.IsEmpty)
        {
            throw new ArgumentException("Threat entity id cannot be empty.", nameof(entityId));
        }

        if (factionId.IsEmpty)
        {
            throw new ArgumentException("Threat faction id cannot be empty.", nameof(factionId));
        }

        if (combatStrength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(combatStrength));
        }

        EntityId = entityId;
        FactionId = factionId;
        Position = position;
        CombatStrength = combatStrength;
        IsAlive = isAlive;
    }

    public EntityId EntityId { get; }
    public FactionId FactionId { get; }
    public CellId Position { get; }
    public int CombatStrength { get; }
    public bool IsAlive { get; }
}

public readonly struct DetectedThreat
{
    public DetectedThreat(EntityId entityId, int distance, int combatStrength)
    {
        EntityId = entityId;
        Distance = distance;
        CombatStrength = combatStrength;
    }

    public EntityId EntityId { get; }
    public int Distance { get; }
    public int CombatStrength { get; }
}

public static class CombatThreatDetector
{
    public static IReadOnlyList<DetectedThreat> FindHostileThreats(
        FactionId observerFaction,
        CellId observerPosition,
        int sightRange,
        IEnumerable<ThreatCandidate> candidates,
        FactionState factions)
    {
        if (sightRange < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sightRange));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (factions is null)
        {
            throw new ArgumentNullException(nameof(factions));
        }

        DetectedThreat[] threats = candidates
            .Where(candidate => candidate.IsAlive
                && factions.AreHostile(observerFaction, candidate.FactionId))
            .Select(candidate => new DetectedThreat(
                candidate.EntityId,
                Distance(observerPosition, candidate.Position),
                candidate.CombatStrength))
            .Where(candidate => candidate.Distance <= sightRange)
            .OrderBy(candidate => candidate.Distance)
            .ThenByDescending(candidate => candidate.CombatStrength)
            .ThenBy(candidate => candidate.EntityId.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<DetectedThreat>(threats);
    }

    private static int Distance(CellId first, CellId second)
    {
        return checked(Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y));
    }
}

public sealed class CombatTacticalPolicy
{
    public CombatTacticalPolicy(
        int retreatHealthThreshold,
        int retreatThreatRatio,
        int defendDistance)
    {
        if (retreatHealthThreshold < 0 || retreatHealthThreshold > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(retreatHealthThreshold));
        }

        if (retreatThreatRatio < 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(retreatThreatRatio));
        }

        if (defendDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defendDistance));
        }

        RetreatHealthThreshold = retreatHealthThreshold;
        RetreatThreatRatio = retreatThreatRatio;
        DefendDistance = defendDistance;
    }

    public int RetreatHealthThreshold { get; }
    public int RetreatThreatRatio { get; }
    public int DefendDistance { get; }
}

public sealed class CombatTacticalDecision
{
    public CombatTacticalDecision(
        CombatIntentKind intent,
        string reasonCode,
        int ownStrength,
        int threatStrength,
        int distance)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Decision reason is required.", nameof(reasonCode));
        }

        Intent = intent;
        ReasonCode = reasonCode.Trim();
        OwnStrength = ownStrength;
        ThreatStrength = threatStrength;
        Distance = distance;
    }

    public CombatIntentKind Intent { get; }
    public string ReasonCode { get; }
    public int OwnStrength { get; }
    public int ThreatStrength { get; }
    public int Distance { get; }
}

public static class CombatTacticalEvaluator
{
    public static CombatTacticalDecision Evaluate(
        CombatTacticalPolicy policy,
        int health,
        int ownStrength,
        int threatStrength,
        int distance,
        int weaponMaximumRange)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        ValidateNonNegative(health, nameof(health));
        ValidateNonNegative(ownStrength, nameof(ownStrength));
        ValidateNonNegative(threatStrength, nameof(threatStrength));
        ValidateNonNegative(distance, nameof(distance));
        ValidateNonNegative(weaponMaximumRange, nameof(weaponMaximumRange));

        bool overwhelmed = threatStrength * 1_000L
            > ownStrength * (long)policy.RetreatThreatRatio;
        if (health <= policy.RetreatHealthThreshold || overwhelmed)
        {
            return new CombatTacticalDecision(
                CombatIntentKind.Retreat,
                health <= policy.RetreatHealthThreshold
                    ? "health_below_retreat_threshold"
                    : "threat_overwhelming",
                ownStrength,
                threatStrength,
                distance);
        }

        if (threatStrength == 0 || distance <= policy.DefendDistance)
        {
            return new CombatTacticalDecision(
                CombatIntentKind.Defend,
                threatStrength == 0 ? "no_detected_threat" : "hold_defensive_distance",
                ownStrength,
                threatStrength,
                distance);
        }

        return distance <= weaponMaximumRange
            ? new CombatTacticalDecision(
                CombatIntentKind.Attack,
                "hostile_target_in_range",
                ownStrength,
                threatStrength,
                distance)
            : new CombatTacticalDecision(
                CombatIntentKind.Approach,
                "hostile_target_out_of_range",
                ownStrength,
                threatStrength,
                distance);
    }

    private static void ValidateNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
}
