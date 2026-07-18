using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly HashSet<EntityId> _directMovementAgents =
            new HashSet<EntityId>();

        internal Result InterruptForDirectMovement(
            IReadOnlyCollection<string> residentIds,
            long tick)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            RequireManualExcavationInitialized();
            HashSet<EntityId> agents = new HashSet<EntityId>();
            foreach (string residentId in residentIds)
            {
                if (string.IsNullOrWhiteSpace(residentId))
                {
                    throw new ArgumentException(
                        "Resident ids cannot contain an empty value.",
                        nameof(residentIds));
                }

                agents.Add(EntityId.Parse(residentId));
            }

            JobSnapshot[] assignments = _jobRepository.Get().GetAll()
                .Where(job => job.AssignedAgentId.HasValue
                    && agents.Contains(job.AssignedAgentId.Value)
                    && (job.Status == JobStatus.Claimed
                        || job.Status == JobStatus.InProgress))
                .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < assignments.Length; index++)
            {
                Result released = _releaseAssignment!.Handle(
                    new ReleaseJobAssignmentCommand(assignments[index].Id, tick));
                if (released.IsFailure)
                {
                    return released;
                }

                _routePlans.Remove(assignments[index].Id);
            }

            foreach (EntityId agentId in agents)
            {
                ClearManualGroupForAgent(agentId);
                _directMovementAgents.Add(agentId);
            }

            return Result.Success();
        }

        internal void ReleaseDirectMovementControl(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            _directMovementAgents.Remove(EntityId.Parse(residentId));
        }

        private bool IsDirectMovementControlled(string residentId)
        {
            return _directMovementAgents.Contains(EntityId.Parse(residentId));
        }
    }
}
