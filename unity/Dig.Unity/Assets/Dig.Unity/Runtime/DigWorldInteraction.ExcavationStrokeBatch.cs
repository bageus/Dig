using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private bool _excavationDesignationBatchDirty;

        private Result StageExcavationCell(CellId target, bool active)
        {
            Result result = _simulation!.StageExcavationDesignation(target, active);
            if (result.IsSuccess)
            {
                _excavationDesignationBatchDirty = true;
            }

            return result;
        }

        private void CommitPendingExcavationStroke()
        {
            if (!_excavationDesignationBatchDirty || _simulation == null)
            {
                return;
            }

            Result result = _simulation.CommitExcavationDesignationBatch(
                _excavationPriority);
            _excavationDesignationBatchDirty = false;
            _hud?.SetCommandResult(result);
        }

        private void ResetExcavationStroke()
        {
            _excavationAxis = ExcavationStrokeAxis.None;
            _excavationAnchor = null;
            _lastExcavationPaintCell = null;
            _excavationEraseBatch.Clear();
            _excavationDesignationBatchDirty = false;
        }
    }
}
