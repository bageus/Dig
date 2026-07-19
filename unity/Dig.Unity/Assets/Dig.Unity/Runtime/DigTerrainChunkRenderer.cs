using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTerrainChunkRenderer : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, DigTerrainChunkVisual> _visuals =
            new Dictionary<Vector2Int, DigTerrainChunkVisual>();
        private readonly Dictionary<Vector2Int, List<DigCellVisual>> _cellsByChunk =
            new Dictionary<Vector2Int, List<DigCellVisual>>();
        private readonly Dictionary<Transform, Vector2Int> _chunkByRoot =
            new Dictionary<Transform, Vector2Int>();
        private readonly Dictionary<DigTerrainMaterialKey, Material> _fallbackMaterials =
            new Dictionary<DigTerrainMaterialKey, Material>();
        private readonly HashSet<Vector2Int> _visibleChunks =
            new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _removedChunks =
            new List<Vector2Int>();
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
            IReadOnlyDictionary<Vector2Int, DigCellVisual> cells,
            IReadOnlyDictionary<Vector2Int, Transform> chunks,
            ISet<Vector2Int> solidCells,
            ISet<Vector2Int> cutawayCells,
            ISet<Vector2Int> protectedCells,
            DigTerrainVisualCatalog? catalog)
        {
            EnsureRoot();
            PrepareChunkCells(cells, chunks);
            _visibleChunks.Clear();
            VertexCount = 0;
            TriangleCount = 0;

            foreach (KeyValuePair<Vector2Int, Transform> pair in chunks)
            {
                Vector2Int chunk = pair.Key;
                _visibleChunks.Add(chunk);
                List<DigCellVisual> chunkCells = _cellsByChunk[chunk];
                chunkCells.Sort(CompareCells);
                ulong signature = CalculateSignature(
                    chunkCells,
                    solidCells,
                    cutawayCells,
                    protectedCells);
                DigTerrainChunkVisual visual = GetOrCreateVisual(chunk);
                if (visual.Signature != signature)
                {
                    DigTerrainChunkMeshData data = DigTerrainChunkMeshBuilder.Build(
                        chunkCells,
                        solidCells,
                        cutawayCells,
                        protectedCells);
                    Material[] materials = ResolveMaterials(data.MaterialKeys, catalog);
                    visual.Apply(chunk, signature, data, materials);
                    RebuildCount++;
                }

                MeshFilter? filter = visual.GetComponent<MeshFilter>();
                Mesh? mesh = filter == null ? null : filter.sharedMesh;
                if (mesh != null)
                {
                    VertexCount += mesh.vertexCount;
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        TriangleCount += (int)mesh.GetIndexCount(submesh) / 3;
                    }
                }
            }

            RemoveMissingChunks();
        }

        private void PrepareChunkCells(
            IReadOnlyDictionary<Vector2Int, DigCellVisual> cells,
            IReadOnlyDictionary<Vector2Int, Transform> chunks)
        {
            _chunkByRoot.Clear();
            foreach (KeyValuePair<Vector2Int, Transform> pair in chunks)
            {
                _chunkByRoot[pair.Value] = pair.Key;
                if (!_cellsByChunk.TryGetValue(pair.Key, out List<DigCellVisual>? list))
                {
                    list = new List<DigCellVisual>();
                    _cellsByChunk.Add(pair.Key, list);
                }

                list.Clear();
            }

            foreach (DigCellVisual cell in cells.Values)
            {
                Transform? parent = cell.transform.parent;
                if (parent != null
                    && _chunkByRoot.TryGetValue(parent, out Vector2Int chunk))
                {
                    _cellsByChunk[chunk].Add(cell);
                }
            }
        }

        private static int CompareCells(DigCellVisual left, DigCellVisual right)
        {
            int y = left.Model.Y.CompareTo(right.Model.Y);
            return y != 0 ? y : left.Model.X.CompareTo(right.Model.X);
        }

        private static ulong CalculateSignature(
            IReadOnlyList<DigCellVisual> cells,
            ISet<Vector2Int> solidCells,
            ISet<Vector2Int> cutawayCells,
            ISet<Vector2Int> protectedCells)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int index = 0; index < cells.Count; index++)
            {
                WorldCellViewModel cell = cells[index].Model;
                Vector2Int key = new Vector2Int(cell.X, cell.Y);
                Mix(ref hash, (ulong)(uint)cell.X, prime);
                Mix(ref hash, (ulong)(uint)cell.Y, prime);
                Mix(ref hash, cell.IsSolid ? 1UL : 0UL, prime);
                Mix(ref hash, cell.IsExplored ? 1UL : 0UL, prime);
                Mix(ref hash, cell.IsDesignated ? 1UL : 0UL, prime);
                Mix(ref hash, (ulong)(uint)cell.Hardness, prime);
                Mix(ref hash, cutawayCells.Contains(key) ? 1UL : 0UL, prime);
                Mix(ref hash, protectedCells.Contains(key) ? 1UL : 0UL, prime);
                Mix(ref hash, IsRenderedSolid(
                    cell.X - 1,
                    cell.Y,
                    solidCells,
                    cutawayCells) ? 1UL : 0UL, prime);
                Mix(ref hash, IsRenderedSolid(
                    cell.X + 1,
                    cell.Y,
                    solidCells,
                    cutawayCells) ? 1UL : 0UL, prime);
                Mix(ref hash, IsRenderedSolid(
                    cell.X,
                    cell.Y - 1,
                    solidCells,
                    cutawayCells) ? 1UL : 0UL, prime);
                Mix(ref hash, IsRenderedSolid(
                    cell.X,
                    cell.Y + 1,
                    solidCells,
                    cutawayCells) ? 1UL : 0UL, prime);
                for (int character = 0; character < cell.MaterialId.Length; character++)
                {
                    Mix(ref hash, cell.MaterialId[character], prime);
                }
            }

            return hash;
        }

        private static bool IsRenderedSolid(
            int x,
            int y,
            ISet<Vector2Int> solidCells,
            ISet<Vector2Int> cutawayCells)
        {
            Vector2Int cell = new Vector2Int(x, y);
            return solidCells.Contains(cell) && !cutawayCells.Contains(cell);
        }

        private static void Mix(ref ulong hash, ulong value, ulong prime)
        {
            hash ^= value;
            hash *= prime;
        }

        private DigTerrainChunkVisual GetOrCreateVisual(Vector2Int chunk)
        {
            if (_visuals.TryGetValue(chunk, out DigTerrainChunkVisual? visual))
            {
                return visual;
            }

            GameObject target = new GameObject($"Terrain chunk {chunk.x},{chunk.y}");
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
                    material = catalog.Resolve(key.MaterialId).Material;
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
                    float hardness = key.Shade / 7f;
                    return Color.Lerp(
                        new Color(0.48f, 0.36f, 0.25f, 1f),
                        new Color(0.32f, 0.36f, 0.42f, 1f),
                        hardness);
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
            foreach (Vector2Int chunk in _visuals.Keys)
            {
                if (!_visibleChunks.Contains(chunk))
                {
                    _removedChunks.Add(chunk);
                }
            }

            for (int index = 0; index < _removedChunks.Count; index++)
            {
                Vector2Int chunk = _removedChunks[index];
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
