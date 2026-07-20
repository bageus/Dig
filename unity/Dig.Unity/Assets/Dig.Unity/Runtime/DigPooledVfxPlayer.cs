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
}
}
