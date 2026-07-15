using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

public enum BuildingStatus
{
    AwaitingMaterials = 0,
    ReadyToBuild = 1,
    UnderConstruction = 2,
    ReadyToComplete = 3,
    Completed = 4,
    Damaged = 5,
    Cancelled = 6,
    Removed = 7,
    AwaitingBox = 8,
}

public sealed class BuildingBoxPlanSnapshot
{
    public BuildingBoxPlanSnapshot(
        EntityId sourceStackId,
        EntityId jobId,
        BuildingBoxCommitState commitState)
    {
        if (sourceStackId.IsEmpty || jobId.IsEmpty)
        {
            throw new ArgumentException("Box plan ids cannot be empty.");
        }

        SourceStackId = sourceStackId;
        JobId = jobId;
        CommitState = commitState;
    }

    public EntityId SourceStackId { get; }

    public EntityId JobId { get; }

    public BuildingBoxCommitState CommitState { get; }
}

public sealed class BuildingSnapshot
{
    public BuildingSnapshot(
        EntityId id,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        IReadOnlyCollection<CellId> footprint,
        CellId workPosition,
        BuildingStatus status,
        int completedWork,
        int durability,
        long version,
        string? diagnosticReason,
        BuildingBoxPlanSnapshot? boxPlan = null)
    {
        Id = id;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Origin = origin;
        Orientation = orientation;
        Footprint = new ReadOnlyCollection<CellId>(footprint.OrderBy(cell => cell).ToArray());
        WorkPosition = workPosition;
        Status = status;
        CompletedWork = completedWork;
        Durability = durability;
        Version = version;
        DiagnosticReason = diagnosticReason;
        BoxPlan = boxPlan;
    }

    public EntityId Id { get; }

    public BuildingDefinition Definition { get; }

    public CellId Origin { get; }

    public BuildingOrientation Orientation { get; }

    public IReadOnlyList<CellId> Footprint { get; }

    public CellId WorkPosition { get; }

    public BuildingStatus Status { get; }

    public int CompletedWork { get; }

    public int Durability { get; }

    public long Version { get; }

    public string? DiagnosticReason { get; }

    public BuildingBoxPlanSnapshot? BoxPlan { get; }

    public bool IsActive => Status != BuildingStatus.Cancelled
        && Status != BuildingStatus.Removed;
}

public sealed class BuildingPlaced : IDomainEvent
{
    public BuildingPlaced(long tick, EntityId buildingId, BuildingDefinitionId definitionId)
    {
        Tick = tick;
        BuildingId = buildingId;
        DefinitionId = definitionId;
    }

    public long Tick { get; }

    public EntityId BuildingId { get; }

    public BuildingDefinitionId DefinitionId { get; }
}

public sealed class BuildingBoxPlanCreated : IDomainEvent
{
    public BuildingBoxPlanCreated(
        long tick,
        EntityId buildingId,
        EntityId sourceStackId,
        EntityId jobId,
        BuildingDefinitionId definitionId)
    {
        Tick = tick;
        BuildingId = buildingId;
        SourceStackId = sourceStackId;
        JobId = jobId;
        DefinitionId = definitionId;
    }

    public long Tick { get; }
    public EntityId BuildingId { get; }
    public EntityId SourceStackId { get; }
    public EntityId JobId { get; }
    public BuildingDefinitionId DefinitionId { get; }
}

public sealed class BuildingBoxCommitChanged : IDomainEvent
{
    public BuildingBoxCommitChanged(
        long tick,
        EntityId buildingId,
        BuildingBoxCommitState previous,
        BuildingBoxCommitState current)
    {
        Tick = tick;
        BuildingId = buildingId;
        Previous = previous;
        Current = current;
    }

    public long Tick { get; }
    public EntityId BuildingId { get; }
    public BuildingBoxCommitState Previous { get; }
    public BuildingBoxCommitState Current { get; }
}

public sealed class BuildingCompleted : IDomainEvent
{
    public BuildingCompleted(long tick, EntityId buildingId)
    {
        Tick = tick;
        BuildingId = buildingId;
    }

    public long Tick { get; }

    public EntityId BuildingId { get; }
}

public sealed class BuildingRemoved : IDomainEvent
{
    public BuildingRemoved(long tick, EntityId buildingId, string reason)
    {
        Tick = tick;
        BuildingId = buildingId;
        Reason = reason;
    }

    public long Tick { get; }

    public EntityId BuildingId { get; }

    public string Reason { get; }
}

public sealed class BuildingDamaged : IDomainEvent
{
    public BuildingDamaged(long tick, EntityId buildingId, int durability)
    {
        Tick = tick;
        BuildingId = buildingId;
        Durability = durability;
    }

    public long Tick { get; }

    public EntityId BuildingId { get; }

    public int Durability { get; }
}
}