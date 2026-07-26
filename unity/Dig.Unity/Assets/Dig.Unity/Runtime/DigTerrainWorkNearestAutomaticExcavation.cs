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
            AssignNearestAutomaticExcavationJobs(agents, cells, tick);
            SuppressAvailableExcavationCandidates();
        }

        private void AssignNearestAutomaticSpatialJobs(
            IReadOnlyList<AgentViewModel> agents,
            long tick)
        {
            AssignNearestAutomaticExcavationJobs(
                agents,
                CollectWorldCells(),
                tick);
            SuppressAvailableExcavationCandidates();
        }

        private void AssignNearestAutomaticExcavationJobs(
            IReadOnlyList<AgentViewModel> agents,
            IReadOnlyDictionary<CellId, CellSnapshot> cells,
            long tick)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            RequireManualExcavationInitialized();
            List<JobSnapshot> digJobs = _jobRepository.Get().GetAll()
                .Where(job => job.Status == JobStatus.Available
                    && job.Definition is DigJobDefinition definition
                    && IsExcavationFrontier(definition.Target.CellId, cells))
                .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
                .ToList();
            List<JobSnapshot> spatialJobs = LoadActiveSpatialJobs()
                .Where(job => job.Status == JobStatus.Available)
                .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
                .ToList();
            if (digJobs.Count == 0 && spatialJobs.Count == 0)
            {
                return;
            }

            Result<NavigationSnapshot> navigation = LoadDirectAssignmentNavigation();
            if (navigation.IsFailure)
            {
                return;
            }

            HashSet<EntityId> alreadyAssigned = new HashSet<EntityId>(
                _jobRepository.Get().GetAll()
                    .Where(job => !job.IsTerminal && job.AssignedAgentId.HasValue)
                    .Select(job => job.AssignedAgentId.GetValueOrDefault()));
            AgentViewModel[] availableAgents = agents
                .Where(agent => !string.IsNullOrWhiteSpace(agent.Id))
                .Where(IsAvailableForAutomaticWork)
                .Where(agent => !alreadyAssigned.Contains(EntityId.Parse(agent.Id!)))
                .OrderBy(agent => agent.Id, StringComparer.Ordinal)
                .ToArray();

            for (int index = 0; index < availableAgents.Length; index++)
            {
                AgentViewModel agent = availableAgents[index];
                DirectJobWorker worker = new DirectJobWorker(
                    EntityId.Parse(agent.Id!),
                    new CellId(agent.CellX, agent.CellY, agent.CellZ));
                DirectJobAssignment? dig = PlanNearestDig(
                    worker,
                    digJobs,
                    navigation.Value);
                bool canTakeSpatial = string.Equals(
                    agent.ScheduledActivity,
                    ScheduleActivity.Work.ToString(),
                    StringComparison.Ordinal);
                DirectJobAssignment? spatial = canTakeSpatial
                    ? PlanNearestSpatial(worker, spatialJobs, navigation.Value)
                    : null;
                DirectJobAssignment? selected = SelectNearest(dig, spatial);
                if (selected == null)
                {
                    continue;
                }

                Result assigned = _specificAssignment!.Handle(
                    new AssignSpecificJobCommand(
                        selected.JobId,
                        selected.AgentId,
                        tick));
                digJobs.RemoveAll(job => job.Id == selected.JobId);
                spatialJobs.RemoveAll(job => job.Id == selected.JobId);
                if (assigned.IsFailure)
                {
                    continue;
                }
            }
        }

        private DirectJobAssignment? PlanNearestDig(
            DirectJobWorker worker,
            IReadOnlyCollection<JobSnapshot> jobs,
            NavigationSnapshot navigation)
        {
            if (jobs.Count == 0)
            {
                return null;
            }

            Result<DirectJobAssignmentPlan> planned =
                _directAssignmentPlanner!.Plan(
                    new[] { worker },
                    jobs,
                    navigation);
            return planned.IsSuccess && planned.Value.Assignments.Count > 0
                ? planned.Value.Assignments[0]
                : null;
        }

        private DirectJobAssignment? PlanNearestSpatial(
            DirectJobWorker worker,
            IReadOnlyCollection<JobSnapshot> jobs,
            NavigationSnapshot navigation)
        {
            if (jobs.Count == 0)
            {
                return null;
            }

            Result<DirectJobAssignmentPlan> planned =
                _directSpatialAssignmentPlanner!.Plan(
                    new[] { worker },
                    jobs,
                    navigation);
            return planned.IsSuccess && planned.Value.Assignments.Count > 0
                ? planned.Value.Assignments[0]
                : null;
        }

        private static DirectJobAssignment? SelectNearest(
            DirectJobAssignment? dig,
            DirectJobAssignment? spatial)
        {
            return new[] { dig, spatial }
                .Where(value => value != null)
                .OrderBy(value => value!.TargetDistance)
                .ThenBy(value => value!.RouteCost)
                .ThenBy(value => value!.Target)
                .ThenBy(
                    value => value!.JobId.ToString(),
                    StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private void SuppressAvailableExcavationCandidates()
        {
            foreach (JobSnapshot job in _jobRepository.Get().GetAll()
                .Where(value => value.Status == JobStatus.Available)
                .Where(value => value.Definition is DigJobDefinition
                    || value.Definition is SpatialDigJobDefinition))
            {
                _candidateProvider!.SetCandidates(job.Id, NoCandidates);
            }
        }
    }
}
