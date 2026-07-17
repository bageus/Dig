using System.Collections.Generic;
using Dig.Presentation.Jobs;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private IReadOnlyList<JobOverlayViewModel>? _jobSelectionSource;

        private void LateUpdate()
        {
            if (object.ReferenceEquals(_jobSelectionSource, _jobs))
            {
                return;
            }

            _jobSelectionSource = _jobs;
            _selectedJob = JobSelectionProjection.Resolve(
                _jobs,
                _selectedJob?.Id);
        }
    }
}
