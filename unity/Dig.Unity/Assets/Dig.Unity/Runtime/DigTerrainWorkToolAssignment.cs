using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly JobToolPreparationModeControl _toolPreparationModeControl =
            new JobToolPreparationModeControl();
        private PrepareSuggestedJobToolHandler? _prepareSuggestedTool;
        private BypassSuggestedJobToolHandler? _bypassSuggestedTool;
        private bool _toolAwareJobAssignmentInitialized;

        internal JobToolPreparationMode SelectedToolPreparationMode =>
            _toolPreparationModeControl.Mode;

        internal string ToolPreparationModeLabel => _toolPreparationModeControl.Label;

        internal bool SelectToolPreparationMode(JobToolPreparationMode mode)
        {
            return _toolPreparationModeControl.Select(mode);
        }

        internal Result PrepareSuggestedJobTool(string jobId, long tick)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job id is required.", nameof(jobId));
            }

            if (_prepareSuggestedTool == null)
            {
                return Result.Failure(JobErrors.ToolPreparationUnavailable);
            }

            return _prepareSuggestedTool.Handle(new PrepareSuggestedJobToolCommand(
                EntityId.Parse(jobId),
                tick));
        }

        internal Result BypassSuggestedJobTool(string jobId, long tick)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job id is required.", nameof(jobId));
            }

            if (_bypassSuggestedTool == null)
            {
                return Result.Failure(JobErrors.ToolPreparationUnavailable);
            }

            return _bypassSuggestedTool.Handle(new BypassSuggestedJobToolCommand(
                EntityId.Parse(jobId),
                tick));
        }

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
            _prepareSuggestedTool = new PrepareSuggestedJobToolHandler(
                _jobRepository,
                preparation,
                journal,
                journal);
            _bypassSuggestedTool = new BypassSuggestedJobToolHandler(
                _jobRepository,
                journal,
                journal,
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
                journal,
                _toolPreparationModeControl);
        }
    }
}
