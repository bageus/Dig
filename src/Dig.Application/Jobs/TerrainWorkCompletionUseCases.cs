using System;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

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
        IEventSink eventSink,
        IAgentSkillGrantService skillGrants)
    {
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
        _ = skillGrants ?? throw new ArgumentNullException(nameof(skillGrants));
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

        _ = job.AssignedAgentId
            ?? throw new InvalidOperationException(
                "An in-progress terrain job must retain its worker.");

        WorldState world = _worldRepository.Get();
        Result<CellSnapshot> targetResult = world.GetCell(terrainJob.Target.CellId);
        if (targetResult.IsFailure)
        {
            return Result<TerrainWorkCompletionResult>.Failure(targetResult.Error!);
        }

        CellSnapshot target = targetResult.Value;
        if (!target.IsSolid && !target.State.IsExcavationOpen)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.TargetNotSolid);
        }

        if (target.State.Designation != CellDesignation.Dig
            && !target.State.IsExcavationOpen)
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
        EntityId[] outputUnitIds = TerrainWorkOutputUnits.CreateIds(command);
        Result validation = TerrainWorkOutputUnits.Validate(
            inventory,
            command,
            outputUnitIds);
        if (validation.IsFailure)
        {
            return Result<TerrainWorkCompletionResult>.Failure(validation.Error!);
        }

        Result<WorldMutationResult> terrain = world.Excavate(
            terrainJob.Target.CellId,
            command.EmptyMaterialId,
            command.Tick,
            command.DepositInstanceId,
            command.DepositExpectedYield);
        if (terrain.IsFailure)
        {
            return Result<TerrainWorkCompletionResult>.Failure(terrain.Error!);
        }

        if (command.ProducesOutput)
        {
            Result added = inventory.AddUnits(
                outputUnitIds,
                command.OutputItemId,
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
