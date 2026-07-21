using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed partial class DigTerrainChunkRenderer : MonoBehaviour
    {
        private readonly Dictionary<DigTerrainChunkKey, DigTerrainChunkVisual> _visuals =
            new Dictionary<DigTerrainChunkKey, DigTerrainChunkVisual>();
        private DigRenderMaterialLibrary? _materials;
        private readonly HashSet<DigTerrainChunkKey> _visibleChunks =
            new HashSet<DigTerrainChunkKey>();
        private readonly List<DigTerrainChunkKey> _removedChunks =
            new List<DigTerrainChunkKey>();
        private Transform? _root;
        private DigTerrainRenderSnapshot? _lastSnapshot;
        private DigTerrainVisualCatalog? _lastCatalog;
        private TerrainVisualDetailLevel _detailLevel = TerrainVisualDetailLevel.Full;

        internal int RebuildCount { get; private set; }
        internal int VertexCount { get; private set; }
        internal int TriangleCount { get; private set; }
        internal TerrainVisualDetailLevel DetailLevel => _detailLevel;

        internal void Invalidate()
        {
            foreach (DigTerrainChunkVisual visual in _visuals.Values)
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
            if (_lastSnapshot != null)
            {
                Render(_lastSnapshot, _lastCatalog);
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

            _lastSnapshot = snapshot;
            _lastCatalog = catalog;
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
                    ulong signature = CalculateSignature(
                        chunk,
                        snapshot,
                        _detailLevel);
                    if (!visual.IsInitialized || visual.Signature != signature)
                    {
                        DigTerrainChunkMeshData data = DigTerrainChunkMeshBuilder.Build(
                            chunk,
                            snapshot,
                            catalog,
                            _detailLevel);
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
            DigTerrainRenderSnapshot snapshot,
            TerrainVisualDetailLevel detailLevel)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            Mix(ref hash, (ulong)(uint)chunk.Key.X, prime);
            Mix(ref hash, (ulong)(uint)chunk.Key.Y, prime);
            Mix(ref hash, (ulong)(uint)chunk.Key.Z, prime);
            Mix(ref hash, (ulong)chunk.Version, prime);
            Mix(ref hash, (ulong)detailLevel, prime);

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
                MixDeposit(ref hash, cell.Key, snapshot, prime);
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
                    material = key.HasVisibleDeposit
                        ? catalog.ResolveDeposit(key.DepositId, key.DepositState)
                        : catalog.ResolveSurface(key.MaterialId, key.Role);
                }

                materials[index] = material ?? ResolveFallbackMaterial(key);
            }

            return materials;
        }

        private Material ResolveFallbackMaterial(DigTerrainMaterialKey key)
        {
            if (_materials == null)
            {
                _materials = GetComponent<DigRenderMaterialLibrary>();
                if (_materials == null)
                    _materials = gameObject.AddComponent<DigRenderMaterialLibrary>();
            }
            return _materials.Resolve(
                RenderMaterialSemantic.Terrain,
                RenderSurfaceKind.Lit,
                Color.white);
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

    }
}
