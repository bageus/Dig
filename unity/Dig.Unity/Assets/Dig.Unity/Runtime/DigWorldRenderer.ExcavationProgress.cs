using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        internal void ClearExcavationQuarterProgress()
        {
            foreach (KeyValuePair<CellId, DigCellVisual> pair in _cells)
            {
                pair.Value.SetExcavationProgress(ExcavationQuarter.None);
            }
        }

        internal void SetExcavationQuarterProgress(
            CellId cell,
            ExcavationQuarter completed)
        {
            if (_cells.TryGetValue(cell, out DigCellVisual? visual))
            {
                visual.SetExcavationProgress(completed);
            }
        }
    }
}
