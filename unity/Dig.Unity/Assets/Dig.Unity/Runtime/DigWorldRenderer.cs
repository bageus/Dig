using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldRenderer : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, DigCellVisual> _cells =
            new Dictionary<Vector2Int, DigCellVisual>();
        private readonly Dictionary<Vector2Int, Transform> _chunks =
            new Dictionary<Vector2Int, Transform>();
        private readonly HashSet<Vector2Int> _visibleCells =
            new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _visibleChunks =
            new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _removedCells =
            new List<Vector2Int>();
        private readonly List<Vector2Int> _removedChunks =
            new List<Vector2Int>();
        private Transform? _visualRoot;
        private DigCellVisual? _selected;

        public void Render(WorldViewModel world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            Vector2Int? selectedCoordinates = _selected == null
                ? (Vector2Int?)null
                : new Vector2Int(_selected.Model.X, _selected.Model.Y);
            EnsureRoot();
            _visibleCells.Clear();
            _visibleChunks.Clear();

            foreach (WorldChunkViewModel chunk in world.Chunks)
            {
                Vector2Int chunkKey = new Vector2Int(chunk.X, chunk.Y);
                _visibleChunks.Add(chunkKey);
                Transform chunkRoot = GetOrCreateChunkRoot(chunkKey, chunk.Version);
                foreach (WorldCellViewModel cell in chunk.Cells)
                {
                    Vector2Int cellKey = new Vector2Int(cell.X, cell.Y);
                    _visibleCells.Add(cellKey);
                    if (!_cells.TryGetValue(cellKey, out DigCellVisual? visual))
                    {
                        visual = CreateCell(chunkRoot);
                        _cells.Add(cellKey, visual);
                    }

                    ApplyCell(visual, cell);
                }
            }

            RemoveMissingCells();
            RemoveMissingChunks();
            RestoreSelection(selectedCoordinates);
        }

        public bool TryGetCell(RaycastHit hit, out DigCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigCellVisual>();
            return cell != null;
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

        public DigCellVisual? SelectAt(int x, int y)
        {
            return _cells.TryGetValue(new Vector2Int(x, y), out DigCellVisual? cell)
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

        private Transform GetOrCreateChunkRoot(Vector2Int key, long version)
        {
            if (!_chunks.TryGetValue(key, out Transform? root))
            {
                root = new GameObject().transform;
                root.SetParent(_visualRoot, worldPositionStays: false);
                _chunks.Add(key, root);
            }

            root.name = $"Chunk {key.x},{key.y} v{version}";
            return root;
        }

        private static DigCellVisual CreateCell(Transform parent)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(parent, worldPositionStays: false);
            return visual.AddComponent<DigCellVisual>();
        }

        private static void ApplyCell(DigCellVisual visual, WorldCellViewModel cell)
        {
            visual.name = $"Cell {cell.X},{cell.Y} [{cell.MaterialId}]";
            float height = cell.IsSolid ? 0.82f : 0.08f;
            visual.transform.localPosition = new Vector3(cell.X, height * 0.5f, cell.Y);
            visual.transform.localScale = new Vector3(0.94f, height, 0.94f);
            visual.Configure(cell, ResolveColor(cell));
        }

        private void RemoveMissingCells()
        {
            _removedCells.Clear();
            foreach (KeyValuePair<Vector2Int, DigCellVisual> pair in _cells)
            {
                if (!_visibleCells.Contains(pair.Key))
                {
                    _removedCells.Add(pair.Key);
                }
            }

            foreach (Vector2Int key in _removedCells)
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
            foreach (KeyValuePair<Vector2Int, Transform> pair in _chunks)
            {
                if (!_visibleChunks.Contains(pair.Key))
                {
                    _removedChunks.Add(pair.Key);
                }
            }

            foreach (Vector2Int key in _removedChunks)
            {
                Transform root = _chunks[key];
                _chunks.Remove(key);
                Destroy(root.gameObject);
            }
        }

        private void RestoreSelection(Vector2Int? selectedCoordinates)
        {
            if (selectedCoordinates.HasValue)
            {
                SelectAt(selectedCoordinates.Value.x, selectedCoordinates.Value.y);
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

            return new Color(0.35f, 0.40f, 0.30f, 1f);
        }
    }
}
