using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dig.Presentation.Rendering
{
public enum PresentationEffectKind
{
    ExcavationImpact = 0,
    DepositReveal = 1,
    ConstructionProgress = 2,
    ProductionPulse = 3,
    StatusPulse = 4,
    CombatImpact = 5,
    AmbientDust = 6,
    LavaGlow = 7,
    CrystalGlow = 8,
    CampfireGlow = 9,
    ProductionBuildingGlow = 10,
}

public sealed class PresentationEffectFact
{
    public PresentationEffectFact(string eventId, PresentationEffectKind kind,
        double worldX, double worldY, double worldZ,
        double magnitude, long version)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException(nameof(eventId));
        if (!Enum.IsDefined(typeof(PresentationEffectKind), kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (magnitude < 0d || magnitude > 1d)
            throw new ArgumentOutOfRangeException(nameof(magnitude));
        if (version < 0L) throw new ArgumentOutOfRangeException(nameof(version));
        EventId = eventId.Trim(); Kind = kind;
        WorldX = worldX; WorldY = worldY; WorldZ = worldZ;
        Magnitude = magnitude; Version = version;
    }

    public string EventId { get; }
    public PresentationEffectKind Kind { get; }
    public double WorldX { get; }
    public double WorldY { get; }
    public double WorldZ { get; }
    public double Magnitude { get; }
    public long Version { get; }
}

public sealed class PresentationEffectFrame
{
    public static PresentationEffectFrame Empty { get; } = new PresentationEffectFrame(
        Array.Empty<EffectSpawnRequest>(), Array.Empty<LightRequest>());

    public PresentationEffectFrame(IReadOnlyList<EffectSpawnRequest> effects,
        IReadOnlyList<LightRequest> lights)
    {
        if (effects == null) throw new ArgumentNullException(nameof(effects));
        if (lights == null) throw new ArgumentNullException(nameof(lights));
        Effects = new ReadOnlyCollection<EffectSpawnRequest>(Copy(effects));
        Lights = new ReadOnlyCollection<LightRequest>(Copy(lights));
    }

    public IReadOnlyList<EffectSpawnRequest> Effects { get; }
    public IReadOnlyList<LightRequest> Lights { get; }

    private static T[] Copy<T>(IReadOnlyList<T> values)
    {
        T[] copy = new T[values.Count];
        for (int index = 0; index < values.Count; index++) copy[index] = values[index];
        return copy;
    }
}
}