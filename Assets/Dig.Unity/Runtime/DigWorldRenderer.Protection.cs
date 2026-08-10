using System;
using System.Collections.Generic;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private readonly HashSet<CellId> _protectedCells =
            new HashSet<CellId>();
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
                _protectedCells.Add(cells[index]);
            }

            RefreshChunkedTerrain();
        }

        internal void HighlightRejected(CellId cell)
        {
            if (_rejectedCell != null)
            {
                _rejectedCell.SetRejected(false);
            }

            if (!_cells.TryGetValue(cell, out DigCellVisual? visual))
            {
                _rejectedCell = null;
                ApplyCellProxyState();
                return;
            }

            _rejectedCell = visual;
            _rejectedCell.SetRejected(true);
            ApplyCellProxyState();
        }

        private void ApplyProtectedVisual(DigCellVisual visual, CellId key)
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
