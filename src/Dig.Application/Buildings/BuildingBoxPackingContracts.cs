using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;

namespace Dig.Application.Buildings
{

public static class BuildingBoxPackingErrors
{
    public static readonly DomainError PackingDisabled = new DomainError(
        "building_box.packing.disabled",
        "The selected building does not support packing.");

    public static readonly DomainError PackingPlanMissing = new DomainError(
        "building_box.packing.plan_missing",
        "The building does not have an active packing plan.");

    public static readonly DomainError JobTypeMismatch = new DomainError(
        "building_box.packing.job_type_mismatch",
        "The requested job is not the packing job for this building.");

    public static readonly DomainError InvalidJobStage = new DomainError(
        "building_box.packing.invalid_job_stage",
        "The packing job is not at the required stage.");

    public static readonly DomainError OutputStackExists = new DomainError(
        "building_box.packing.output_exists",
        "The requested output stack id is already in use.");
}

public sealed class StartBuildingBoxPackingCommand : ICommand<Result>
{
    public StartBuildingBoxPackingCommand(
        EntityId buildingId,
        EntityId jobId,
        EntityId outputStackId,
        int priority,
        long tick)
    {
        if (buildingId.IsEmpty || jobId.IsEmpty || outputStackId.IsEmpty)
        {
            throw new ArgumentException("Building, job and output stack ids are required.");
        }

        if (priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        BuildingId = buildingId;
        JobId = jobId;
        OutputStackId = outputStackId;
        Priority = priority;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public EntityId OutputStackId { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class AddBuildingBoxPackingWorkCommand : ICommand<Result>
{
    public AddBuildingBoxPackingWorkCommand(
        EntityId buildingId,
        EntityId jobId,
        int workAmount,
        long tick)
    {
        if (buildingId.IsEmpty || jobId.IsEmpty)
        {
            throw new ArgumentException("Building and job ids are required.");
        }

        if (workAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workAmount));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

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

public sealed class CompleteBuildingBoxPackingCommand : ICommand<Result>
{
    public CompleteBuildingBoxPackingCommand(EntityId buildingId, EntityId jobId, long tick)
    {
        if (buildingId.IsEmpty || jobId.IsEmpty)
        {
            throw new ArgumentException("Building and job ids are required.");
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        BuildingId = buildingId;
        JobId = jobId;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CancelBuildingBoxPackingCommand : ICommand<Result>
{
    public CancelBuildingBoxPackingCommand(
        EntityId buildingId,
        string reason,
        long tick)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Packing cancellation reason is required.", nameof(reason));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
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
