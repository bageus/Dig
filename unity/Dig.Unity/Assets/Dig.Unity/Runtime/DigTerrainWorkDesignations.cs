using System;
using System.Collections.Generic;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private SyncDigDesignationJobsHandler? _designationSync;
        private AssignAvailableJobsHandler? _assignmentHandler;
        private InMemoryJobCandidateProvider? _candidateProvider;
        private DemoDigJobIdSource? _dynamicIds;

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
        }

        public void SynchronizeDesignations(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
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
                new SyncDigDesignationJobsCommand(priority: 750, tick));
            foreach (EntityId cancelledJobId in report.Cancelled)
            {
                _routePlans.Remove(cancelledJobId);
                _outputStackIds.Remove(cancelledJobId);
            }

            foreach (CreatedDigDesignationJob created in report.Created)
            {
                _outputStackIds.Add(created.JobId, _dynamicIds.NextStackId());
                _candidateProvider.SetCandidates(
                    created.JobId,
                    CreateDynamicCandidates(agents, created.CellId));
            }

            _assignmentHandler.Handle(new AssignAvailableJobsCommand(tick));
        }

        private static IReadOnlyList<JobCandidate> CreateDynamicCandidates(
            IReadOnlyList<AgentViewModel> agents,
            CellId target)
        {
            JobCandidate[] values = new JobCandidate[agents.Count];
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                int distance = Math.Abs(agent.CellX - target.X)
                    + Math.Abs(agent.CellY - target.Y);
                values[index] = new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 5_000 - (index * 250),
                    distanceCost: distance,
                    isAvailable: agent.IsAlive);
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
