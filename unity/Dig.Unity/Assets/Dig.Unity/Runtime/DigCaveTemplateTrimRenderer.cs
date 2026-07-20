using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigCaveTemplateTrimRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigCaveTemplateTrimVisual> _visuals =
            new Dictionary<string, DigCaveTemplateTrimVisual>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _fallbackMaterials =
            new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly HashSet<string> _visibleInstances =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _removedInstances = new List<string>();
        private Transform? _root;
        private Shader? _fallbackShader;
        private CaveTemplateTrimVolumeViewModel? _lastVolume;
        private DigTerrainVisualCatalog? _lastCatalog;
        private TerrainVisualDetailLevel _detailLevel = TerrainVisualDetailLevel.Full;

        internal int InstanceCount => _visuals.Count;
        internal int RebuildCount { get; private set; }
        internal int VertexCount { get; private set; }
        internal int TriangleCount { get; private set; }
        internal TerrainVisualDetailLevel DetailLevel => _detailLevel;

        internal void Invalidate()
        {
            foreach (DigCaveTemplateTrimVisual visual in _visuals.Values)
            {
                visual.Invalidate();
            }
        }

        internal void SetDetailLevel(TerrainVisualDetailLevel detailLevel)
        {
            if (!Enum.IsDefined(typeof(TerrainVisualDetailLevel), detailLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(detailLevel));
            }

            if (_detailLevel == detailLevel)
            {
                return;
            }

            _detailLevel = detailLevel;
            Invalidate();
            if (_lastVolume != null)
            {
                Render(_lastVolume, _lastCatalog);
            }
        }

        internal void Render(
            CaveTemplateTrimVolumeViewModel volume,
            DigTerrainVisualCatalog? catalog)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            _lastVolume = volume;
            _lastCatalog = catalog;
            EnsureRoot();
            _visibleInstances.Clear();
            VertexCount = 0;
            TriangleCount = 0;
            for (int index = 0; index < volume.Instances.Count; index++)
            {
                CaveTemplateTrimInstanceViewModel instance = volume.Instances[index];
                _visibleInstances.Add(instance.InstanceId);
                DigCaveTemplateTrimVisual visual = GetOrCreateVisual(instance);
                ulong signature = CalculateSignature(instance, _detailLevel);
                if (!visual.IsInitialized || visual.Signature != signature)
                {
                    DigCaveTemplateTrimMeshData data =
                        DigCaveTemplateTrimMeshBuilder.Build(instance, _detailLevel);
                    visual.Apply(
                        signature,
                        data,
                        ResolveMaterials(instance.TemplateId, data, catalog));
                    RebuildCount++;
                }

                CountVisual(visual);
            }

            RemoveMissingInstances();
        }

        private DigCaveTemplateTrimVisual GetOrCreateVisual(
            CaveTemplateTrimInstanceViewModel instance)
        {
            if (_visuals.TryGetValue(
                    instance.InstanceId,
                    out DigCaveTemplateTrimVisual? visual))
            {
                return visual;
            }

            GameObject target = new GameObject(
                $"Cave template trim {instance.InstanceId}");
            target.transform.SetParent(_root, worldPositionStays: false);
            visual = target.AddComponent<DigCaveTemplateTrimVisual>();
            _visuals.Add(instance.InstanceId, visual);
            return visual;
        }

        private Material[] ResolveMaterials(
            string templateId,
            DigCaveTemplateTrimMeshData data,
            DigTerrainVisualCatalog? catalog)
        {
            Material[] materials = new Material[data.MaterialRoles.Length];
            for (int index = 0; index < data.MaterialRoles.Length; index++)
            {
                CaveTemplateTrimRole role = data.MaterialRoles[index];
                Material? material = catalog?.ResolveCaveTemplate(templateId, role);
                materials[index] = material
                    ?? ResolveFallbackMaterial(templateId, role);
            }

            return materials;
        }

        private Material ResolveFallbackMaterial(
            string templateId,
            CaveTemplateTrimRole role)
        {
            string key = $"{templateId}:{role}";
            if (_fallbackMaterials.TryGetValue(key, out Material? material))
            {
                return material;
            }

            EnsureFallbackShader();
            material = new Material(_fallbackShader!)
            {
                name = $"Cave template fallback {key}",
                color = ResolveFallbackColor(templateId, role),
            };
            _fallbackMaterials.Add(key, material);
            return material;
        }

        private static Color ResolveFallbackColor(
            string templateId,
            CaveTemplateTrimRole role)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < templateId.Length; index++)
                {
                    hash ^= templateId[index];
                    hash *= 16777619u;
                }

                float hue = 0.06f + ((hash & 255u) / 255f * 0.10f);
                float value;
                switch (role)
                {
                    case CaveTemplateTrimRole.Entrance:
                        value = 0.82f;
                        break;
                    case CaveTemplateTrimRole.Arch:
                        value = 0.72f;
                        break;
                    case CaveTemplateTrimRole.SideWall:
                        value = 0.58f;
                        break;
                    default:
                        value = 0.46f;
                        break;
                }

                return Color.HSVToRGB(hue, 0.40f, value);
            }
        }

        private static ulong CalculateSignature(
            CaveTemplateTrimInstanceViewModel instance,
            TerrainVisualDetailLevel detailLevel)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            MixString(ref hash, instance.InstanceId, prime);
            MixString(ref hash, instance.TemplateId, prime);
            Mix(ref hash, (ulong)(uint)instance.Entrance.X, prime);
            Mix(ref hash, (ulong)(uint)instance.Entrance.Y, prime);
            Mix(ref hash, (ulong)(uint)instance.Depth, prime);
            Mix(ref hash, instance.Variant, prime);
            Mix(ref hash, instance.HasBackWall ? 1UL : 0UL, prime);
            Mix(ref hash, (ulong)detailLevel, prime);
            for (int index = 0; index < instance.Rows.Count; index++)
            {
                CaveTemplateTrimRowViewModel row = instance.Rows[index];
                Mix(ref hash, (ulong)(uint)row.Level, prime);
                Mix(ref hash, (ulong)(uint)row.MinX, prime);
                Mix(ref hash, (ulong)(uint)row.Y, prime);
                Mix(ref hash, (ulong)(uint)row.Width, prime);
            }

            for (int index = 0; index < instance.ArchDepths.Count; index++)
            {
                Mix(ref hash, (ulong)(uint)instance.ArchDepths[index], prime);
            }

            return hash;
        }

        private static void MixString(ref ulong hash, string value, ulong prime)
        {
            for (int index = 0; index < value.Length; index++)
            {
                Mix(ref hash, value[index], prime);
            }
        }

        private static void Mix(ref ulong hash, ulong value, ulong prime)
        {
            hash ^= value;
            hash *= prime;
        }

        private void CountVisual(DigCaveTemplateTrimVisual visual)
        {
            Mesh? mesh = visual.ResolveMesh();
            if (mesh == null)
            {
                return;
            }

            VertexCount += mesh.vertexCount;
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                TriangleCount += (int)mesh.GetIndexCount(index) / 3;
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("Cave Template Trim Visuals").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }

        private void EnsureFallbackShader()
        {
            if (_fallbackShader != null)
            {
                return;
            }

            _fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_fallbackShader == null)
            {
                _fallbackShader = Shader.Find("Standard");
            }

            if (_fallbackShader == null)
            {
                throw new InvalidOperationException(
                    "Cave template trim requires a compatible lit shader.");
            }
        }

        private void RemoveMissingInstances()
        {
            _removedInstances.Clear();
            foreach (string instanceId in _visuals.Keys)
            {
                if (!_visibleInstances.Contains(instanceId))
                {
                    _removedInstances.Add(instanceId);
                }
            }

            for (int index = 0; index < _removedInstances.Count; index++)
            {
                string instanceId = _removedInstances[index];
                DigCaveTemplateTrimVisual visual = _visuals[instanceId];
                _visuals.Remove(instanceId);
                Destroy(visual.gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _fallbackMaterials.Values)
            {
                Destroy(material);
            }
        }
    }
}
