using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Production;

namespace Dig.Presentation.Rendering
{

public readonly struct PresentationEffectLocation
{
    public PresentationEffectLocation(double worldX, double worldY, double worldZ)
    {
        WorldX = worldX;
        WorldY = worldY;
        WorldZ = worldZ;
    }

    public double WorldX { get; }
    public double WorldY { get; }
    public double WorldZ { get; }
}

public sealed class PresentationDomainEffectProjector
{
    public IReadOnlyList<PresentationEffectFact> Project(
        IReadOnlyList<IDomainEvent> events,
        Func<EntityId, PresentationEffectLocation?> resolveLocation)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        if (resolveLocation == null)
            throw new ArgumentNullException(nameof(resolveLocation));

        List<PresentationEffectFact> facts =
            new List<PresentationEffectFact>(events.Count);
        for (int index = 0; index < events.Count; index++)
        {
            IDomainEvent item = events[index]
                ?? throw new ArgumentException("Domain event cannot be null.", nameof(events));
            ProjectOne(item, resolveLocation, facts);
        }

        return new ReadOnlyCollection<PresentationEffectFact>(facts);
    }

    private static void ProjectOne(
        IDomainEvent item,
        Func<EntityId, PresentationEffectLocation?> resolve,
        ICollection<PresentationEffectFact> facts)
    {
        switch (item)
        {
            case BuildingConstructionProgressed construction:
                Add(facts, resolve, construction.BuildingId,
                    $"construction:{construction.BuildingId}:{construction.Tick}:"
                        + construction.CompletedWork,
                    PresentationEffectKind.ConstructionProgress,
                    Ratio(construction.CompletedWork, construction.RequiredWork),
                    construction.Tick);
                break;
            case BuildingPackingProgressed packing:
                Add(facts, resolve, packing.BuildingId,
                    $"building-packing:{packing.BuildingId}:{packing.Tick}:"
                        + packing.CompletedWork,
                    PresentationEffectKind.ConstructionProgress,
                    Ratio(packing.CompletedWork, packing.RequiredWork),
                    packing.Tick);
                break;
            case BuildingCompleted completed:
                Add(facts, resolve, completed.BuildingId,
                    $"building-completed:{completed.BuildingId}:{completed.Tick}",
                    PresentationEffectKind.ConstructionProgress,
                    1d,
                    completed.Tick);
                break;
            case ProductionWorkApplied production:
                Add(facts, resolve, production.BuildingId,
                    $"production:{production.OrderId}:{production.Tick}:"
                        + production.CompletedWork,
                    PresentationEffectKind.ProductionPulse,
                    Ratio(production.CompletedWork, production.RequiredWork),
                    production.Tick);
                break;
            case CombatAttackResolved combat:
                Add(facts, resolve, combat.Resolution.TargetId,
                    combat.EventId,
                    PresentationEffectKind.CombatImpact,
                    CombatMagnitude(combat.Resolution),
                    combat.Tick);
                break;
            case CombatStatusTicked status:
                Add(facts, resolve, status.Damage.TargetId,
                    $"combat-status:{status.Damage.TargetId}:"
                        + $"{status.Damage.StatusId}:{status.Tick}:"
                        + status.Damage.TicksApplied,
                    PresentationEffectKind.StatusPulse,
                    DamageMagnitude(status.Damage.Damage),
                    status.Tick);
                break;
            case AgentExternalEffectApplied external
                when !external.SourceId.StartsWith("combat", StringComparison.Ordinal):
                Add(facts, resolve, external.AgentId,
                    $"agent-status:{external.AgentId}:{external.Tick}:"
                        + external.SourceId,
                    PresentationEffectKind.StatusPulse,
                    DamageMagnitude(Math.Abs(external.CurrentHealth - external.PreviousHealth)),
                    external.Tick);
                break;
        }
    }

    private static void Add(
        ICollection<PresentationEffectFact> facts,
        Func<EntityId, PresentationEffectLocation?> resolve,
        EntityId entityId,
        string eventId,
        PresentationEffectKind kind,
        double magnitude,
        long version)
    {
        PresentationEffectLocation? location = resolve(entityId);
        if (!location.HasValue) return;
        PresentationEffectLocation value = location.Value;
        facts.Add(new PresentationEffectFact(
            eventId,
            kind,
            value.WorldX,
            value.WorldY,
            value.WorldZ,
            magnitude,
            version));
    }

    private static double Ratio(int completed, int required)
    {
        return required <= 0 ? 0d : Math.Min(1d, Math.Max(0d, (double)completed / required));
    }

    private static double CombatMagnitude(CombatAttackResolution resolution)
    {
        if (resolution.Outcome == CombatAttackOutcome.Miss) return 0.20d;
        if (resolution.Outcome == CombatAttackOutcome.Blocked) return 0.45d;
        return DamageMagnitude(resolution.Damage);
    }

    private static double DamageMagnitude(int damage)
    {
        return Math.Min(1d, 0.25d + (Math.Max(0, damage) / 40d));
    }
}
}
