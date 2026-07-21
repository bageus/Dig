using System;
using System.Collections.Generic;

namespace Dig.Presentation.Rendering
{
public sealed class PresentationEffectPresenter
{
    public PresentationEffectFrame Present(IReadOnlyList<PresentationEffectFact> facts)
    {
        if (facts == null) throw new ArgumentNullException(nameof(facts));
        if (facts.Count == 0) return PresentationEffectFrame.Empty;

        List<PresentationEffectFact> ordered = new List<PresentationEffectFact>(facts.Count);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < facts.Count; index++)
        {
            PresentationEffectFact fact = facts[index]
                ?? throw new ArgumentException("Presentation effect fact cannot be null.", nameof(facts));
            if (!ids.Add(fact.EventId))
                throw new InvalidOperationException(
                    $"Duplicate presentation effect id '{fact.EventId}'.");
            ordered.Add(fact);
        }
        ordered.Sort(CompareFacts);

        List<EffectSpawnRequest> effects = new List<EffectSpawnRequest>(ordered.Count);
        List<LightRequest> lights = new List<LightRequest>(ordered.Count);
        for (int index = 0; index < ordered.Count; index++)
            Project(ordered[index], effects, lights);
        return new PresentationEffectFrame(effects, lights);
    }

    private static int CompareFacts(PresentationEffectFact left,
        PresentationEffectFact right)
    {
        int id = StringComparer.Ordinal.Compare(left.EventId, right.EventId);
        if (id != 0) return id;
        int kind = left.Kind.CompareTo(right.Kind);
        return kind != 0 ? kind : left.Version.CompareTo(right.Version);
    }

    private static void Project(PresentationEffectFact fact,
        ICollection<EffectSpawnRequest> effects,
        ICollection<LightRequest> lights)
    {
        switch (fact.Kind)
        {
            case PresentationEffectKind.ExcavationImpact:
                effects.Add(Effect(fact, "vfx.excavation.impact",
                    VfxCategory.Excavation, VfxPriority.Normal,
                    0.65d, 24, 40, 0.70d, 0.60d));
                break;
            case PresentationEffectKind.DepositReveal:
                effects.Add(Effect(fact, "vfx.deposit.reveal",
                    VfxCategory.Deposit, VfxPriority.Important,
                    0.90d, 32, 48, 0.80d, 0.70d));
                break;
            case PresentationEffectKind.ConstructionProgress:
                effects.Add(Effect(fact, "vfx.construction.progress",
                    VfxCategory.Construction, VfxPriority.Normal,
                    0.80d, 18, 30, 0.70d, 0.50d));
                break;
            case PresentationEffectKind.ProductionPulse:
                effects.Add(Effect(fact, "vfx.production.pulse",
                    VfxCategory.Production, VfxPriority.Normal,
                    0.90d, 20, 36, 0.80d, 0.60d));
                break;
            case PresentationEffectKind.StatusPulse:
                effects.Add(Effect(fact, "vfx.status.pulse",
                    VfxCategory.Status, VfxPriority.Important,
                    1.10d, 18, 28, 0.75d, 0.50d));
                break;
            case PresentationEffectKind.CombatImpact:
                effects.Add(Effect(fact, "vfx.combat.impact",
                    VfxCategory.Combat, VfxPriority.Critical,
                    0.45d, 28, 60, 0.80d, 0.80d));
                break;
            case PresentationEffectKind.AmbientDust:
                effects.Add(Effect(fact, "vfx.ambient.dust",
                    VfxCategory.Ambient, VfxPriority.Ambient,
                    2.50d, 12, 18, 0.90d, 0.80d));
                break;
            case PresentationEffectKind.LavaGlow:
                effects.Add(Effect(fact, "vfx.ambient.lava",
                    VfxCategory.Ambient, VfxPriority.Normal,
                    1.80d, 12, 24, 0.90d, 0.70d));
                lights.Add(Light(fact, "light.lava", RealtimeLightPriority.Focused,
                    4.5d, 1.6d, 1.00d, 0.24d, 0.06d, true));
                break;
            case PresentationEffectKind.CrystalGlow:
                effects.Add(Effect(fact, "vfx.deposit.crystal-glow",
                    VfxCategory.Deposit, VfxPriority.Normal,
                    1.60d, 10, 20, 0.80d, 0.50d));
                lights.Add(Light(fact, "light.crystal", RealtimeLightPriority.Normal,
                    3.4d, 1.1d, 0.24d, 0.72d, 1.00d, false));
                break;
            case PresentationEffectKind.CampfireGlow:
                effects.Add(Effect(fact, "vfx.production.campfire",
                    VfxCategory.Production, VfxPriority.Important,
                    1.20d, 14, 24, 0.80d, 0.50d));
                lights.Add(Light(fact, "light.campfire", RealtimeLightPriority.Focused,
                    4.0d, 1.4d, 1.00d, 0.52d, 0.16d, true));
                break;
            case PresentationEffectKind.ProductionBuildingGlow:
                effects.Add(Effect(fact, "vfx.production.building",
                    VfxCategory.Production, VfxPriority.Normal,
                    1.20d, 12, 24, 0.80d, 0.50d));
                lights.Add(Light(fact, "light.production-building",
                    RealtimeLightPriority.Normal,
                    3.8d, 1.2d, 1.00d, 0.38d, 0.10d, false));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fact));
        }
    }

    private static EffectSpawnRequest Effect(PresentationEffectFact fact,
        string effectId, VfxCategory category, VfxPriority priority,
        double duration, int baseParticles, int extraParticles,
        double baseScale, double extraScale)
    {
        int particles = baseParticles
            + (int)Math.Round(extraParticles * fact.Magnitude, MidpointRounding.AwayFromZero);
        double scale = baseScale + extraScale * fact.Magnitude;
        return new EffectSpawnRequest(fact.EventId + ":effect", effectId,
            category, priority, fact.WorldX, fact.WorldY, fact.WorldZ,
            duration, particles, scale, fact.Version);
    }

    private static LightRequest Light(PresentationEffectFact fact,
        string suffix, RealtimeLightPriority priority,
        double baseRange, double baseIntensity,
        double red, double green, double blue, bool castsShadows)
    {
        double factor = 0.65d + 0.70d * fact.Magnitude;
        return new LightRequest(fact.EventId + ":" + suffix,
            RealtimeLightKind.Point, priority,
            fact.WorldX, fact.WorldY, fact.WorldZ,
            baseRange * factor, baseIntensity * factor,
            red, green, blue, castsShadows, fact.Version);
    }
}
}