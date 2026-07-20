using System;
using Dig.Application.Inventory;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public static class TerrainWorkCompletionErrors
{
    public static readonly DomainError JobTypeUnsupported = new DomainError(
        "terrain_work.job_type_unsupported",
        "The requested job is not a terrain work job.");

    public static readonly DomainError JobNotReady = new DomainError(
        "terrain_work.job_not_ready",
        "The terrain work job is not waiting at its finalization stage.");

    public static readonly DomainError TargetNotSolid = new DomainError(
        "terrain_work.target_not_solid",
        "The target cell is no longer solid.");

    public static readonly DomainError TargetNotDesignated = new DomainError(
        "terrain_work.target_not_designated",
        "The target cell is no longer designated.");

    public static readonly DomainError UnknownOutputItem = new DomainError(
        "terrain_work.output_item_unknown",
        "The output item is not registered in Inventory.");
}

public sealed class CompleteTerrainWorkCommand
    : ICommand<Result<TerrainWorkCompletionResult>>
{
    public CompleteTerrainWorkCommand(
        EntityId jobId,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        MaterialId emptyMaterialId,
        long tick)
        : this(
            jobId,
            outputStackId,
            outputItemId,
            outputQuantity,
            emptyMaterialId,
            tick,
            producesOutput: true)
    {
    }

    private CompleteTerrainWorkCommand(
        EntityId jobId,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        MaterialId emptyMaterialId,
        long tick,
        bool producesOutput)
    {
        JobId = jobId;
        OutputStackId = outputStackId;
        OutputItemId = outputItemId;
        OutputQuantity = outputQuantity;
        EmptyMaterialId = emptyMaterialId;
        Tick = tick;
        ProducesOutput = producesOutput;
    }

    public EntityId JobId { get; }
    public EntityId OutputStackId { get; }
    public ItemId OutputItemId { get; }
    public int OutputQuantity { get; }
    public MaterialId EmptyMaterialId { get; }
    public long Tick { get; }
    public bool ProducesOutput { get; }

    public static CompleteTerrainWorkCommand WithoutOutput(
        EntityId jobId,
        MaterialId emptyMaterialId,
        long tick)
    {
        return new CompleteTerrainWorkCommand(
            jobId,
            default,
            default,
            outputQuantity: 0,
            emptyMaterialId,
            tick,
            producesOutput: false);
    }
}

public sealed class TerrainWorkCompletionResult
{
    public TerrainWorkCompletionResult(
        EntityId jobId,
        CellId targetCell,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        bool producedOutput,
        long worldVersion,
        long inventoryVersion)
    {
        JobId = jobId;
        TargetCell = targetCell;
        OutputStackId = outputStackId;
        OutputItemId = outputItemId;
        OutputQuantity = outputQuantity;
        ProducedOutput = producedOutput;
        WorldVersion = worldVersion;
        InventoryVersion = inventoryVersion;
    }

    public EntityId JobId { get; }
    public CellId TargetCell { get; }
    public EntityId OutputStackId { get; }
    public ItemId OutputItemId { get; }
    public int OutputQuantity { get; }
    public bool ProducedOutput { get; }
    public long WorldVersion { get; }
    public long InventoryVersion { get; }
}

public sealed class CompleteTerrainWorkCommandHandler
    : ICommandHandler<CompleteTerrainWorkCommand, Result<TerrainWorkCompletionResult>>
{
    private readonly IJobRepository _jobRepository;
    private readonly IWorldRepository _worldRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEventSink _eventSink;

    public CompleteTerrainWorkCommandHandler(
        IJobRepository jobRepository,
        IWorldRepository worldRepository,
        IInventoryRepository inventoryRepository,
        IEventSink eventSink)
    {
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<TerrainWorkCompletionResult> Handle(CompleteTerrainWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.Tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.Tick));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job is null)
        {
            return Result<TerrainWorkCompletionResult>.Failure(JobErrors.NotFound);
        }

        if (job.Definition is not DigJobDefinition terrainJob)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.JobTypeUnsupported);
        }

        if (job.Status != JobStatus.InProgress || job.Stage != JobStageKind.Finalize)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.JobNotReady);
        }

        WorldState world = _worldRepository.Get();
        Result<CellSnapshot> targetResult = world.GetCell(terrainJob.Target.CellId);
        if (targetResult.IsFailure)
        {
            return Result<TerrainWorkCompletionResult>.Failure(targetResult.Error!);
        }

        CellSnapshot target = targetResult.Value;
        if (!target.IsSolid)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.TargetNotSolid);
        }

        if (target.State.Designation != CellDesignation.Dig)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.TargetNotDesignated);
        }

        MaterialDefinition? emptyMaterial = world.Materials.Get(command.EmptyMaterialId);
        if (emptyMaterial is null)
        {
            return Result<TerrainWorkCompletionResult>.Failure(WorldErrors.UnknownMaterial);
        }

        if (emptyMaterial.IsSolid)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                WorldErrors.ExcavationRequiresEmptyMaterial);
        }

        InventoryState inventory = _inventoryRepository.Get();
        Result validation = ValidateOutput(inventory, command);
        if (validation.IsFailure)
        {
            return Result<TerrainWorkCompletionResult>.Failure(validation.Error!);
        }

        Result<WorldMutationResult> terrain = world.Excavate(
            terrainJob.Target.CellId,
            command.EmptyMaterialId,
            command.Tick);
        EnsureCommitStep(terrain.IsSuccess, terrain.Error);

        if (command.ProducesOutput)
        {
            Result added = inventory.AddStack(
                command.OutputStackId,
                command.OutputItemId,
                command.OutputQuantity,
                ItemLocation.InWorld(terrainJob.Target.CellId),
                command.Tick);
            EnsureCommitStep(added.IsSuccess, added.Error);
        }

        Result completed = jobs.Complete(command.JobId, command.Tick);
        EnsureCommitStep(completed.IsSuccess, completed.Error);

        _worldRepository.Save(world);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(world.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());

        return Result<TerrainWorkCompletionResult>.Success(
            new TerrainWorkCompletionResult(
                command.JobId,
                terrainJob.Target.CellId,
                command.OutputStackId,
                command.OutputItemId,
                command.OutputQuantity,
                command.ProducesOutput,
                world.Version,
                inventory.Version));
    }

    private static Result ValidateOutput(
        InventoryState inventory,
        CompleteTerrainWorkCommand command)
    {
        if (!command.ProducesOutput)
        {
            return Result.Success();
        }

        if (!inventory.Catalog.Contains(command.OutputItemId))
        {
            return Result.Failure(TerrainWorkCompletionErrors.UnknownOutputItem);
        }

        if (inventory.GetStack(command.OutputStackId) is not null)
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        if (command.OutputQuantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        ItemDefinition definition = inventory.Catalog.Get(command.OutputItemId);
        return command.OutputQuantity > definition.MaximumStackSize
            ? Result.Failure(InventoryErrors.StackSizeExceeded)
            : Result.Success();
    }

    private static void EnsureCommitStep(bool succeeded, DomainError? error)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException(
                $"A validated terrain work commit failed: {error}");
        }
    }
}

}