using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private IReadOnlyCollection<CellId> CollectDesignatedCells()
        {
            return _worldSession.LoadSnapshot().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => cell.IsSolid
                    && cell.State.Designation == CellDesignation.Dig)
                .Select(cell => cell.Id)
                .ToArray();
        }

        private IReadOnlyCollection<IReadOnlyCollection<CellId>> CollectTemplateRoomGroups(
            IReadOnlyCollection<CellId> designatedCells)
        {
            HashSet<CellId> designated = new HashSet<CellId>(designatedCells);
            return _templateInstances.Values
                .Where(instance => instance.LifecycleState
                    == ExcavationTemplateLifecycleState.Active)
                .OrderBy(instance => instance.Id, System.StringComparer.Ordinal)
                .Select(instance => (IReadOnlyCollection<CellId>)instance.OrderedMask
                    .Where(designated.Contains)
                    .OrderBy(cell => cell)
                    .ToArray())
                .Where(group => group.Count > 0)
                .ToArray();
        }


        private Dictionary<CellId, CellSnapshot> CollectWorldCells()
        {
            return _worldSession.LoadSnapshot().Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
        }

        private Dictionary<CellId, JobSnapshot> CollectActiveDigJobs()
        {
            return _jobRepository.Get().GetAll()
                .Where(job => !job.IsTerminal && job.Definition is DigJobDefinition)
                .GroupBy(job => ((DigJobDefinition)job.Definition).Target.CellId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(job => job.Id.ToString(), System.StringComparer.Ordinal)
                        .First());
        }
    }
}
