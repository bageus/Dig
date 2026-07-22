using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private static readonly JobCandidate[] NoCandidates = Array.Empty<JobCandidate>();
        private SyncDigDesignationJobsHandler? _designationSync;
        private AssignAvailableJobsHandler? _assignmentHandler;
        private InMemoryJobCandidateProvider? _candidateProvider;
        private DemoDigJobIdSource? _dynamicIds;
        private EraseExcavationBatchHandler? _eraseExcavationBatch;

        internal void InitializeDynamicDesignations(InMemoryExecutionJournal journal)
        {
            if (journal == null)
            {
                throw new ArgumentNullException(nameof(journal));
            }

            _candidateProvider = new InMemoryJobCandidateProvider();
            _dynamicIds = new DemoDigJobIdSource();
            _designationSync = new SyncDigDesignationJobsHandler(
                _worldSession.Repository,
                _jobRepository,
                _dynamicIds,
                journal);
            _assignmentHandler = new AssignAvailableJobsHandler(
                _jobRepository,
                _candidateProvider,
                journal);
            _eraseExcavationBatch = new EraseExcavationBatchHandler(
                _worldSession.Repository,
                _jobRepository,
                journal);
            InitializeManualExcavation(journal);
        }

        internal Result<EraseExcavationBatchReport> EraseExcavationBatch(
            IReadOnlyList<CellId> cells,
            long tick)
        {
            if (_eraseExcavationBatch == null)
            {
                throw new InvalidOperationException(
                    "Dynamic designation synchronization is not initialized.");
            }

            Result<EraseExcavationBatchReport> result = _eraseExcavationBatch.Handle(
                new EraseExcavationBatchCommand(cells, tick));
            if (result.IsFailure)
            {
                return result;
            }

            for (int index = 0; index < result.Value.CancelledJobIds.Count; index++)
            {
                EntityId jobId = result.Value.CancelledJobIds[index];
                _routePlans.Remove(jobId);
                _outputStackIds.Remove(jobId);
                RemoveManualExcavationJob(jobId);
            }

            return result;
        }

        public void SynchronizeDesignations(
            long tick,
            IReadOnlyList<AgentViewModel> agents,
            int priority = 750)
        {
            if (_designationSync == null
                || _assignmentHandler == null
                || _candidateProvider == null
                || _dynamicIds == null)
            {
                throw new InvalidOperationException(
                    "Dynamic designation synchronization is not initialized.");
            }

            DigDesignationJobSyncReport report = _designationSync.Handle(
                new SyncDigDesignationJobsCommand(priority, tick));
            foreach (EntityId cancelledJobId in report.Cancelled)
            {
                _routePlans.Remove(cancelledJobId);
                _outputStackIds.Remove(cancelledJobId);
                RemoveManualExcavationJob(cancelledJobId);
            }

            foreach (CreatedDigDesignationJob created in report.Created)
            {
                _outputStackIds.Add(created.JobId, _dynamicIds.NextStackId());
            }

            Dictionary<CellId, CellSnapshot> cells = _worldSession.LoadSnapshot().Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll()
                .Where(value => value.Definition is DigJobDefinition && !value.IsTerminal))
            {
                DigJobDefinition definition = (DigJobDefinition)job.Definition;
                if (IsManualExcavationJob(job.Id)
                    || !IsExcavationFrontier(definition.Target.CellId, cells))
                {
                    _candidateProvider.SetCandidates(job.Id, NoCandidates);
                    continue;
                }

                _candidateProvider.SetCandidates(
                    job.Id,
                    CreateDynamicCandidates(agents, definition.Target.CellId));
            }

            _assignmentHandler.Handle(new AssignAvailableJobsCommand(tick));
            Result pending = RetryPendingManualExcavations(tick);
            if (pending.IsFailure)
            {
                throw new InvalidOperationException(pending.Error!.ToString());
            }
        }

        private static bool IsExcavationFrontier(
            CellId target,
            IReadOnlyDictionary<CellId, CellSnapshot> cells)
        {
            if (!cells.TryGetValue(target, out CellSnapshot cell)
                || !cell.IsSolid
                || cell.State.Designation != CellDesignation.Dig)
            {
                return false;
            }

            CellId[] neighbors =
            {
                target.X > 0 ? new CellId(target.X - 1, target.Y) : target,
                new CellId(target.X + 1, target.Y),
                target.Y > 0 ? new CellId(target.X, target.Y - 1) : target,
                new CellId(target.X, target.Y + 1),
            };
            for (int index = 0; index < neighbors.Length; index++)
            {
                if (neighbors[index] != target
                    && cells.TryGetValue(neighbors[index], out CellSnapshot neighbor)
                    && !neighbor.IsSolid)
                {
                    return true;
                }
            }

            return false;
        }

        private IReadOnlyList<JobCandidate> CreateDynamicCandidates(
            IReadOnlyList<AgentViewModel> agents,
            CellId target)
        {
            JobCandidate[] values = new JobCandidate[agents.Count];
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                int distance = Math.Abs(agent.CellX - target.X)
                    + Math.Abs(agent.CellY - target.Y)
                    + Math.Abs(agent.CellZ);
                values[index] = new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 5_000 - (index * 250),
                    distanceCost: distance,
                    isAvailable: agent.CellZ == 0
                        && IsAvailableForAutomaticWork(agent));
            }

            return values;
        }

        private sealed class DemoDigJobIdSource : IDigJobIdSource
        {
            private ulong _jobSequence = 1;
            private ulong _stackSequence = 1;

            public EntityId Next()
            {
                return EntityId.Parse("7" + (_jobSequence++).ToString("x31"));
            }

            public EntityId NextStackId()
            {
                return EntityId.Parse("8" + (_stackSequence++).ToString("x31"));
            }
        }
    }
}
