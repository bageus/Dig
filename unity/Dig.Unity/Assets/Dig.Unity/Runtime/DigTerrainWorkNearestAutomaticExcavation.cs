using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private void AssignNearestAutomaticDigJobs(
            IReadOnlyList<AgentViewModel> agents,
            IReadOnlyDictionary<CellId, CellSnapshot> cells,
            long tick)
        {
            RequireManualExcavationInitialized();
            JobSnapshot[] available = _jobRepository.Get().GetAll()
                .Where(job => job.Status == JobStatus.Available
                    && job.Definition is DigJobDefinition definition
                    && IsExcavationFrontier(definition.Target.CellId, cells))
                .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            AssignNearestAutomaticJobs(
                agents,
                available,
                spatial: false,
                requireWorkSchedule: false,
                tick);
            SuppressAvailableExcavationCandidates<DigJobDefinition>();
        }

        private void AssignNearestAutomaticSpatialJobs(
            IReadOnlyList<AgentViewModel> agents,
            long tick)
        {
            RequireManualExcavationInitialized();
            JobSnapshot[] available = LoadActiveSpatialJobs()
                .Where(job => job.Status == JobStatus.Available)
                .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            AssignNearestAutomaticJobs(
                agents,
                available,
                spatial: true,
                requireWorkSchedule: true,
                tick);
            SuppressAvailableExcavationCandidates<SpatialDigJobDefinition>();
        }

        private void AssignNearestAutomaticJobs(
            IReadOnlyList<AgentViewModel> agents,
            IReadOnlyCollection<JobSnapshot> jobs,
            bool spatial,
            bool requireWorkSchedule,
            long tick)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            if (jobs.Count == 0)
            {
                return;
            }

            HashSet<EntityId> alreadyAssigned = _jobRepository.Get().GetAll()
                .Where(job => !job.IsTerminal && job.AssignedAgentId.HasValue)
                .Select(job => job.AssignedAgentId!.Value)
                .ToHashSet();
            DirectJobWorker[] workers = agents
                .Where(agent => !string.IsNullOrWhiteSpace(agent.Id))
                .Where(IsAvailableForAutomaticWork)
                .Where(agent => !requireWorkSchedule
                    || string.Equals(
                        agent.ScheduledActivity,
                        ScheduleActivity.Work.ToString(),
                        StringComparison.Ordinal))
                .Where(agent => !alreadyAssigned.Contains(EntityId.Parse(agent.Id!)))
                .Select(agent => new DirectJobWorker(
                    EntityId.Parse(agent.Id!),
                    new CellId(agent.CellX, agent.CellY, agent.CellZ)))
                .OrderBy(worker => worker.AgentId.ToString(), StringComparer.Ordinal)
                .ToArray();
            if (workers.Length == 0)
            {
                return;
            }

            Result<NavigationSnapshot> navigation = LoadDirectAssignmentNavigation();
            if (navigation.IsFailure)
            {
                return;
            }

            Result<DirectJobAssignmentPlan> planned = spatial
                ? _directSpatialAssignmentPlanner!.Plan(workers, jobs, navigation.Value)
                : _directAssignmentPlanner!.Plan(workers, jobs, navigation.Value);
            if (planned.IsFailure)
            {
                return;
            }

            for (int index = 0; index < planned.Value.Assignments.Count; index++)
            {
                DirectJobAssignment assignment = planned.Value.Assignments[index];
                JobSnapshot? current = _jobRepository.Get().Get(assignment.JobId);
                if (current == null || current.Status != JobStatus.Available)
                {
                    continue;
                }

                _specificAssignment!.Handle(new AssignSpecificJobCommand(
                    assignment.JobId,
                    assignment.AgentId,
                    tick));
            }
        }

        private void SuppressAvailableExcavationCandidates<TDefinition>()
            where TDefinition : JobDefinition
        {
            foreach (JobSnapshot job in _jobRepository.Get().GetAll()
                .Where(value => value.Status == JobStatus.Available
                    && value.Definition is TDefinition))
            {
                _candidateProvider!.SetCandidates(job.Id, NoCandidates);
            }
        }
    }
}
