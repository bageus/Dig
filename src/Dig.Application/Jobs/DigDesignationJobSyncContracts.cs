using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public interface IDigJobIdSource
{
    EntityId Next();
}

public sealed class SyncDigDesignationJobsCommand
    : ICommand<DigDesignationJobSyncReport>
{
    public SyncDigDesignationJobsCommand(int priority, long tick)
    {
        if (priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Priority = priority;
        Tick = tick;
    }

    public int Priority { get; }

    public long Tick { get; }
}

public readonly struct CreatedDigDesignationJob
{
    public CreatedDigDesignationJob(EntityId jobId, CellId cellId)
    {
        JobId = jobId;
        CellId = cellId;
    }

    public EntityId JobId { get; }

    public CellId CellId { get; }
}

public sealed class DigDesignationJobSyncReport
{
    public DigDesignationJobSyncReport(
        IReadOnlyCollection<CreatedDigDesignationJob> created,
        IReadOnlyCollection<EntityId> cancelled)
    {
        Created = new ReadOnlyCollection<CreatedDigDesignationJob>(created
            .OrderBy(value => value.CellId)
            .ThenBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ToArray());
        Cancelled = new ReadOnlyCollection<EntityId>(cancelled
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<CreatedDigDesignationJob> Created { get; }

    public IReadOnlyList<EntityId> Cancelled { get; }
}
}
