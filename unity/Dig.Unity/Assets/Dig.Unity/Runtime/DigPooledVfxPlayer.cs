using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigPooledVfxPlayer : MonoBehaviour
{
    private const string CatalogPath = "VisualCatalogs/Vfx";
    private const int MaximumPoolSize = 64;
    private readonly Dictionary<string, DigPooledVfxInstance> _active =
        new Dictionary<string, DigPooledVfxInstance>(StringComparer.Ordinal);
    private readonly Dictionary<string, Stack<DigPooledVfxInstance>> _pools =
        new Dictionary<string, Stack<DigPooledVfxInstance>>(StringComparer.Ordinal);
    private readonly List<string> _expiredIds = new List<string>();
    private Transform? _root;
    private DigVfxCatalog? _catalog;
    private DigRenderMaterialLibrary? _materials;
    private Material? _sharedMaterial;
    private RenderFrameBudget _budget = RenderFrameBudget.Default;
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
        RenderBudgetPlan plan = RenderBudgetPlan.Create(requests,
            Array.Empty<LightRequest>(), _budget, focus.x, focus.y, focus.z);
        float now = Time.unscaledTime;
        for (int index = 0; index < plan.Effects.Count; index++)
            PlayOne(plan.Effects[index], now);
    }

    private void PlayOne(EffectSpawnRequest request, float now)
    {
        DigPooledVfxInstance? existing;
        if (_active.TryGetValue(request.RequestId, out existing))
        {
            if (existing.Version == request.Version) return;
            Release(request.RequestId, existing);
        }
        if (_active.Count >= _budget.MaximumEffects
            || _activeParticles + request.ParticleBudget > _budget.MaximumParticles)
            return;

        DigVfxProfile? profile = ResolveProfile(request.EffectId);
        if (profile != null && CountActive(request.EffectId) >= profile.MaximumInstances)
            return;
        DigPooledVfxInstance instance = Acquire(request.EffectId, profile);
        instance.Play(request, profile, _sharedMaterial!, now);
        _active.Add(request.RequestId, instance);
        _activeParticles += instance.ParticleBudget;
    }

    private DigPooledVfxInstance Acquire(string effectId, DigVfxProfile? profile)
    {
        Stack<DigPooledVfxInstance>? pool;
        if (_pools.TryGetValue(effectId, out pool) && pool.Count > 0)
        {
            _pooledCount--;
            return pool.Pop();
        }
        return CreateInstance(effectId, profile);
    }

    private DigPooledVfxInstance CreateInstance(string effectId, DigVfxProfile? profile)
    {
        GameObject root;
        ParticleSystem? particles = null;
        if (profile?.Prefab != null)
        {
            root = Instantiate(profile.Prefab);
            particles = root.GetComponentInChildren<ParticleSystem>(includeInactive: true);
            if (particles == null)
            {
                Destroy(root);
                root = CreateFallbackRoot(effectId, out particles);
            }
        }
        else
        {
            root = CreateFallbackRoot(effectId, out particles);
        }
        root.name = "VFX " + effectId;
        root.transform.SetParent(_root, worldPositionStays: true);
        DisableColliders(root);
        DigPooledVfxInstance instance = root.GetComponent<DigPooledVfxInstance>();
        if (instance == null) instance = root.AddComponent<DigPooledVfxInstance>();
        instance.Initialize(particles!);
        return instance;
    }

    private static GameObject CreateFallbackRoot(string effectId,
        out ParticleSystem particles)
    {
        GameObject root = new GameObject("Fallback VFX " + effectId);
        root.AddComponent<DigVisualPrefabRoot>();
        particles = root.AddComponent<ParticleSystem>();
        return root;
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
            string id = _expiredIds[index];
            Release(id, _active[id]);
        }
    }

    private void Release(string requestId, DigPooledVfxInstance instance)
    {
        _active.Remove(requestId);
        _activeParticles = Mathf.Max(0, _activeParticles - instance.ParticleBudget);
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

    private DigVfxProfile? ResolveProfile(string effectId)
    {
        DigVfxProfile profile;
        return _catalog != null && _catalog.TryResolve(effectId, out profile)
            ? profile : null;
    }

    private int CountActive(string effectId)
    {
        int count = 0;
        foreach (DigPooledVfxInstance instance in _active.Values)
        {
            if (string.Equals(instance.EffectId, effectId, StringComparison.Ordinal)) count++;
        }
        return count;
    }

    private void EnsureResources()
    {
        if (_root == null)
        {
            _root = new GameObject("Pooled VFX").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }
        if (_catalog == null) _catalog = Resources.Load<DigVfxCatalog>(CatalogPath);
        if (_materials == null) _materials = GetComponent<DigRenderMaterialLibrary>();
        if (_materials == null) _materials = gameObject.AddComponent<DigRenderMaterialLibrary>();
        if (_sharedMaterial == null)
            _sharedMaterial = _materials.Resolve(RenderMaterialSemantic.Vfx,
                RenderSurfaceKind.Unlit, Color.white);
    }

    private static void DisableColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
        for (int index = 0; index < colliders.Length; index++) colliders[index].enabled = false;
    }
}
}
