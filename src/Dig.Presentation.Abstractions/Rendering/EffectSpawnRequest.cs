using System;

namespace Dig.Presentation.Rendering
{
public sealed class EffectSpawnRequest
{
    public EffectSpawnRequest(string requestId, string effectId,
        VfxCategory category, VfxPriority priority,
        double worldX, double worldY, double worldZ,
        double durationSeconds, int particleBudget, double scale, long version)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException(nameof(requestId));
        if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException(nameof(effectId));
        if (!Enum.IsDefined(typeof(VfxCategory), category)) throw new ArgumentOutOfRangeException(nameof(category));
        if (!Enum.IsDefined(typeof(VfxPriority), priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (durationSeconds <= 0d || durationSeconds > 30d) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (particleBudget < 1 || particleBudget > 512) throw new ArgumentOutOfRangeException(nameof(particleBudget));
        if (scale < 0.1d || scale > 10d) throw new ArgumentOutOfRangeException(nameof(scale));
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        RequestId = requestId.Trim(); EffectId = effectId.Trim();
        Category = category; Priority = priority;
        WorldX = worldX; WorldY = worldY; WorldZ = worldZ;
        DurationSeconds = durationSeconds; ParticleBudget = particleBudget;
        Scale = scale; Version = version;
    }
    public string RequestId { get; }
    public string EffectId { get; }
    public VfxCategory Category { get; }
    public VfxPriority Priority { get; }
    public double WorldX { get; }
    public double WorldY { get; }
    public double WorldZ { get; }
    public double DurationSeconds { get; }
    public int ParticleBudget { get; }
    public double Scale { get; }
    public long Version { get; }
}
}
