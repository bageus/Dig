using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private static CellId ResolveBuildingBoxAssemblyTarget(
            JobSnapshot job,
            BuildingBoxAssemblyJobDefinition assembly,
            ItemStackSnapshot? box)
        {
            bool acquiring = job.Status == JobStatus.Claimed
                || job.Stage == JobStageKind.AcquireItem;
            if (acquiring
                && box?.Location.Kind == ItemLocationKind.World
                && box.Location.HasCell)
            {
                return box.Location.CellId;
            }

            return assembly.WorkPosition;
        }

        private static IReadOnlyList<JobCandidate> CreateBuildingBoxAssemblyCandidates(
            IReadOnlyList<AgentViewModel> agents,
            ItemStackSnapshot box)
        {
            if (box.Location.Kind == ItemLocationKind.AgentInventory && box.Location.HasOwner)
            {
                return agents
                    .Where(agent => string.Equals(
                        agent.Id,
                        box.Location.OwnerId.ToString(),
                        StringComparison.Ordinal))
                    .Select(agent => new JobCandidate(
                        EntityId.Parse(agent.Id),
                        skillLevel: 5_000,
                        distanceCost: 0,
                        isAvailable: agent.IsAvailableForAutomaticPlanning))
                    .ToArray();
            }

            if (box.Location.Kind != ItemLocationKind.World || !box.Location.HasCell)
            {
                return Array.Empty<JobCandidate>();
            }

            CellId source = box.Location.CellId;
            return agents.Select((agent, index) => new JobCandidate(
                EntityId.Parse(agent.Id),
                skillLevel: 4_800 - (index * 150),
                distanceCost: Math.Abs(agent.CellX - source.X) + Math.Abs(agent.CellY - source.Y),
                isAvailable: agent.IsAvailableForAutomaticPlanning)).ToArray();
        }
    }
}
