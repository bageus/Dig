using System.Collections.Generic;
using Dig.Domain.Jobs;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal IReadOnlyList<JobSnapshot> LoadJobSnapshots()
    {
        return _jobRepository.Get().GetAll();
    }
}

}