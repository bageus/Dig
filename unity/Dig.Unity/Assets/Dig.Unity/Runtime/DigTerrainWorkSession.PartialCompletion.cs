using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result CompletePartialTerrainJobAtWorkCell(
        JobSnapshot job,
        CaveRoomExcavationTarget target,
        long tick)
    {
        Result completed = _partialCompletionHandler.Handle(
            new CompletePartialTerrainWorkCommand(
                job.Id,
                target.RequiredQuarters,
                tick));
        if (completed.IsFailure)
        {
            return completed;
        }

        MarkAuthoritativeWorldChanged();
        CompleteExcavationQuarterTarget(target.Cell);
        _routePlans.Remove(job.Id);
        MarkTemplateCellExcavated(target.Cell);
        PublishTerrainCompletionEffects(
            job.Id,
            target.Cell,
            tick,
            false);
        Result refresh = RefreshNavigation();
        return refresh.IsFailure ? refresh : Result.Success();
    }

 }

}
