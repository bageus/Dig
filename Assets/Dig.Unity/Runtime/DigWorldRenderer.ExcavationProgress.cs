using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private readonly Dictionary<CellId, ExcavationQuarter>
            _excavationQuarterProgress =
                new Dictionary<CellId, ExcavationQuarter>();


        private void SynchronizeWorldExcavationProgress(WorldViewModel world)
        {
            _excavationQuarterProgress.Clear();
            for (int chunkIndex = 0; chunkIndex < world.Chunks.Count; chunkIndex++)
            {
                WorldChunkViewModel chunk = world.Chunks[chunkIndex];
                for (int cellIndex = 0; cellIndex < chunk.Cells.Count; cellIndex++)
                {
                    WorldCellViewModel cell = chunk.Cells[cellIndex];
                    if (cell.CompletedExcavationQuarters == ExcavationQuarter.None)
                    {
                        continue;
                    }

                    _excavationQuarterProgress[new CellId(cell.X, cell.Y, cell.Z)] =
                        cell.CompletedExcavationQuarters;
                }
            }
        }

        internal void SynchronizeExcavationQuarterProgress(
            IReadOnlyList<ExcavationQuarterProgressSnapshot> progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            Dictionary<CellId, ExcavationQuarter> next =
                new Dictionary<CellId, ExcavationQuarter>();
            for (int index = 0; index < progress.Count; index++)
            {
                ExcavationQuarterProgressSnapshot snapshot = progress[index];
                if (snapshot.Completed != ExcavationQuarter.None)
                {
                    next[snapshot.Target.CellId] = snapshot.Completed;
                }
            }

            bool chunkMembershipChanged = false;
            foreach (KeyValuePair<CellId, ExcavationQuarter> previous
                in _excavationQuarterProgress)
            {
                if (!next.ContainsKey(previous.Key))
                {
                    chunkMembershipChanged = true;
                    if (_cells.TryGetValue(previous.Key, out DigCellVisual? visual))
                    {
                        visual.SetExcavationProgress(ExcavationQuarter.None);
                    }
                }
            }

            foreach (KeyValuePair<CellId, ExcavationQuarter> current in next)
            {
                if (!_excavationQuarterProgress.ContainsKey(current.Key))
                {
                    chunkMembershipChanged = true;
                }

                if (_cells.TryGetValue(current.Key, out DigCellVisual? visual))
                {
                    visual.SetExcavationProgress(current.Value);
                }
            }

            _excavationQuarterProgress.Clear();
            foreach (KeyValuePair<CellId, ExcavationQuarter> current in next)
            {
                _excavationQuarterProgress.Add(current.Key, current.Value);
            }

            if (chunkMembershipChanged)
            {
                RefreshChunkedTerrain();
            }
        }

        internal void ClearExcavationQuarterProgress()
        {
            SynchronizeExcavationQuarterProgress(
                Array.Empty<ExcavationQuarterProgressSnapshot>());
        }

        internal void SetExcavationQuarterProgress(
            CellId cell,
            ExcavationQuarter completed)
        {
            if (completed == ExcavationQuarter.None)
            {
                if (_excavationQuarterProgress.Remove(cell)
                    && _cells.TryGetValue(cell, out DigCellVisual? cleared))
                {
                    cleared.SetExcavationProgress(ExcavationQuarter.None);
                    RefreshChunkedTerrain();
                }

                return;
            }

            bool added = !_excavationQuarterProgress.ContainsKey(cell);
            _excavationQuarterProgress[cell] = completed;
            if (_cells.TryGetValue(cell, out DigCellVisual? visual))
            {
                visual.SetExcavationProgress(completed);
            }

            if (added)
            {
                RefreshChunkedTerrain();
            }
        }
    }
}
