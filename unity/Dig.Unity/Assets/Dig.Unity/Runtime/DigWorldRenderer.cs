using System;
using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigWorldRenderer : MonoBehaviour
    {
        private readonly Dictionary<CellId, DigCellVisual> _cells =
            new Dictionary<CellId, DigCellVisual>();
        private readonly Dictionary<ChunkId, Transform> _chunks =
            new Dictionary<ChunkId, Transform>();
        private readonly HashSet<CellId> _visibleCells =
            new HashSet<CellId>();
        private readonly HashSet<CellId> _visibleChunks =
            new HashSet<CellId>();
        private readonly HashSet<CellId> _solidCells =
            new HashSet<CellId>();
        private readonly HashSet<CellId> _walkSurfaceCells =
            new HashSet<CellId>();
        private readonly HashSet<CellId> _tunnelCutaway =
            new HashSet<CellId>();
        private readonly List<CellId> _removedCells =
            new List<CellId>();
        private readonly List<CellId> _removedChunks =
            new List<CellId>();
        private Transform? _visualRoot;
        private DigCellVisual? _selected;

        internal WorldCellViewModel? SelectedModel => _selected?.Model;

        public void Render(WorldViewModel world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            CellId? selectedCoordinates = _selected == null
                ? (CellId?)null
                : new CellId(
                    _selected.Model.X,
                    _selected.Model.Y,
                    _selected.Model.Z);
            EnsureRoot();
            _visibleCells.Clear();
            _visibleChunks.Clear();
            _solidCells.Clear();
            _walkSurfaceCells.Clear();

            foreach (WorldChunkViewModel chunk in world.Chunks)
            {
                foreach (WorldCellViewModel cell in chunk.Cells)
                {
                    if (cell.IsSolid)
                    {
                        _solidCells.Add(new CellId(cell.X, cell.Y, cell.Z));
                    }
                }
            }

            foreach (WorldChunkViewModel chunk in world.Chunks)
            {
                ChunkId chunkKey = new ChunkId(chunk.X, chunk.Y, chunk.Z);
                _visibleChunks.Add(chunkKey);
                Transform chunkRoot = GetOrCreateChunkRoot(chunkKey, chunk.Version);
                foreach (WorldCellViewModel cell in chunk.Cells)
                {
                    CellId cellKey = new CellId(cell.X, cell.Y, cell.Z);
                    _visibleCells.Add(cellKey);
                    bool walkSurface = !cell.IsSolid
                        && _solidCells.Contains(new CellId(cell.X, cell.Y + 1, cell.Z));
                    if (walkSurface)
                    {
                        _walkSurfaceCells.Add(cellKey);
                    }

                    if (!_cells.TryGetValue(cellKey, out DigCellVisual? visual))
                    {
                        visual = CreateCell(chunkRoot);
                        _cells.Add(cellKey, visual);
                    }

                    ApplyCell(visual, cell, walkSurface);
                    ApplyProtectedVisual(visual, cellKey);
                }
            }

            RemoveMissingCells();
            RemoveMissingChunks();
            RestoreSelection(selectedCoordinates);
            ApplyTunnelCutaway();
            RefreshChunkedTerrain(world);
        }

        internal void SetTunnelCutaway(TunnelNavigationVolume volume)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            _tunnelCutaway.Clear();
            foreach (CellId cell in volume.Cells)
            {
                _tunnelCutaway.Add(cell);
            }

            ApplyTunnelCutaway();
            RefreshChunkedTerrain();
        }

        public bool TryGetCell(RaycastHit hit, out DigCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigCellVisual>();
            return cell != null;
        }

        internal bool TryGetWalkSurface(RaycastHit hit, out CellId cell)
        {
            cell = default;
            if (!TryGetCell(hit, out DigCellVisual visual))
            {
                return false;
            }

            CellId key = new CellId(
                visual.Model.X,
                visual.Model.Y,
                visual.Model.Z);
            if (!_walkSurfaceCells.Contains(key))
            {
                return false;
            }

            cell = key;
            return true;
        }

        public DigCellVisual? Select(DigCellVisual? cell)
        {
            if (_selected != null)
            {
                _selected.SetSelected(false);
            }

            _selected = cell;
            if (_selected != null)
            {
                _selected.SetSelected(true);
            }

            return _selected;
        }

        public DigCellVisual? SelectAt(int x, int y, int z)
        {
            return _cells.TryGetValue(new CellId(x, y, z), out DigCellVisual? cell)
                ? Select(cell)
                : Select(null);
        }

        private void EnsureRoot()
        {
            if (_visualRoot != null)
            {
                return;
            }

            _visualRoot = new GameObject("World Visuals").transform;
            _visualRoot.SetParent(transform, worldPositionStays: false);
        }

        private Transform GetOrCreateChunkRoot(ChunkId key, long version)
        {
            if (!_chunks.TryGetValue(key, out Transform? root))
            {
                root = new GameObject().transform;
                root.SetParent(_visualRoot, worldPositionStays: false);
                _chunks.Add(key, root);
            }

            root.name = $"Chunk {key.X},{key.Y},{key.Z} v{version}";
            return root;
        }

        private static DigCellVisual CreateCell(Transform parent)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(parent, worldPositionStays: false);
            return visual.AddComponent<DigCellVisual>();
        }

        private static void ApplyCell(
            DigCellVisual visual,
            WorldCellViewModel cell,
            bool walkSurface)
        {
            CellId cellId = new CellId(cell.X, cell.Y, cell.Z);
            visual.name = $"Cell {cell.X},{cell.Y},{cell.Z} [{cell.MaterialId}]";
            if (cell.IsSolid)
            {
                visual.transform.localPosition = DigTunnelProjection.CellWorldPosition(cellId);
                visual.transform.localScale = Vector3.one * 0.96f;
            }
            else if (walkSurface)
            {
                visual.transform.localPosition =
                    DigTunnelProjection.FloorWorldPosition(cellId);
                visual.transform.localScale = new Vector3(
                    0.94f,
                    DigTunnelProjection.FloorThickness,
                    DigTunnelProjection.FloorDepth);
            }
            else
            {
                visual.transform.localPosition = DigTunnelProjection.CellWorldPosition(cellId);
                visual.transform.localScale = Vector3.zero;
            }

            visual.Configure(cell, ResolveColor(cell));
        }

        private void ApplyTunnelCutaway()
        {
            foreach (KeyValuePair<CellId, DigCellVisual> pair in _cells)
            {
                bool renderable = pair.Value.Model.IsSolid
                    || _walkSurfaceCells.Contains(pair.Key);
                bool hiddenByCutaway = pair.Value.Model.IsSolid
                    && _tunnelCutaway.Contains(pair.Key);
                bool visible = renderable && !hiddenByCutaway;
                pair.Value.gameObject.SetActive(visible);
                if (!visible && _selected == pair.Value)
                {
                    _selected = null;
                }
            }
        }

        private void RemoveMissingCells()
        {
            _removedCells.Clear();
            foreach (KeyValuePair<CellId, DigCellVisual> pair in _cells)
            {
                if (!_visibleCells.Contains(pair.Key))
                {
                    _removedCells.Add(pair.Key);
                }
            }

            foreach (CellId key in _removedCells)
            {
                DigCellVisual visual = _cells[key];
                if (_selected == visual)
                {
                    _selected = null;
                }

                _cells.Remove(key);
                Destroy(visual.gameObject);
            }
        }

        private void RemoveMissingChunks()
        {
            _removedChunks.Clear();
            foreach (KeyValuePair<ChunkId, Transform> pair in _chunks)
            {
                if (!_visibleChunks.Contains(pair.Key))
                {
                    _removedChunks.Add(pair.Key);
                }
            }

            foreach (ChunkId key in _removedChunks)
            {
                Transform root = _chunks[key];
                _chunks.Remove(key);
                Destroy(root.gameObject);
            }
        }

        private void RestoreSelection(CellId? selectedCoordinates)
        {
            if (selectedCoordinates.HasValue)
            {
                SelectAt(selectedCoordinates.Value.X, selectedCoordinates.Value.Y, selectedCoordinates.Value.Z);
            }
        }

        private static Color ResolveColor(WorldCellViewModel cell)
        {
            if (!cell.IsExplored)
            {
                return new Color(0.08f, 0.09f, 0.12f, 1f);
            }

            if (cell.IsDesignated)
            {
                return new Color(0.95f, 0.47f, 0.12f, 1f);
            }

            if (cell.IsSolid)
            {
                float hardness = Mathf.Clamp01(cell.Hardness / 250f);
                return Color.Lerp(
                    new Color(0.48f, 0.36f, 0.25f, 1f),
                    new Color(0.32f, 0.36f, 0.42f, 1f),
                    hardness);
            }

            return new Color(0.20f, 0.52f, 0.66f, 1f);
        }
    }
}
