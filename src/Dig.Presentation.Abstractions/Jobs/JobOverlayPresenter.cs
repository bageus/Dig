using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Jobs;

namespace Dig.Presentation.Jobs
{

public sealed class JobOverlayPresenter
{
    private readonly IQueryHandler<GetJobsQuery, IReadOnlyList<JobSnapshot>> _jobs;
    private readonly IQueryHandler<GetJobReservationsQuery, IReadOnlyList<ReservationSnapshot>> _reservations;

    public JobOverlayPresenter(
        IQueryHandler<GetJobsQuery, IReadOnlyList<JobSnapshot>> jobs,
        IQueryHandler<GetJobReservationsQuery, IReadOnlyList<ReservationSnapshot>> reservations)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
    }

    public IReadOnlyList<JobOverlayViewModel> Load()
    {
        IReadOnlyList<JobSnapshot> jobs = _jobs.Handle(new GetJobsQuery());
        IReadOnlyList<ReservationSnapshot> reservations = _reservations.Handle(
            new GetJobReservationsQuery());
        JobOverlayViewModel[] models = new JobOverlayViewModel[jobs.Count];
        for (int index = 0; index < jobs.Count; index++)
        {
            models[index] = Map(jobs[index], reservations);
        }

        return new ReadOnlyCollection<JobOverlayViewModel>(models);
    }

    private static JobOverlayViewModel Map(
        JobSnapshot job,
        IReadOnlyList<ReservationSnapshot> reservations)
    {
        JobReservationViewModel[] values = reservations
            .Where(item => item.JobId == job.Id)
            .Select(item => new JobReservationViewModel(
                item.Key.Kind.ToString(),
                item.Key.Value,
                item.AgentId.ToString(),
                item.AcquiredTick))
            .OrderBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .ToArray();
        int? targetX = null;
        int? targetY = null;
        DigJobDefinition? digging = job.Definition as DigJobDefinition;
        if (digging != null)
        {
            targetX = digging.Target.CellId.X;
            targetY = digging.Target.CellId.Y;
        }

        return new JobOverlayViewModel(
            job.Id.ToString(),
            job.Definition.Description,
            job.Status.ToString(),
            job.Stage.ToString(),
            job.Definition.Priority,
            job.AssignedAgentId?.ToString(),
            targetX,
            targetY,
            job.RetryCount,
            job.NextRetryTick,
            job.Reason?.ToString(),
            new ReadOnlyCollection<JobReservationViewModel>(values));
    }
}
}