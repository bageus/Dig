using System;

namespace Dig.Presentation.Rendering
{
public sealed class RenderFrameBudget
{
    public RenderFrameBudget(int maximumEffects, int maximumParticles,
        int maximumRealtimeLights, int maximumShadowedLights)
    {
        if (maximumEffects < 1 || maximumEffects > 256)
            throw new ArgumentOutOfRangeException(nameof(maximumEffects));
        if (maximumParticles < 16 || maximumParticles > 8192)
            throw new ArgumentOutOfRangeException(nameof(maximumParticles));
        if (maximumRealtimeLights < 0 || maximumRealtimeLights > 32)
            throw new ArgumentOutOfRangeException(nameof(maximumRealtimeLights));
        if (maximumShadowedLights < 0 || maximumShadowedLights > maximumRealtimeLights)
            throw new ArgumentOutOfRangeException(nameof(maximumShadowedLights));
        MaximumEffects = maximumEffects;
        MaximumParticles = maximumParticles;
        MaximumRealtimeLights = maximumRealtimeLights;
        MaximumShadowedLights = maximumShadowedLights;
    }
    public int MaximumEffects { get; }
    public int MaximumParticles { get; }
    public int MaximumRealtimeLights { get; }
    public int MaximumShadowedLights { get; }
    public static RenderFrameBudget Default { get; } =
        new RenderFrameBudget(64, 2048, 8, 2);
}
}
