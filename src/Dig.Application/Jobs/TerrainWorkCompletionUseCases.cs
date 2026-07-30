using System;
using System.Collections.Generic;
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
    private readonly MiningOutputCommitState _miningOutputCommits;

    public CompleteTerrainWorkCommandHandler(
        IJobRepository jobRepository,
        IWorldRepository worldRepository,
        IInventoryRepository inventoryRepository,
        IEventSink eventSink,
        IAgentSkillGrantService skillGrants,
        MiningOutputCommitState? miningOutputCommits = null)
    {
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
        _ = skillGrants ?? throw new ArgumentNullException(nameof(skillGrants));
        _miningOutputCommits = miningOutputCommits ?? new MiningOutputCommitState();
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

        CellId targetCell = terrainJob.Target.CellId;
        if (command.ResolvedPlanCell.HasValue
            && command.ResolvedPlanCell.Value != targetCell)
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.OutputPlanCellMismatch);
        }

        WorldState world = _worldRepository.Get();
        Result<CellSnapshot> targetResult = world.GetCell(targetCell);
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

        MiningOutputPlan outputPlan = command.CreatePlan(targetCell);
        if (_miningOutputCommits.IsCommitted(targetCell))
        {
            return Result<TerrainWorkCompletionResult>.Failure(
                TerrainWorkCompletionErrors.OutputAlreadyCommitted);
        }

        if (command.HasResolvedOutputPlan)
        {
            try
            {
                _miningOutputCommits.Validate(
                    outputPlan,
                    outputUnitIds,
                    inventory,
                    world.TerrainDeposits);
            }
            catch (Exception error) when (
                error is ArgumentException
                || error is InvalidOperationException
                || error is OverflowException)
            {
                return Result<TerrainWorkCompletionResult>.Failure(new DomainError(
                    "terrain_work.mining_output_invalid",
                    error.Message));
            }
        }

        Result<WorldMutationResult> terrain = world.Excavate(
            targetCell,
            command.EmptyMaterialId,
            command.Tick,
            command.DepositInstanceId,
            command.DepositExpectedYield);
        if (terrain.IsFailure)
        {
            return Result<TerrainWorkCompletionResult>.Failure(terrain.Error!);
        }

        IReadOnlyList<TerrainWorkProducedOutput> produced =
            TerrainWorkOutputUnits.AddToInventory(
                inventory,
                command,
                outputUnitIds,
                ItemLocation.InWorld(targetCell),
                command.Tick);

        Result completed = jobs.Complete(command.JobId, command.Tick);
        EnsureCommitStep(completed.IsSuccess, completed.Error);
        _miningOutputCommits.Record(outputPlan, outputUnitIds);

        _worldRepository.Save(world);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(world.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());

        return Result<TerrainWorkCompletionResult>.Success(
            new TerrainWorkCompletionResult(
                command.JobId,
                targetCell,
                produced,
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
