using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Buildings
{

public static class BuildingBoxErrors
{
    public static readonly DomainError DefinitionNotBoxEnabled = new DomainError(
        "building_box.definition.not_enabled",
        "The building definition does not use the BuildingBox policy.");

    public static readonly DomainError SourceStackMissing = new DomainError(
        "building_box.source.missing",
        "The selected BuildingBox stack does not exist.");

    public static readonly DomainError SourceItemMismatch = new DomainError(
        "building_box.source.item_mismatch",
        "The selected stack is not the box item required by this building.");

    public static readonly DomainError BoxMustBeSingle = new DomainError(
        "building_box.source.not_single",
        "A BuildingBox item must be a non-stackable quantity-one item.");

    public static readonly DomainError JobAlreadyExists = new DomainError(
        "building_box.job.exists",
        "The requested BuildingBox job id is already in use.");

    public static readonly DomainError JobTypeMismatch = new DomainError(
        "building_box.job.type_mismatch",
        "The requested job is not the BuildingBox job for this plan.");

    public static readonly DomainError InvalidJobStage = new DomainError(
        "building_box.job.invalid_stage",
        "The BuildingBox job is not at the required stage.");
}

public sealed class ConfirmBuildingBoxPlacementCommand : ICommand<Result>
{
    public ConfirmBuildingBoxPlacementCommand(
        EntityId buildingId,
        EntityId jobId,
        EntityId sourceStackId,
        BuildingDefinitionId definitionId,
        CellId origin,
        BuildingOrientation orientation,
        IReadOnlyCollection<CellId> reachableCells,
        int priority,
        long tick)
    {
        if (buildingId.IsEmpty || jobId.IsEmpty || sourceStackId.IsEmpty)
        {
            throw new ArgumentException("Building, job and source stack ids are required.");
        }

        if (definitionId.IsEmpty)
        {
            throw new ArgumentException("Building definition id is required.", nameof(definitionId));
        }

        if (priority < 0 || priority > 1000 || tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        BuildingId = buildingId;
        JobId = jobId;
        SourceStackId = sourceStackId;
        DefinitionId = definitionId;
        Origin = origin;
        Orientation = orientation;
        ReachableCells = reachableCells
            ?? throw new ArgumentNullException(nameof(reachableCells));
        Priority = priority;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public EntityId SourceStackId { get; }
    public BuildingDefinitionId DefinitionId { get; }
    public CellId Origin { get; }
    public BuildingOrientation Orientation { get; }
    public IReadOnlyCollection<CellId> ReachableCells { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class CommitBuildingBoxToSiteCommand : ICommand<Result>
{
    public CommitBuildingBoxToSiteCommand(EntityId buildingId, EntityId jobId, long tick)
    {
        BuildingId = buildingId;
        JobId = jobId;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class AddBuildingBoxAssemblyWorkCommand : ICommand<Result>
{
    public AddBuildingBoxAssemblyWorkCommand(
        EntityId buildingId,
        EntityId jobId,
        int workAmount,
        long tick)
    {
        BuildingId = buildingId;
        JobId = jobId;
        WorkAmount = workAmount;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public int WorkAmount { get; }
    public long Tick { get; }
}

public sealed class CompleteBuildingBoxAssemblyCommand : ICommand<Result>
{
    public CompleteBuildingBoxAssemblyCommand(EntityId buildingId, EntityId jobId, long tick)
    {
        BuildingId = buildingId;
        JobId = jobId;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CancelBuildingBoxPlanCommand : ICommand<Result>
{
    public CancelBuildingBoxPlanCommand(
        EntityId buildingId,
        string reason,
        long tick)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        }

        BuildingId = buildingId;
        Reason = reason.Trim();
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public string Reason { get; }
    public long Tick { get; }
}
}