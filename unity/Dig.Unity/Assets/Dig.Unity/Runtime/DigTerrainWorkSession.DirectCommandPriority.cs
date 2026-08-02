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
}

}
