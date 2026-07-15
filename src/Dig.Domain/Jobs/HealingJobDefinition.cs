using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class HealingJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] HealingStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public HealingJobDefinition(
        EntityId id,
        EntityId patientId,
        CellId workPosition,
        int healthRestored,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, HealingStages, dependencies)
    {
        if (patientId.IsEmpty)
        {
            throw new ArgumentException("Patient id cannot be empty.", nameof(patientId));
        }

        if (healthRestored <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(healthRestored));
        }

        PatientId = patientId;
        WorkPosition = workPosition;
        HealthRestored = healthRestored;
    }

    public EntityId PatientId { get; }

    public CellId WorkPosition { get; }

    public int HealthRestored { get; }

    public override string Description => $"Heal:{PatientId}@{WorkPosition}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForDestination(PatientId),
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}
}
