using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private Func<string, bool>? _isManualMovementActive;
        private Func<EntityId, bool>? _isTaskTransitionPaused;

        internal void BindManualMovementSource(Func<string, bool> isManualMovementActive)
        {
            _isManualMovementActive = isManualMovementActive
                ?? throw new ArgumentNullException(nameof(isManualMovementActive));
        }

        internal void BindTaskTransitionPauseSource(
            Func<EntityId, bool> isTaskTransitionPaused)
        {
            _isTaskTransitionPaused = isTaskTransitionPaused
                ?? throw new ArgumentNullException(nameof(isTaskTransitionPaused));
        }

        internal Result InterruptForManualMovement(
            IReadOnlyCollection<string> residentIds,
            long tick)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
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

        private bool IsAvailableForAutomaticWork(AgentViewModel agent)
        {
            EntityId agentId = EntityId.Parse(agent.Id);
            bool hasActiveReservation = _jobRepository.Get().GetReservations()
                .Any(reservation =>
                    reservation.Key == ReservationKey.ForAgent(agentId));
            return agent.IsAvailableForAutomaticPlanning
                && !string.Equals(agent.ActiveIntent, "Eat", StringComparison.Ordinal)
                && !string.Equals(agent.ActiveIntent, "Sleep", StringComparison.Ordinal)
                && !hasActiveReservation
                && !(_isTaskTransitionPaused?.Invoke(agentId) ?? false)
                && !(_isManualMovementActive?.Invoke(agent.Id) ?? false);
        }

        private Result ReleaseAssignmentsForAgents(
            IReadOnlyCollection<EntityId> agents,
            long tick)
        {
            if (agents.Count == 0)
            {
                return Result.Success();
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
                // Direct movement owns the resident immediately. A stale work release
                // is reconciled by the owning job system and must never block the
                // authoritative manual movement order for this or other residents.
                _releaseAssignment!.Handle(
                    new ReleaseJobAssignmentCommand(assignments[index].Id, tick));
                RemoveAllRoutePlans(assignments[index].Id);
            }

            return Result.Success();
        }

        private static HashSet<EntityId> ParseResidentIds(
            IReadOnlyCollection<string> residentIds)
        {
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

            return agents;
        }

        private void RemoveAllRoutePlans(EntityId jobId)
        {
            _routePlans.Remove(jobId);
            _haulingRoutes.Remove(jobId);
            _buildingPackingRoutes.Remove(jobId);
            _buildingBoxPickupRoutes.Remove(jobId);
            _worldItemPickupRoutes.Remove(jobId);
            _buildingBoxAssemblyRoutes.Remove(jobId);
            _buildingProductionRoutes.Remove(jobId);
            _buildingSupplyRoutes.Remove(jobId);
        }
    }
}
