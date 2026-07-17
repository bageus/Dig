using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private bool _toolAwareJobAssignmentInitialized;

        internal void InitializeToolAwareJobAssignment(InMemoryExecutionJournal journal)
        {
            if (journal == null)
            {
                throw new ArgumentNullException(nameof(journal));
            }

            if (_toolAwareJobAssignmentInitialized)
            {
                return;
            }

            if (_buildingInventoryRepository == null
                || _candidateProvider == null
                || _buildingPackingCandidates == null
                || _buildingBoxAssemblyCandidates == null)
            {
                throw new InvalidOperationException(
                    "Tool-aware job assignment requires initialized runtime candidates and inventory.");
            }

            InventoryJobToolPreparationService preparation =
                new InventoryJobToolPreparationService(
                    _buildingInventoryRepository,
                    journal);
            _assignmentHandler = CreateToolAwareAssignmentHandler(
                _candidateProvider,
                preparation,
                journal);
            _buildingPackingAssignment = CreateToolAwareAssignmentHandler(
                _buildingPackingCandidates,
                preparation,
                journal);
            _buildingBoxAssemblyAssignment = CreateToolAwareAssignmentHandler(
                _buildingBoxAssemblyCandidates,
                preparation,
                journal);
            _toolAwareJobAssignmentInitialized = true;
        }

        private AssignAvailableJobsHandler CreateToolAwareAssignmentHandler(
            InMemoryJobCandidateProvider candidates,
            InventoryJobToolPreparationService preparation,
            InMemoryExecutionJournal journal)
        {
            InventoryAwareJobCandidateProvider inventoryAware =
                new InventoryAwareJobCandidateProvider(
                    candidates,
                    _buildingInventoryRepository!,
                    ResidentEquipmentRates);
            return new AssignAvailableJobsHandler(
                _jobRepository,
                inventoryAware,
                journal,
                preparation,
                journal);
        }
    }
}
