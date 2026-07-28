using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigWorldInteraction
{
    private CellId? ResolveExcavationTarget(RaycastHit hit)
    {
        if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
        && !IsTerminalJobStatus(job.Model.Status)
        && job.Model.TargetX.HasValue
        && job.Model.TargetY.HasValue
        && (!job.Model.TargetZ.HasValue || job.Model.TargetZ.Value == 0))
        {
        CellId target = new CellId(
            job.Model.TargetX.Value,
            job.Model.TargetY.Value,
            job.Model.TargetZ ?? 0);
        if (!_session!.IsExcavationOpen(target))
        {
            return target;
        }
        }

        if (_renderer!.TryGetCell(hit, out DigCellVisual cell)
        && cell.Model.IsDesignated
        && !cell.Model.IsExcavationOpen
        && cell.Model.Z == 0)
        {
        return new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
        }

        return null;
    }

    private CellId? ResolveExcavationPaintTarget(RaycastHit hit)
    {
        if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
        && !IsTerminalJobStatus(job.Model.Status)
        && job.Model.TargetX.HasValue
        && job.Model.TargetY.HasValue
        && (!job.Model.TargetZ.HasValue || job.Model.TargetZ.Value == 0))
        {
        CellId target = new CellId(
            job.Model.TargetX.Value,
            job.Model.TargetY.Value,
            job.Model.TargetZ ?? 0);
        if (!_session!.IsExcavationOpen(target))
        {
            return target;
        }
        }

        if (_renderer!.TryGetCell(hit, out DigCellVisual cell)
        && cell.Model.Z == 0
        && !cell.Model.IsExcavationOpen)
        {
        return new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
        }

        return null;
    }

}
}
