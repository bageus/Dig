using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTerrainChunkRenderer : MonoBehaviour
    {
        private readonly Dictionary<DigTerrainChunkKey, DigTerrainChunkVisual> _visuals =
            new Dictionary<DigTerrainChunkKey, DigTerrainChunkVisual>();
        private readonly Dictionary<DigTerrainMaterialKey, Material> _fallbackMaterials =
            new Dictionary<DigTerrainMaterialKey, Material>();
        private readonly HashSet<DigTerrainChunkKey> _visibleChunks =
            new HashSet<DigTerrainChunkKey>();
        private readonly List<DigTerrainChunkKey> _removedChunks =
            new List<DigTerrainChunkKey>();
        private Transform? _root;
        private Shader? _fallbackShader;

        internal int RebuildCount { get; private set; }
        internal int VertexCount { get; private set; }
        internal int TriangleCount { get; private set; }

        internal void Invalidate()
        {
            foreach (DigTerrainChunkVisual visual in _visuals.Values)
            {
                visual.Invalidate();
            }
        }

        internal void Render(
            DigTerrainRenderSnapshot snapshot,
            DigTerrainVisualCatalog? catalog)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            EnsureRoot();
            _visibleChunks.Clear();
            VertexCount = 0;
            TriangleCount = 0;

            for (int index = 0; index < snapshot.Chunks.Count; index++)
            {
                DigTerrainRenderChunk chunk = snapshot.Chunks[index];
                _visibleChunks.Add(chunk.Key);
                DigTerrainChunkVisual visual = GetOrCreateVisual(chunk.Key);
                if (snapshot.IsDirty(chunk.Key)
                    || !visual.IsInitialized
                    || visual.Signature == ulong.MaxValue)
                {
                    ulong signature = CalculateSignature(chunk, snapshot);
                    if (!visual.IsInitialized || visual.Signature != signature)
                    {
                        DigTerrainChunkMeshData data = DigTerrainChunkMeshBuilder.Build(
                            chunk,
                            snapshot);
                        Material[] materials = ResolveMaterials(
                            data.MaterialKeys,
                            catalog);
                        visual.Apply(chunk.Key, signature, data, materials);
                        RebuildCount++;
                    }
                }

                CountVisual(visual);
            }

            RemoveMissingChunks();
        }

        private static ulong CalculateSignature(
            DigTerrainRenderChunk chunk,
            DigTerrainRenderSnapshot snapshot)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Mix(ref hash, (ulong)(uint)chunk.Key.X, prime);
            Mix(ref hash, (ulong)(uint)chunk.Key.Y, prime);
            Mix(ref hash, (ulong)(uint)chunk.Key.Z, prime);
            Mix(ref hash, (ulong)chunk.Version, prime);

            for (int index = 0; index < chunk.Cells.Count; index++)
            {
                DigTerrainRenderCell cell = chunk.Cells[index];
                Mix(ref hash, (ulong)(uint)cell.Key.X, prime);
                Mix(ref hash, (ulong)(uint)cell.Key.Y, prime);
                Mix(ref hash, (ulong)(uint)cell.Key.Z, prime);
                Mix(ref hash, cell.IsSolid ? 1UL : 0UL, prime);
                Mix(ref hash, cell.IsExplored ? 1UL : 0UL, prime);
                Mix(ref hash, cell.IsDesignated ? 1UL : 0UL, prime);
                Mix(ref hash, (ulong)(uint)cell.Hardness, prime);
                Mix(ref hash, snapshot.IsCutaway(cell.Key) ? 1UL : 0UL, prime);
                Mix(ref hash, snapshot.IsProtected(cell.Key) ? 1UL : 0UL, prime);
                MixNeighbour(ref hash, cell.Key.Offset(-1, 0, 0), snapshot, prime);
                MixNeighbour(ref hash, cell.Key.Offset(1, 0, 0), snapshot, prime);
                MixNeighbour(ref hash, cell.Key.Offset(0, -1, 0), snapshot, prime);
                MixNeighbour(ref hash, cell.Key.Offset(0, 1, 0), snapshot, prime);
                MixNeighbour(ref hash, cell.Key.Offset(0, 0, -1), snapshot, prime);
                MixNeighbour(ref hash, cell.Key.Offset(0, 0, 1), snapshot, prime);
                for (int character = 0; character < cell.MaterialId.Length; character++)
                {
                    Mix(ref hash, cell.MaterialId[character], prime);
                }
            }

            return hash;
        }

        private static void MixNeighbour(
            ref ulong hash,
            DigTerrainCellKey cell,
            DigTerrainRenderSnapshot snapshot,
            ulong prime)
        {
            Mix(ref hash, snapshot.IsRenderedSolid(cell) ? 1UL : 0UL, prime);
        }

        private static void Mix(ref ulong hash, ulong value, ulong prime)
        {
            hash ^= value;
            hash *= prime;
        }

        private DigTerrainChunkVisual GetOrCreateVisual(DigTerrainChunkKey chunk)
        {
            if (_visuals.TryGetValue(chunk, out DigTerrainChunkVisual? visual))
            {
                return visual;
            }

            GameObject target = new GameObject($"Terrain chunk {chunk}");
            target.transform.SetParent(_root, worldPositionStays: false);
            visual = target.AddComponent<DigTerrainChunkVisual>();
            _visuals.Add(chunk, visual);
            return visual;
        }

        private Material[] ResolveMaterials(
            IReadOnlyList<DigTerrainMaterialKey> keys,
            DigTerrainVisualCatalog? catalog)
        {
            Material[] materials = new Material[keys.Count];
            for (int index = 0; index < keys.Count; index++)
            {
                DigTerrainMaterialKey key = keys[index];
                Material? material = null;
                if (key.State == DigTerrainSurfaceState.Solid && catalog != null)
                {
                    material = catalog.ResolveSurface(key.MaterialId, key.Role);
                }

                materials[index] = material ?? ResolveFallbackMaterial(key);
            }

            return materials;
        }

        private Material ResolveFallbackMaterial(DigTerrainMaterialKey key)
        {
            if (_fallbackMaterials.TryGetValue(key, out Material? material))
            {
                return material;
            }

            EnsureFallbackShader();
            material = new Material(_fallbackShader!)
            {
                name = $"Terrain fallback {key}",
                color = ResolveFallbackColor(key),
            };
            _fallbackMaterials.Add(key, material);
            return material;
        }

        private static Color ResolveFallbackColor(DigTerrainMaterialKey key)
        {
            switch (key.State)
            {
                case DigTerrainSurfaceState.Unexplored:
                    return new Color(0.08f, 0.09f, 0.12f, 1f);
                case DigTerrainSurfaceState.Designated:
                    return new Color(0.95f, 0.47f, 0.12f, 1f);
                case DigTerrainSurfaceState.Protected:
                    return new Color(0.18f, 0.22f, 0.28f, 1f);
                default:
                    return ResolveSolidFallbackColor(key);
            }
        }

        private static Color ResolveSolidFallbackColor(DigTerrainMaterialKey key)
        {
            float hardness = key.Shade / 7f;
            Color baseColor = Color.Lerp(
                new Color(0.48f, 0.36f, 0.25f, 1f),
                new Color(0.32f, 0.36f, 0.42f, 1f),
                hardness);
            switch (key.Role)
            {
                case DigTerrainSurfaceRole.Floor:
                    return Color.Lerp(baseColor, Color.white, 0.08f);
                case DigTerrainSurfaceRole.Ceiling:
                    return Color.Lerp(baseColor, Color.black, 0.12f);
                case DigTerrainSurfaceRole.FreshCut:
                    return Color.Lerp(
                        baseColor,
                        new Color(0.62f, 0.43f, 0.28f, 1f),
                        0.22f);
                default:
                    return baseColor;
            }
        }

        private void CountVisual(DigTerrainChunkVisual visual)
        {
            Mesh? mesh = visual.ResolveMesh();
            if (mesh == null)
            {
                return;
            }

            VertexCount += mesh.vertexCount;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                TriangleCount += (int)mesh.GetIndexCount(submesh) / 3;
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("Chunked Terrain Visuals").transform;
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
                    "Chunked terrain requires a compatible lit shader.");
            }
        }

        private void RemoveMissingChunks()
        {
            _removedChunks.Clear();
            foreach (DigTerrainChunkKey chunk in _visuals.Keys)
            {
                if (!_visibleChunks.Contains(chunk))
                {
                    _removedChunks.Add(chunk);
                }
            }

            for (int index = 0; index < _removedChunks.Count; index++)
            {
                DigTerrainChunkKey chunk = _removedChunks[index];
                DigTerrainChunkVisual visual = _visuals[chunk];
                _visuals.Remove(chunk);
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
