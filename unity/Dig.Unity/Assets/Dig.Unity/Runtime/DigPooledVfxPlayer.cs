using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed partial class DigPooledVfxPlayer : MonoBehaviour
{
    private const int MaximumPoolSize = 64;
    private const string CatalogPath = "VisualCatalogs/Vfx";
    private readonly Dictionary<string, DigPooledVfxInstance> _active =
        new Dictionary<string, DigPooledVfxInstance>(StringComparer.Ordinal);
    private readonly Dictionary<string, Stack<DigPooledVfxInstance>> _pools =
        new Dictionary<string, Stack<DigPooledVfxInstance>>(StringComparer.Ordinal);
    private readonly List<string> _expiredIds = new List<string>();
    private RenderFrameBudget _budget = RenderFrameBudget.Default;
    private Transform? _root;
    private DigVfxCatalog? _catalog;
    private DigRenderMaterialLibrary? _materials;
    private Material? _sharedMaterial;
    private int _activeParticles;
    private int _pooledCount;

    public int ActiveCount => _active.Count;
    public int ActiveParticleCount => _activeParticles;
    public int PooledCount => _pooledCount;

    public void SetBudget(RenderFrameBudget budget)
    {
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
    }

    public void Play(IReadOnlyList<EffectSpawnRequest> requests, Camera? camera)
    {
        if (requests == null) throw new ArgumentNullException(nameof(requests));
        EnsureResources();
        Vector3 focus = camera == null ? Vector3.zero : camera.transform.position;
        RenderBudgetPlan plan = RenderBudgetPlan.Create(
            requests,
            Array.Empty<LightRequest>(),
            _budget,
            focus.x,
            focus.y,
            focus.z);
        float now = Time.unscaledTime;
        for (int index = 0; index < plan.Effects.Count; index++)
        {
            TryPresent(plan.Effects[index], now);
        }
    }

    private void TryPresent(EffectSpawnRequest request, float now)
    {
        DigPooledVfxInstance? existing;
        if (_active.TryGetValue(request.RequestId, out existing))
        {
            if (existing.Version == request.Version) return;
            Recycle(request.RequestId, existing);
        }
        if (_active.Count >= _budget.MaximumEffects
            || _activeParticles + request.ParticleBudget > _budget.MaximumParticles)
        {
            return;
        }
        DigVfxProfile? profile = ResolveProfile(request.EffectId);
        if (profile != null
            && CountActive(request.EffectId) >= profile.MaximumInstances)
        {
            return;
        }
        DigPooledVfxInstance instance = Acquire(request.EffectId, profile);
        instance.Play(request, profile, _sharedMaterial!, now);
        _active.Add(request.RequestId, instance);
        _activeParticles += instance.ParticleBudget;
    }

    private void Update()
    {
        if (_active.Count == 0) return;
        _expiredIds.Clear();
        float now = Time.unscaledTime;
        foreach (KeyValuePair<string, DigPooledVfxInstance> pair in _active)
        {
            if (pair.Value.IsExpired(now)) _expiredIds.Add(pair.Key);
        }
        for (int index = 0; index < _expiredIds.Count; index++)
        {
            string requestId = _expiredIds[index];
            Recycle(requestId, _active[requestId]);
        }
    }

    private void Recycle(string requestId, DigPooledVfxInstance instance)
    {
        _active.Remove(requestId);
        _activeParticles = Mathf.Max(
            0,
            _activeParticles - instance.ParticleBudget);
        string effectId = instance.EffectId;
        instance.StopAndHide();
        if (_pooledCount >= MaximumPoolSize)
        {
            Destroy(instance.gameObject);
            return;
        }
        Stack<DigPooledVfxInstance>? pool;
        if (!_pools.TryGetValue(effectId, out pool))
        {
            pool = new Stack<DigPooledVfxInstance>();
            _pools.Add(effectId, pool);
        }
        pool.Push(instance);
        _pooledCount++;
    }

    private int CountActive(string effectId)
    {
        int count = 0;
        foreach (DigPooledVfxInstance instance in _active.Values)
        {
            if (instance.EffectId == effectId) count++;
        }
        return count;
    }
}
}
