using System;

namespace Dig.Presentation.Rendering
{
public sealed class EffectSpawnRequest
{
    public EffectSpawnRequest(
        string requestId,
        string effectId,
        VfxCategory category,
        VfxPriority priority,
        double worldX,
        double worldY,
        double worldZ,
        double durationSeconds,
        long version)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException(nameof(requestId));
        if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException(nameof(effectId));
        if (!Enum.IsDefined(typeof(VfxCategory), category)) throw new ArgumentOutOfRangeException(nameof(category));
        if (!Enum.IsDefined(typeof(VfxPriority), priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (durationSeconds <= 0d || durationSeconds > 30d) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        RequestId = requestId.Trim();
        EffectId = effectId.Trim();
        Category = category;
        Priority = priority;
        WorldX = worldX;
        WorldY = worldY;
        WorldZ = worldZ;
        DurationSeconds = durationSeconds;
        Version = version;
    }

    public string RequestId { get; }
    public string EffectId { get; }
    public VfxCategory Category { get; }
    public VfxPriority Priority { get; }
    public double WorldX { get; }
    public double WorldY { get; }
    public double WorldZ { get; }
    public double DurationSeconds { get; }
    public long Version { get; }
}
}