using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dig.Presentation.Jobs
{

public delegate void JobActionHandler(string jobId, JobActionViewModel action);

public sealed class JobActionRoute
{
    public JobActionRoute(JobActionKind kind, JobActionHandler handler)
    {
        if (!Enum.IsDefined(typeof(JobActionKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public JobActionKind Kind { get; }

    public JobActionHandler Handler { get; }
}

public sealed class JobActionDispatcher
{
    private readonly IReadOnlyDictionary<JobActionKind, JobActionHandler> _handlers;

    public JobActionDispatcher(IEnumerable<JobActionRoute> routes)
    {
        if (routes is null)
        {
            throw new ArgumentNullException(nameof(routes));
        }

        Dictionary<JobActionKind, JobActionHandler> handlers =
            new Dictionary<JobActionKind, JobActionHandler>();
        foreach (JobActionRoute route in routes)
        {
            if (route is null)
            {
                throw new ArgumentException(
                    "Job action routes cannot contain null values.",
                    nameof(routes));
            }

            if (!handlers.TryAdd(route.Kind, route.Handler))
            {
                throw new ArgumentException(
                    $"A Job action route is already registered for {route.Kind}.",
                    nameof(routes));
            }
        }

        _handlers = new ReadOnlyDictionary<JobActionKind, JobActionHandler>(handlers);
    }

    public void Dispatch(string jobId, JobActionViewModel action)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!action.IsEnabled)
        {
            throw new InvalidOperationException(
                $"Disabled Job action {action.Kind} cannot be dispatched: " +
                $"{action.DisabledReasonCode} | {action.DisabledReasonMessage}");
        }

        if (!_handlers.TryGetValue(action.Kind, out JobActionHandler? handler))
        {
            throw new InvalidOperationException(
                $"Unsupported Job action kind: {action.Kind}.");
        }

        handler(jobId.Trim(), action);
    }
}
}
