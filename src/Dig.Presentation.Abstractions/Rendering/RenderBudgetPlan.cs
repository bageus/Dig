using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dig.Presentation.Rendering
{
public sealed class SelectedLight
{
    public SelectedLight(LightRequest request, bool shadowsEnabled)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        ShadowsEnabled = shadowsEnabled;
    }

    public LightRequest Request { get; }
    public bool ShadowsEnabled { get; }
}

public sealed class RenderBudgetPlan
{
    private RenderBudgetPlan(IReadOnlyList<EffectSpawnRequest> effects,
        IReadOnlyList<SelectedLight> lights, int particleCount,
        int droppedEffects, int droppedLights)
    {
        Effects = effects;
        Lights = lights;
        ParticleCount = particleCount;
        DroppedEffects = droppedEffects;
        DroppedLights = droppedLights;
    }

    public IReadOnlyList<EffectSpawnRequest> Effects { get; }
    public IReadOnlyList<SelectedLight> Lights { get; }
    public int ParticleCount { get; }
    public int DroppedEffects { get; }
    public int DroppedLights { get; }

    public static RenderBudgetPlan Create(
        IReadOnlyList<EffectSpawnRequest> effects,
        IReadOnlyList<LightRequest> lights,
        RenderFrameBudget budget,
        double focusX, double focusY, double focusZ)
    {
        if (effects == null) throw new ArgumentNullException(nameof(effects));
        if (lights == null) throw new ArgumentNullException(nameof(lights));
        if (budget == null) throw new ArgumentNullException(nameof(budget));
        ValidateUniqueEffects(effects);
        ValidateUniqueLights(lights);

        List<EffectSpawnRequest> orderedEffects = new List<EffectSpawnRequest>(effects);
        orderedEffects.Sort((left, right) => CompareEffects(
            left, right, focusX, focusY, focusZ));
        List<EffectSpawnRequest> selectedEffects = new List<EffectSpawnRequest>();
        int particles = 0;
        for (int index = 0; index < orderedEffects.Count; index++)
        {
            EffectSpawnRequest effect = orderedEffects[index];
            if (selectedEffects.Count >= budget.MaximumEffects
                || particles + effect.ParticleBudget > budget.MaximumParticles)
            {
                continue;
            }
            selectedEffects.Add(effect);
            particles += effect.ParticleBudget;
        }

        List<LightRequest> orderedLights = new List<LightRequest>(lights);
        orderedLights.Sort((left, right) => CompareLights(
            left, right, focusX, focusY, focusZ));
        List<SelectedLight> selectedLights = new List<SelectedLight>();
        int shadowed = 0;
        for (int index = 0; index < orderedLights.Count; index++)
        {
            if (selectedLights.Count >= budget.MaximumRealtimeLights) break;
            LightRequest request = orderedLights[index];
            bool enableShadows = request.CastsShadows
                && shadowed < budget.MaximumShadowedLights;
            if (enableShadows) shadowed++;
            selectedLights.Add(new SelectedLight(request, enableShadows));
        }

        return new RenderBudgetPlan(
            new ReadOnlyCollection<EffectSpawnRequest>(selectedEffects),
            new ReadOnlyCollection<SelectedLight>(selectedLights),
            particles,
            effects.Count - selectedEffects.Count,
            lights.Count - selectedLights.Count);
    }

    private static int CompareEffects(EffectSpawnRequest left,
        EffectSpawnRequest right, double x, double y, double z)
    {
        int priority = right.Priority.CompareTo(left.Priority);
        if (priority != 0) return priority;
        int distance = DistanceSquared(left.WorldX, left.WorldY, left.WorldZ, x, y, z)
            .CompareTo(DistanceSquared(right.WorldX, right.WorldY, right.WorldZ, x, y, z));
        return distance != 0 ? distance
            : string.Compare(left.RequestId, right.RequestId, StringComparison.Ordinal);
    }

    private static int CompareLights(LightRequest left,
        LightRequest right, double x, double y, double z)
    {
        int priority = right.Priority.CompareTo(left.Priority);
        if (priority != 0) return priority;
        int distance = DistanceSquared(left.WorldX, left.WorldY, left.WorldZ, x, y, z)
            .CompareTo(DistanceSquared(right.WorldX, right.WorldY, right.WorldZ, x, y, z));
        return distance != 0 ? distance
            : string.Compare(left.RequestId, right.RequestId, StringComparison.Ordinal);
    }

    private static double DistanceSquared(double ax, double ay, double az,
        double bx, double by, double bz)
    {
        double dx = ax - bx;
        double dy = ay - by;
        double dz = az - bz;
        return dx * dx + dy * dy + dz * dz;
    }

    private static void ValidateUniqueEffects(IReadOnlyList<EffectSpawnRequest> requests)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < requests.Count; index++)
        {
            EffectSpawnRequest request = requests[index]
                ?? throw new ArgumentException("Effect request cannot be null.", nameof(requests));
            if (!ids.Add(request.RequestId))
                throw new InvalidOperationException("Duplicate effect request id.");
        }
    }

    private static void ValidateUniqueLights(IReadOnlyList<LightRequest> requests)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < requests.Count; index++)
        {
            LightRequest request = requests[index]
                ?? throw new ArgumentException("Light request cannot be null.", nameof(requests));
            if (!ids.Add(request.RequestId))
                throw new InvalidOperationException("Duplicate light request id.");
        }
    }
}
}
