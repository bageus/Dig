using System;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private static readonly DomainError NoExcavationFront = new DomainError(
            "unity.excavation.no_reachable_front",
            "The selected excavation zone has no reachable front cell.");
        private AssignSpecificJobHandler? _specificAssignment;
        private ReleaseJobAssignmentHandler? _releaseAssignment;
        private ExcavationClusterPlanner? _clusterPlanner;
        private DirectJobAssignmentPlanner? _directAssignmentPlanner;
        private DirectSpatialJobAssignmentPlanner? _directSpatialAssignmentPlanner;

        internal void InitializeManualExcavation(InMemoryExecutionJournal journal)
        {
            _specificAssignment = new AssignSpecificJobHandler(_jobRepository, journal);
            _releaseAssignment = new ReleaseJobAssignmentHandler(_jobRepository, journal);
            _clusterPlanner = new ExcavationClusterPlanner();
            _directAssignmentPlanner = new DirectJobAssignmentPlanner(_routePlanner);
            _directSpatialAssignmentPlanner = new DirectSpatialJobAssignmentPlanner(
                new Dig.Domain.Navigation.NavigationPathfinder());
        }

        internal Result AssignExcavationCluster(
            CellId seed,
            string residentId,
            long tick)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            return AssignExcavationClusterToResidents(
                seed,
                new[] { residentId },
                tick);
        }

        private void RequireManualExcavationInitialized()
        {
            if (_specificAssignment == null
                || _releaseAssignment == null
                || _clusterPlanner == null
                || _directAssignmentPlanner == null
                || _directSpatialAssignmentPlanner == null
                || _candidateProvider == null)
            {
                throw new InvalidOperationException(
                    "Direct excavation assignment is not initialized.");
            }
        }
    }
}
