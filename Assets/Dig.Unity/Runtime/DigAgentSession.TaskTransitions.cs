using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    internal bool IsResidentTaskTransitionPaused(EntityId residentId, long tick)
    {
        if (residentId.IsEmpty)
        {
            return false;
        }

        AgentState? resident = _repository.Get(residentId);
        return resident != null
            && resident.IsAlive
            && _autonomy.IsTaskTransitionPaused(resident, tick);
    }

    internal Result RecordCompletedResidentTasks(
        IReadOnlyCollection<EntityId> residentIds,
        long tick)
    {
        if (residentIds == null)
        {
            throw new ArgumentNullException(nameof(residentIds));
        }

        foreach (EntityId residentId in residentIds
            .Where(id => !id.IsEmpty)
            .Distinct()
            .OrderBy(id => id.ToString(), StringComparer.Ordinal))
        {
            AgentState? resident = _repository.Get(residentId);
            if (resident == null || !resident.IsAlive)
            {
                continue;
            }

            Result recorded = RecordResidentTaskCompletion(
                resident,
                "job_completed",
                tick);
            if (recorded.IsFailure)
            {
                return recorded;
            }
        }

        return Result.Success();
    }

    private Result RecordResidentTaskCompletion(
        AgentState resident,
        string reason,
        long tick)
    {
        Result recorded = resident.RecordTaskCompletion(reason, tick);
        if (recorded.IsFailure)
        {
            return recorded;
        }

        _repository.Save(resident);
        _tunnelJournal!.Append(resident.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
