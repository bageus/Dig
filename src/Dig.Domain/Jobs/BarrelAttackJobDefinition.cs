using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class BarrelAttackJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public BarrelAttackJobDefinition(
        EntityId id,
        EntityId barrelId,
        CellId targetCell,
        CellId workPosition,
        long barrelVersion,
        long contentsGeneration,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies)
    {
        if (barrelId.IsEmpty)
        {
            throw new ArgumentException("Barrel id cannot be empty.", nameof(barrelId));
        }

        if (barrelVersion < 0 || contentsGeneration < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(barrelVersion));
        }

        BarrelId = barrelId;
        TargetCell = targetCell;
        WorkPosition = workPosition;
        BarrelVersion = barrelVersion;
        ContentsGeneration = contentsGeneration;
    }

    public EntityId BarrelId { get; }
    public CellId TargetCell { get; }
    public CellId WorkPosition { get; }
    public long BarrelVersion { get; }
    public long ContentsGeneration { get; }

    public override string Description => $"AttackBarrel:{BarrelId}@{TargetCell}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        // The barrel target intentionally has no exclusive reservation: multiple residents
        // may attack it concurrently and the first authoritative destruction commit wins.
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}

}