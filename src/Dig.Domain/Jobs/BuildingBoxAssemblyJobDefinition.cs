using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class BuildingBoxAssemblyJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] AssemblyStages =
    {
        JobStageKind.AcquireItem,
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public BuildingBoxAssemblyJobDefinition(
        EntityId id,
        EntityId buildingId,
        EntityId sourceStackId,
        CellId siteCell,
        CellId workPosition,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            AssemblyStages,
            dependencies)
    {
        if (buildingId.IsEmpty || sourceStackId.IsEmpty)
        {
            throw new ArgumentException("Building and source stack ids are required.");
        }

        BuildingId = buildingId;
        SourceStackId = sourceStackId;
        SiteCell = siteCell;
        WorkPosition = workPosition;
    }

    public EntityId BuildingId { get; }

    public EntityId SourceStackId { get; }

    public CellId SiteCell { get; }

    public CellId WorkPosition { get; }

    public override string Description => $"Deliver and assemble building box for {BuildingId}";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForItem(SourceStackId),
            ReservationKey.ForDestination(BuildingId),
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}
}
