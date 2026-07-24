using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly Dictionary<EntityId, ManualExcavationGroup> _manualGroups =
            new Dictionary<EntityId, ManualExcavationGroup>();
        private readonly Dictionary<EntityId, EntityId> _manualGroupByJob =
            new Dictionary<EntityId, EntityId>();

        private bool IsManualExcavationJob(EntityId jobId)
        {
            return _manualGroupByJob.ContainsKey(jobId);
        }

        private void RemoveManualExcavationJob(EntityId jobId)
        {
            if (!_manualGroupByJob.TryGetValue(jobId, out EntityId groupId))
            {
                return;
            }

            _manualGroupByJob.Remove(jobId);
            if (_manualGroups.TryGetValue(groupId, out ManualExcavationGroup? group))
            {
                group.Remove(jobId);
                if (group.JobIds.Count == 0 && !HasPendingManualTargets(group))
                {
                    _manualGroups.Remove(groupId);
                }
            }
        }

        private void ClearManualGroupForAgent(EntityId agentId)
        {
            ManualExcavationGroup? group = _manualGroups.Values
                .FirstOrDefault(value => value.AgentId == agentId);
            if (group == null)
            {
                return;
            }

            EntityId[] jobIds = group.JobIds.ToArray();
            for (int index = 0; index < jobIds.Length; index++)
            {
                _manualGroupByJob.Remove(jobIds[index]);
            }

            _manualGroups.Remove(group.Id);
        }

        private void RemoveJobFromExistingManualGroup(EntityId jobId)
        {
            if (_manualGroupByJob.TryGetValue(jobId, out EntityId groupId)
                && _manualGroups.TryGetValue(groupId, out ManualExcavationGroup? group))
            {
                group.Remove(jobId);
                if (group.JobIds.Count == 0)
                {
                    _manualGroups.Remove(groupId);
                }
            }
        }

        private sealed class ManualExcavationGroup
        {
            private readonly List<EntityId> _jobIds;
            private readonly List<CellId> _targetCells;

            internal ManualExcavationGroup(
                EntityId id,
                EntityId agentId,
                IEnumerable<EntityId> jobIds,
                IEnumerable<CellId> targetCells)
            {
                Id = id;
                AgentId = agentId;
                _jobIds = jobIds.Distinct().ToList();
                _targetCells = targetCells.Distinct().ToList();
            }

            internal EntityId Id { get; }
            internal EntityId AgentId { get; }
            internal IReadOnlyList<EntityId> JobIds => _jobIds;
            internal IReadOnlyList<CellId> TargetCells => _targetCells;

            internal void Add(EntityId jobId)
            {
                if (!_jobIds.Contains(jobId))
                {
                    _jobIds.Add(jobId);
                }
            }

            internal void Remove(EntityId jobId)
            {
                _jobIds.Remove(jobId);
            }
        }
    }
}
