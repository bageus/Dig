using System;
using Dig.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dig.Unity.Editor
{
[InitializeOnLoad]
internal static class DigUrpProjectConfigurator
{
    private const string SettingsFolder = "Assets/Dig.Unity/Settings";
    private const string ResourceFolder = "Assets/Dig.Unity/Resources/VisualCatalogs";
    private const string MaterialFolder = ResourceFolder + "/Materials";
    private const string VfxFolder = ResourceFolder + "/VfxPrefabs";
    private const string RendererPath = SettingsFolder + "/DigUniversalRenderer.asset";
    private const string PipelinePath = SettingsFolder + "/DigUniversalRenderPipeline.asset";
    private const string MaterialCatalogPath = ResourceFolder + "/RenderMaterials.asset";
    private const string VfxCatalogPath = ResourceFolder + "/Vfx.asset";

    private static readonly VfxSpec[] VfxProfiles =
    {
        new VfxSpec("vfx.excavation.impact", 0, 12, 64),
        new VfxSpec("vfx.deposit.reveal", 1, 8, 80),
        new VfxSpec("vfx.deposit.crystal-glow", 1, 8, 48),
        new VfxSpec("vfx.construction.progress", 2, 10, 48),
        new VfxSpec("vfx.production.pulse", 3, 8, 48),
        new VfxSpec("vfx.production.campfire", 3, 8, 48),
        new VfxSpec("vfx.production.building", 3, 8, 48),
        new VfxSpec("vfx.status.pulse", 4, 12, 40),
        new VfxSpec("vfx.combat.impact", 5, 16, 96),
        new VfxSpec("vfx.ambient.dust", 6, 8, 32),
        new VfxSpec("vfx.ambient.lava", 6, 8, 40),
    };

    static DigUrpProjectConfigurator()
    {
        EditorApplication.delayCall += EnsureConfiguration;
    }

    [MenuItem("Tools/Dig/Configure Stylized URP Pipeline")]
    internal static void EnsureConfiguration()
    {
        EnsureFolder(SettingsFolder);
        EnsureFolder(ResourceFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(VfxFolder);

        UniversalRendererData renderer = LoadOrCreateRenderer();
        UniversalRenderPipelineAsset pipeline = LoadOrCreatePipeline(renderer);
        AssignPipeline(pipeline);
        Material lit = LoadOrCreateMaterial("DigStylizedLit", "Dig/Stylized Lit");
        Material unlit = LoadOrCreateMaterial("DigStylizedUnlit", "Dig/Stylized Unlit");
        EnsureMaterialCatalog(lit, unlit);
        EnsureVfxCatalog(unlit);
        AssetDatabase.SaveAssets();
    }

    private static UniversalRendererData LoadOrCreateRenderer()
    {
        UniversalRendererData? renderer =
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer != null) return renderer;
        renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        renderer.name = "Dig Universal Renderer";
        AssetDatabase.CreateAsset(renderer, RendererPath);
        return renderer;
    }

    private static UniversalRenderPipelineAsset LoadOrCreatePipeline(
        UniversalRendererData renderer)
    {
        UniversalRenderPipelineAsset? pipeline =
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline != null) return pipeline;
        pipeline = UniversalRenderPipelineAsset.Create(renderer);
        pipeline.name = "Dig Universal Render Pipeline";
        pipeline.renderScale = 1f;
        pipeline.shadowDistance = 36f;
        pipeline.supportsCameraDepthTexture = true;
        pipeline.supportsCameraOpaqueTexture = false;
        AssetDatabase.CreateAsset(pipeline, PipelinePath);
        return pipeline;
    }

    private static void AssignPipeline(UniversalRenderPipelineAsset pipeline)
    {
        if (GraphicsSettings.defaultRenderPipeline != pipeline)
            GraphicsSettings.defaultRenderPipeline = pipeline;
        if (QualitySettings.renderPipeline != pipeline)
            QualitySettings.renderPipeline = pipeline;
    }

    private static Material LoadOrCreateMaterial(string name, string shaderName)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material? material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find(shaderName)
            ?? throw new InvalidOperationException("Missing shader '" + shaderName + "'.");
        material = new Material(shader) { name = name, enableInstancing = true };
        material.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureMaterialCatalog(Material lit, Material unlit)
    {
        if (AssetDatabase.LoadAssetAtPath<DigRenderMaterialCatalog>(MaterialCatalogPath)
            != null) return;
        DigRenderMaterialCatalog catalog =
            ScriptableObject.CreateInstance<DigRenderMaterialCatalog>();
        SerializedObject serialized = new SerializedObject(catalog);
        SerializedProperty profiles = serialized.FindProperty("profiles");
        int[] semantics = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        int[] surfaces = { 0, 0, 2, 0, 0, 0, 0, 2, 1, 1 };
        profiles.arraySize = semantics.Length;
        for (int index = 0; index < semantics.Length; index++)
        {
            SerializedProperty profile = profiles.GetArrayElementAtIndex(index);
            profile.FindPropertyRelative("semantic").enumValueIndex = semantics[index];
            profile.FindPropertyRelative("surface").enumValueIndex = surfaces[index];
            profile.FindPropertyRelative("material").objectReferenceValue =
                surfaces[index] == 0 ? lit : unlit;
            profile.FindPropertyRelative("fallbackTint").colorValue = Color.magenta;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(catalog, MaterialCatalogPath);
    }

    private static void EnsureVfxCatalog(Material material)
    {
        if (AssetDatabase.LoadAssetAtPath<DigVfxCatalog>(VfxCatalogPath) != null) return;
        DigVfxCatalog catalog = ScriptableObject.CreateInstance<DigVfxCatalog>();
        SerializedObject serialized = new SerializedObject(catalog);
        SerializedProperty profiles = serialized.FindProperty("profiles");
        profiles.arraySize = VfxProfiles.Length;
        for (int index = 0; index < VfxProfiles.Length; index++)
        {
            VfxSpec spec = VfxProfiles[index];
            SerializedProperty profile = profiles.GetArrayElementAtIndex(index);
            profile.FindPropertyRelative("stableId").stringValue = spec.Id;
            profile.FindPropertyRelative("category").enumValueIndex = spec.Category;
            profile.FindPropertyRelative("prefab").objectReferenceValue =
                LoadOrCreateVfxPrefab(spec, material);
            profile.FindPropertyRelative("maximumInstances").intValue = spec.Instances;
            profile.FindPropertyRelative("maximumParticles").intValue = spec.Particles;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(catalog, VfxCatalogPath);
    }

    private static GameObject LoadOrCreateVfxPrefab(VfxSpec spec, Material material)
    {
        string fileName = spec.Id.Replace('.', '_') + ".prefab";
        string path = VfxFolder + "/" + fileName;
        GameObject? prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) return prefab;
        GameObject root = new GameObject(spec.Id);
        root.AddComponent<DigVisualPrefabRoot>();
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = spec.Particles;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    private readonly struct VfxSpec
    {
        internal VfxSpec(string id, int category, int instances, int particles)
        { Id = id; Category = category; Instances = instances; Particles = particles; }
        internal string Id { get; }
        internal int Category { get; }
        internal int Instances { get; }
        internal int Particles { get; }
    }
}
}
