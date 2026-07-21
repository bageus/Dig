using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigRenderMaterialLibrary : MonoBehaviour
{
    private const string ResourcePath = "VisualCatalogs/RenderMaterials";
    private readonly Dictionary<string, Material> _resolved =
        new Dictionary<string, Material>(StringComparer.Ordinal);
    private readonly HashSet<Material> _ownedFallbacks = new HashSet<Material>();
    private DigRenderMaterialCatalog? _catalog;
    public bool IsScriptablePipelineActive => GraphicsSettings.currentRenderPipeline != null;
    public int CachedMaterialCount => _resolved.Count;
    public int RuntimeFallbackCount => _ownedFallbacks.Count;
    private void Awake()
    {
        DigStylizedLightingRig lighting = GetComponent<DigStylizedLightingRig>();
        if (lighting == null) lighting = gameObject.AddComponent<DigStylizedLightingRig>();
        lighting.Configure();
        if (GetComponent<DigPresentationEffectBridge>() == null)
            gameObject.AddComponent<DigPresentationEffectBridge>();
    }
    public Material Resolve(RenderMaterialSemantic semantic,
        RenderSurfaceKind surface, Color fallbackTint)
    {
        string key = semantic + ":" + surface;
        Material? material;
        if (_resolved.TryGetValue(key, out material)) return material;
        if (_catalog == null) _catalog = Resources.Load<DigRenderMaterialCatalog>(ResourcePath);
        DigRenderMaterialProfile profile;
        if (_catalog != null && _catalog.TryResolve(semantic, surface, out profile))
        {
            material = profile.Material;
            if (material != null) { _resolved.Add(key, material); return material; }
            fallbackTint = profile.FallbackTint;
        }
        material = CreateFallback(semantic, surface, fallbackTint);
        _resolved.Add(key, material); _ownedFallbacks.Add(material);
        return material;
    }
    private static Material CreateFallback(RenderMaterialSemantic semantic,
        RenderSurfaceKind surface, Color tint)
    {
        Shader? shader = ResolveShader(surface);
        if (shader == null)
            throw new InvalidOperationException("No supported render fallback shader was found.");
        Material material = new Material(shader)
        {
            name = "Dig Runtime Fallback " + semantic + " " + surface,
            color = tint, enableInstancing = true,
        };
        if (surface == RenderSurfaceKind.Overlay) material.renderQueue = 3000;
        return material;
    }
    private static Shader? ResolveShader(RenderSurfaceKind surface)
    {
        if (surface == RenderSurfaceKind.Lit)
            return Shader.Find("Dig/Stylized Lit")
                ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return Shader.Find("Dig/Stylized Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
    }
    private void OnDestroy()
    {
        foreach (Material material in _ownedFallbacks)
            if (material != null) Destroy(material);
        _ownedFallbacks.Clear(); _resolved.Clear();
    }
}
}