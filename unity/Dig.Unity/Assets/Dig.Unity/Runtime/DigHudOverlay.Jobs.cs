using System;
using System.Collections.Generic;
using Dig.Application.Jobs;
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

        private int JobCount => _jobs.Count;

        private bool HasJobSelection => _selectedJob != null;

        public void SetJobs(IReadOnlyList<JobOverlayViewModel> jobs)
        {
            _jobs = jobs;
        }

        internal void SetToolAssignmentControls(DigTerrainWorkSession session)
        {
            _toolAssignmentSession = session
                ?? throw new ArgumentNullException(nameof(session));
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
