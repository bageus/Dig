using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private IReadOnlyList<JobOverlayViewModel> _jobs =
            Array.Empty<JobOverlayViewModel>();
        private JobOverlayViewModel? _selectedJob;
        private DigTerrainWorkSession? _toolAssignmentSession;
        private DigJobRenderer? _toolJobRenderer;
        private JobActionDispatcher? _jobActionDispatcher;

        private int JobCount => _jobs.Count;

        private bool HasJobSelection => _selectedJob != null;

        private JobActionDispatcher JobActions =>
            _jobActionDispatcher ??= new JobActionDispatcher(new[]
            {
                new JobActionRoute(
                    JobActionKind.PrepareSuggestedTool,
                    ExecutePrepareSuggestedToolAction),
            });

        public void SetJobs(IReadOnlyList<JobOverlayViewModel> jobs)
        {
            _jobs = jobs;
        }

        internal void SetToolAssignmentControls(
            DigTerrainWorkSession session,
            DigJobRenderer jobRenderer)
        {
            _toolAssignmentSession = session
                ?? throw new ArgumentNullException(nameof(session));
            _toolJobRenderer = jobRenderer
                ?? throw new ArgumentNullException(nameof(jobRenderer));
        }

        public void SetJobSelection(JobOverlayViewModel? selected)
        {
            _selectedJob = selected;
            _selectedCell = null;
            _selectedAgent = null;
            _residentInventory = null;
            ClearBuildingSelection();
        }

        private void ClearJobSelection()
        {
            _selectedJob = null;
        }

        private void DrawToolAssignmentControls()
        {
            if (_toolAssignmentSession == null)
            {
                return;
            }

            JobToolPreparationMode selected =
                _toolAssignmentSession.SelectedToolPreparationMode;
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"Tool policy: {_toolAssignmentSession.ToolPreparationModeLabel}",
                GUILayout.Width(180f));
            bool previousEnabled = GUI.enabled;
            GUI.enabled = selected != JobToolPreparationMode.Automatic;
            if (GUILayout.Button("Automatic", GUILayout.Width(90f)))
            {
                SelectToolPreparationMode(JobToolPreparationMode.Automatic);
            }

            GUI.enabled = selected != JobToolPreparationMode.Suggest;
            if (GUILayout.Button("Suggest only", GUILayout.Width(95f)))
            {
                SelectToolPreparationMode(JobToolPreparationMode.Suggest);
            }

            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }

        private void SelectToolPreparationMode(JobToolPreparationMode mode)
        {
            if (_toolAssignmentSession == null
                || !_toolAssignmentSession.SelectToolPreparationMode(mode))
            {
                return;
            }

            SetStatus(
                $"Tool policy set to {_toolAssignmentSession.ToolPreparationModeLabel}. " +
                "Applies to future Job assignments.");
        }

        private void DrawJobSelection()
        {
            if (_selectedJob == null)
            {
                return;
            }

            JobOverlayViewModel job = _selectedJob;
            GUILayout.Label("SELECTED JOB");
            GUILayout.Label($"{job.Description} | priority {job.Priority}");
            GUILayout.Label($"Status: {job.Status} | stage: {job.Stage}");
            GUILayout.Label($"Worker: {job.AssignedAgentId ?? "unassigned"}");
            if (job.HasTarget)
            {
                GUILayout.Label($"Target cell: {job.TargetX},{job.TargetY}");
            }

            if (job.PreferredToolKind.HasValue)
            {
                GUILayout.Label($"Preferred tool: {job.PreferredToolKind.Value}");
            }

            DrawJobAssignmentDiagnostic(job.AssignmentDiagnostic);
            DrawJobActions(job);
            GUILayout.Label($"Retries: {job.RetryCount} | next: {job.NextRetryTick}");
            if (job.Reason != null)
            {
                GUILayout.Label($"Reason: {job.Reason}");
            }

            GUILayout.Space(5f);
            GUILayout.Label($"RESERVATIONS ({job.Reservations.Count})");
            foreach (JobReservationViewModel reservation in job.Reservations)
            {
                GUILayout.Label(
                    $"{reservation.Kind}: {reservation.Value} | tick {reservation.AcquiredTick}");
            }
        }

        private void DrawJobActions(JobOverlayViewModel job)
        {
            foreach (JobActionViewModel action in job.Actions)
            {
                DrawJobAction(job.Id, action);
            }
        }

        private void DrawJobAction(string jobId, JobActionViewModel action)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && action.IsEnabled;
            bool invoked = GUILayout.Button(action.Label, GUILayout.Width(190f));
            GUI.enabled = previousEnabled;

            if (invoked)
            {
                JobActions.Dispatch(jobId, action);
            }

            if (!action.IsEnabled)
            {
                GUILayout.Label(
                    $"Unavailable: {action.DisabledReasonCode} | " +
                    action.DisabledReasonMessage);
            }
        }

        private void ExecutePrepareSuggestedToolAction(
            string jobId,
            JobActionViewModel _)
        {
            ExecuteSuggestedToolPreparation(jobId);
        }

        private void ExecuteSuggestedToolPreparation(string jobId)
        {
            if (_toolAssignmentSession == null)
            {
                SetStatus("unity.tool_assignment.not_initialized");
                return;
            }

            long tick = _simulation?.CurrentTick ?? _tick;
            Result result = _toolAssignmentSession.PrepareSuggestedJobTool(jobId, tick);
            SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            IReadOnlyList<JobOverlayViewModel> jobs = _toolAssignmentSession.LoadJobs();
            SetJobs(jobs);
            _toolJobRenderer?.Render(jobs);
            _selectedJob = jobs.FirstOrDefault(
                value => string.Equals(value.Id, jobId, StringComparison.Ordinal));
            if (_toolJobRenderer != null)
            {
                DigJobVisual? selected = _toolJobRenderer.SelectById(jobId);
                _selectedJob = selected?.Model ?? _selectedJob;
            }

            _simulation?.RefreshEquipmentPresentation();
            SetStatus("Suggested tool equipped. The active Job and reservations were preserved.");
        }

        private static void DrawJobAssignmentDiagnostic(
            JobAssignmentDiagnosticViewModel? diagnostic)
        {
            if (diagnostic == null)
            {
                return;
            }

            if (diagnostic.IsFailure)
            {
                GUILayout.Label(
                    $"Assignment failed: {diagnostic.FailureCode} | {diagnostic.FailureMessage}");
                return;
            }

            string tool = diagnostic.ToolStackId ?? "none";
            GUILayout.Label(
                $"Tool preparation: {diagnostic.ToolPreparation} | stack {tool}");
            GUILayout.Label(
                $"Assignment score: {diagnostic.Score} | tick {diagnostic.Tick}");
        }
    }
}
