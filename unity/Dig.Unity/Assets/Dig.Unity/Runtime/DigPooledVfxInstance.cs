using System;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigPooledVfxInstance : MonoBehaviour
{
    private ParticleSystem? _particles;
    private string _effectId = string.Empty;
    private string _requestId = string.Empty;
    private long _version;
    private float _expiresAt;
    private int _particleBudget;

    public string EffectId => _effectId;
    public string RequestId => _requestId;
    public long Version => _version;
    public int ParticleBudget => _particleBudget;
    public bool IsExpired(float now) => now >= _expiresAt;

    internal void Initialize(ParticleSystem particles)
    {
        _particles = particles ?? throw new ArgumentNullException(nameof(particles));
    }

    internal void Play(EffectSpawnRequest request, DigVfxProfile? profile,
        Material material, float now)
    {
        if (_particles == null) throw new InvalidOperationException("VFX instance is not initialized.");
        _effectId = request.EffectId;
        _requestId = request.RequestId;
        _version = request.Version;
        _particleBudget = profile == null
            ? request.ParticleBudget
            : Mathf.Min(request.ParticleBudget, profile.MaximumParticles);
        _expiresAt = now + (float)request.DurationSeconds;
        transform.position = new Vector3((float)request.WorldX,
            (float)request.WorldY, (float)request.WorldZ);
        transform.localScale = Vector3.one * (float)request.Scale;
        ConfigureParticles(_particles, request, material, _particleBudget);
        gameObject.SetActive(true);
        _particles.Play(withChildren: true);
    }

    internal void StopAndHide()
    {
        if (_particles != null)
            _particles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
        _requestId = string.Empty;
        _version = 0L;
        _particleBudget = 0;
    }

    private static void ConfigureParticles(ParticleSystem particles,
        EffectSpawnRequest request, Material material, int particleBudget)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = Mathf.Max(0.1f, (float)request.DurationSeconds);
        main.startLifetime = Mathf.Min(1.2f, main.duration);
        main.startSpeed = 1.2f;
        main.startSize = 0.12f;
        main.maxParticles = particleBudget;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = CategoryColor(request.Category);
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        short burstCount = (short)Mathf.Clamp(particleBudget, 1, short.MaxValue);
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
    }

    private static Color CategoryColor(VfxCategory category)
    {
        switch (category)
        {
            case VfxCategory.Excavation: return new Color(0.72f, 0.56f, 0.34f, 1f);
            case VfxCategory.Deposit: return new Color(0.42f, 0.82f, 0.90f, 1f);
            case VfxCategory.Construction: return new Color(0.92f, 0.72f, 0.24f, 1f);
            case VfxCategory.Production: return new Color(1f, 0.40f, 0.14f, 1f);
            case VfxCategory.Status: return new Color(0.38f, 0.86f, 0.46f, 1f);
            case VfxCategory.Combat: return new Color(0.94f, 0.24f, 0.18f, 1f);
            default: return new Color(0.62f, 0.68f, 0.78f, 1f);
        }
    }
}
}
