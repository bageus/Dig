using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal Result InterruptForCombat(
            IReadOnlyCollection<string> residentIds,
            long tick)
        {
            if (residentIds == null)
            {
                throw new System.ArgumentNullException(nameof(residentIds));
            }

            RequireManualExcavationInitialized();
            HashSet<EntityId> agents = ParseResidentIds(residentIds);
            Result released = ReleaseAssignmentsForAgents(agents, tick);
            if (released.IsFailure)
            {
                return released;
            }

            foreach (EntityId agentId in agents)
            {
                _excavationQuarterWork.Cancel(agentId);
            }

            return Result.Success();
        }
    }
}
