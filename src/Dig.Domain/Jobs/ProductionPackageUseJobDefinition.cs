using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class ProductionPackageUseJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public ProductionPackageUseJobDefinition(
        EntityId id,
        EntityId packageStackId,
        CellId targetCell,
        CellId workPosition,
        long packageVersion,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies)
    {
        if (packageStackId.IsEmpty || packageVersion < 0)
        {
            throw new ArgumentException("Package identity and version are required.");
        }

        PackageStackId = packageStackId;
        TargetCell = targetCell;
        WorkPosition = workPosition;
        PackageVersion = packageVersion;
    }

    public EntityId PackageStackId { get; }
    public CellId TargetCell { get; }
    public CellId WorkPosition { get; }
    public long PackageVersion { get; }

    public override string Description => $"UseProductionPackage:{PackageStackId}@{TargetCell}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}

}
