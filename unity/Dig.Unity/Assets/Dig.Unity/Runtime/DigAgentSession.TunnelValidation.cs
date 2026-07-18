using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigAgentSession
    {
        internal Result ValidateResidentThroughTunnel(
            string residentId,
            SpatialCellId destination)
        {
            return ValidateResidentsThroughTunnel(new[] { residentId }, destination);
        }

        internal Result ValidateResidentsThroughTunnel(
            IReadOnlyCollection<string> residentIds,
            SpatialCellId destination)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            if (residentIds.Count == 0)
            {
                return Result.Failure(new DomainError(
                    "unity.tunnel.no_residents",
                    "At least one resident is required."));
            }

            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string residentId in residentIds)
            {
                if (string.IsNullOrWhiteSpace(residentId) || !unique.Add(residentId))
                {
                    return Result.Failure(new DomainError(
                        "unity.tunnel.invalid_residents",
                        "Resident ids must be non-empty and unique."));
                }

                EntityId id = EntityId.Parse(residentId);
                AgentState? agent = _repository.Get(id);
                if (agent == null)
                {
                    return Result.Failure(new DomainError(
                        "unity.tunnel.resident_not_found",
                        $"Resident '{residentId}' was not found."));
                }

                if (!agent.IsAlive)
                {
                    return Result.Failure(AgentErrors.AgentDead);
                }

                var path = TunnelVolume.FindPath(agent.SpatialPosition, destination);
                if (!path.Succeeded)
                {
                    return Result.Failure(new DomainError(
                        $"agents.tunnel.{path.FailureReason.ToString().ToLowerInvariant()}",
                        path.Detail));
                }
            }

            return Result.Success();
        }
    }
}
