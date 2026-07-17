using System;
using System.Collections.Generic;
using System.Linq;
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
                .ToDictionary(
                    job => ((DigJobDefinition)job.Definition).Target.CellId,
                    job => job);
        }

        private static int Preferred(JobSnapshot job, CellId? preferredCell)
        {
            if (!preferredCell.HasValue || job.Definition is not DigJobDefinition dig)
            {
                return 0;
            }

            return dig.Target.CellId == preferredCell.Value ? 1 : 0;
        }

        private static int Distance(CellId cell, CellId? preferredCell)
        {
            return preferredCell.HasValue
                ? Math.Abs(cell.X - preferredCell.Value.X)
                    + Math.Abs(cell.Y - preferredCell.Value.Y)
                : 0;
        }
    }
}
