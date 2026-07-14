using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public abstract class JobDefinition
{
    private readonly EntityId[] _dependencies;
    private readonly JobStageKind[] _stages;

    protected JobDefinition(
        EntityId id,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<JobStageKind> stages,
        IEnumerable<EntityId>? dependencies)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(id));
        }

        if (priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (createdTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(createdTick));
        }

        if (stages is null)
        {
            throw new ArgumentNullException(nameof(stages));
        }

        _stages = stages.ToArray();
        if (_stages.Length == 0 || _stages.Any(stage => stage == JobStageKind.None))
        {
            throw new ArgumentException("A job needs at least one concrete stage.", nameof(stages));
        }

        if (_stages.Distinct().Count() != _stages.Length)
        {
            throw new ArgumentException("Job stages cannot contain duplicates.", nameof(stages));
        }

        Id = id;
        Priority = priority;
        CreatedTick = createdTick;
        RetryPolicy = retryPolicy;
        _dependencies = (dependencies ?? Array.Empty<EntityId>())
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();

        if (_dependencies.Any(value => value.IsEmpty || value == id))
        {
            throw new ArgumentException("Dependencies must contain valid other job ids.", nameof(dependencies));
        }

        if (_dependencies.Distinct().Count() != _dependencies.Length)
        {
            throw new ArgumentException("Dependencies cannot contain duplicates.", nameof(dependencies));
        }
    }

    public EntityId Id { get; }

    public int Priority { get; }

    public long CreatedTick { get; }

    public JobRetryPolicy RetryPolicy { get; }

    public IReadOnlyList<EntityId> Dependencies =>
        new ReadOnlyCollection<EntityId>(_dependencies);

    public IReadOnlyList<JobStageKind> Stages =>
        new ReadOnlyCollection<JobStageKind>(_stages);

    public abstract string Description { get; }

    public abstract IReadOnlyList<ReservationKey> CreateReservationKeys();
}

public sealed class DigJobTarget
{
    public DigJobTarget(CellId cellId)
    {
        CellId = cellId;
    }

    public CellId CellId { get; }

    public override string ToString()
    {
        return $"Dig:{CellId}";
    }
}

public sealed class DigJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] DigStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public DigJobDefinition(
        EntityId id,
        DigJobTarget target,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, DigStages, dependencies)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public DigJobTarget Target { get; }

    public override string Description => Target.ToString();

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(Target.CellId),
            ReservationKey.ForDesignation(Target.CellId),
        });
    }
}
}
