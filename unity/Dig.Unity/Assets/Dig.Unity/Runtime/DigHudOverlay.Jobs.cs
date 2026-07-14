using System.Collections.Generic;
using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private IReadOnlyList<JobOverlayViewModel> _jobs =
            System.Array.Empty<JobOverlayViewModel>();
        private JobOverlayViewModel? _selectedJob;

        private int JobCount => _jobs.Count;

        private bool HasJobSelection => _selectedJob != null;

        public void SetJobs(IReadOnlyList<JobOverlayViewModel> jobs)
        {
            _jobs = jobs;
        }

        public void SetJobSelection(JobOverlayViewModel? selected)
        {
            _selectedJob = selected;
            _selectedCell = null;
            _selectedAgent = null;
        }

        private void ClearJobSelection()
        {
            _selectedJob = null;
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
    }
}