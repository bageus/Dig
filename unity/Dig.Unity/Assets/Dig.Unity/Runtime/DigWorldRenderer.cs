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
        private Transform? _visualRoot;
        private DigCellVisual? _selected;

        public void Render(WorldViewModel world)
        {
            Vector2Int? selectedCoordinates = _selected == null
                ? (Vector2Int?)null
                : new Vector2Int(_selected.Model.X, _selected.Model.Y);
            ClearVisuals();
            _visualRoot = new GameObject("World Visuals").transform;
            _visualRoot.SetParent(transform, worldPositionStays: false);

            foreach (WorldChunkViewModel chunk in world.Chunks)
            {
                Transform chunkRoot = new GameObject(
                    $"Chunk {chunk.X},{chunk.Y} v{chunk.Version}").transform;
                chunkRoot.SetParent(_visualRoot, worldPositionStays: false);
                foreach (WorldCellViewModel cell in chunk.Cells)
                {
                    CreateCell(chunkRoot, cell);
                }
            }

            if (selectedCoordinates.HasValue)
            {
                SelectAt(selectedCoordinates.Value.x, selectedCoordinates.Value.y);
            }
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

        private void CreateCell(Transform parent, WorldCellViewModel cell)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = $"Cell {cell.X},{cell.Y} [{cell.MaterialId}]";
            visual.transform.SetParent(parent, worldPositionStays: false);
            float height = cell.IsSolid ? 0.82f : 0.08f;
            visual.transform.localPosition = new Vector3(cell.X, height * 0.5f, cell.Y);
            visual.transform.localScale = new Vector3(0.94f, height, 0.94f);
            DigCellVisual cellVisual = visual.AddComponent<DigCellVisual>();
            cellVisual.Configure(cell, ResolveColor(cell));
            _cells.Add(new Vector2Int(cell.X, cell.Y), cellVisual);
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

        private void ClearVisuals()
        {
            _cells.Clear();
            _selected = null;
            if (_visualRoot != null)
            {
                Destroy(_visualRoot.gameObject);
                _visualRoot = null;
            }
        }
    }
}
