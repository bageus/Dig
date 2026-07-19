using System;
using System.Collections.Generic;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private readonly HashSet<Vector2Int> _protectedCells =
            new HashSet<Vector2Int>();
        private DigCellVisual? _rejectedCell;

        internal void SetProtectedCells(IReadOnlyList<CellId> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            _protectedCells.Clear();
            for (int index = 0; index < cells.Count; index++)
            {
                _protectedCells.Add(new Vector2Int(cells[index].X, cells[index].Y));
            }

            RefreshChunkedTerrain();
        }

        internal void HighlightRejected(CellId cell)
        {
            if (_rejectedCell != null)
            {
                _rejectedCell.SetRejected(false);
            }

            Vector2Int key = new Vector2Int(cell.X, cell.Y);
            if (!_cells.TryGetValue(key, out DigCellVisual? visual))
            {
                _rejectedCell = null;
                return;
            }

            _rejectedCell = visual;
            _rejectedCell.SetRejected(true);
        }

        private void ApplyProtectedVisual(DigCellVisual visual, Vector2Int key)
        {
            if (!visual.Model.IsSolid || !_protectedCells.Contains(key))
            {
                return;
            }

            visual.Configure(
                visual.Model,
                new Color(0.18f, 0.22f, 0.28f, 1f));
        }
    }
}
