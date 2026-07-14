using System.Collections.ObjectModel;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs;

public enum BuildingWorkKind
{
    Construction = 0,
    Repair = 1,
    Demolition = 2,
}

public sealed class BuildingWorkJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public BuildingWorkJobDefinition(
        EntityId id,
        EntityId buildingId,
        BuildingWorkKind kind,
        CellId workPosition,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(buildingId));
        }

        BuildingId = buildingId;
        Kind = kind;
        WorkPosition = workPosition;
    }

    public EntityId BuildingId { get; }

    public BuildingWorkKind Kind { get; }

    public CellId WorkPosition { get; }

    public override string Description =>
        $"{Kind}:{BuildingId}@{WorkPosition}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}
