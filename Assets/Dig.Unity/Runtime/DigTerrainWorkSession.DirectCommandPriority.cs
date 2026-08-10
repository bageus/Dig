using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal bool HasActiveResidentDirectCommand(EntityId residentId)
    {
        if (residentId.IsEmpty)
        {
            return false;
        }

        JobSnapshot[] assigned = CollectAssignedActiveJobs(
            _jobRepository.Get(),
            residentId);
        return assigned.Length > 0;
    }

    internal IReadOnlyDictionary<EntityId, EntityId>
        CaptureActiveResidentTaskAssignments()
    {
        Dictionary<EntityId, EntityId> result =
            new Dictionary<EntityId, EntityId>();
        foreach (JobSnapshot job in _jobRepository.Get().GetAll()
            .Where(job => !job.IsTerminal && job.AssignedAgentId.HasValue)
            .OrderBy(job => job.AssignedAgentId!.Value.ToString(), StringComparer.Ordinal)
            .ThenBy(job => job.Id.ToString(), StringComparer.Ordinal))
        {
            EntityId residentId = job.AssignedAgentId!.Value;
            if (!result.ContainsKey(residentId))
            {
                result.Add(residentId, job.Id);
            }
        }

        return result;
    }

    internal IReadOnlyList<EntityId> ResolveCompletedResidentTasks(
        IReadOnlyDictionary<EntityId, EntityId> captured)
    {
        if (captured == null)
        {
            throw new ArgumentNullException(nameof(captured));
        }

        List<EntityId> completed = new List<EntityId>();
        JobSystem jobs = _jobRepository.Get();
        foreach (KeyValuePair<EntityId, EntityId> pair in captured
            .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            JobSnapshot? job = jobs.Get(pair.Value);
            if (job?.Status == JobStatus.Completed)
            {
                completed.Add(pair.Key);
            }
        }

        return completed;
    }
}

}
