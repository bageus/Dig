using System;
using System.Collections.Generic;
using Dig.Application.Jobs;
using Dig.Application.Navigation;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private ValidateTunnelManualPlacementHandler? _tunnelManualValidation;
    private CreateTunnelManualWorkHandler? _tunnelManualCreation;
    private CancelTunnelManualWorkHandler? _tunnelManualCancellation;
    private CompleteTunnelManualWorkHandler? _tunnelManualCompletion;
    private ulong _tunnelManualJobSequence = 1UL;

    internal Result<TunnelManualPlacementPlan> ValidateTunnelManualPlacement(
        string residentId,
        string stackId,
        CellId targetCell)
    {
        EnsureTunnelManualRuntime();
        return _tunnelManualValidation!.Handle(
            new ValidateTunnelManualPlacementQuery(
                EntityId.Parse(residentId),
                EntityId.Parse(stackId),
                targetCell));
    }

    internal Result CreateTunnelManualWork(
        string residentId,
        string stackId,
        CellId targetCell,
        long tick)
    {
        EnsureTunnelManualRuntime();
        Result prepared = PrepareResidentsForDirectCommand(
            new[] { residentId },
            tick);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        Result<EntityId> created = _tunnelManualCreation!.Handle(
            new CreateTunnelManualWorkCommand(
                NextTunnelManualJobId(),
                EntityId.Parse(residentId),
                EntityId.Parse(stackId),
                targetCell,
                tick));
        return created.IsSuccess
            ? Result.Success()
            : Result.Failure(created.Error!);
    }

    private bool TryPlanTunnelManualWorkMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (job.Definition is not TunnelManualWorkJobDefinition definition)
        {
            return false;
        }

        if (job.AssignedAgentId != definition.OwnerResidentId)
        {
            CancelTunnelManualJob(job, tick);
            return true;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(start, definition.TargetCell, navigation.NavigationVersion));
        if (!path.Succeeded || path.Path == null)
        {
            CancelTunnelManualJob(job, tick);
            return true;
        }

        _routePlans[job.Id] = new TerrainWorkRoutePlan(
            job.Id,
            definition.TargetCell,
            definition.TargetCell,
            path,
            candidateCount: 1);
        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : definition.TargetCell;
        return true;
    }

    private Result AdvanceTunnelManualWork(
        JobSnapshot job,
        AgentViewModel agent,
        long tick)
    {
        if (job.Definition is not TunnelManualWorkJobDefinition definition)
        {
            return Result.Success();
        }

        if (job.AssignedAgentId != definition.OwnerResidentId)
        {
            return CancelTunnelManualJob(job, tick);
        }

        CellId current = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        if (current != definition.TargetCell)
        {
            return Result.Success();
        }

        if (job.Stage == JobStageKind.Finalize)
        {
            Result completed = _tunnelManualCompletion!.Handle(
                new CompleteTunnelManualWorkCommand(job.Id, tick));
            if (completed.IsSuccess)
            {
                _routePlans.Remove(job.Id);
                PublishTunnelInfrastructureVisuals();
            }

            return completed;
        }

        return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
    }

    private Result CancelTunnelManualJob(JobSnapshot job, long tick)
    {
        EnsureTunnelManualRuntime();
        Result cancelled = _tunnelManualCancellation!.Handle(
            new CancelTunnelManualWorkCommand(job.Id, tick));
        if (cancelled.IsSuccess)
        {
            _routePlans.Remove(job.Id);
        }

        return cancelled;
    }

    private Result CancelTunnelManualForDirectCommand(JobSnapshot job, long tick)
    {
        return CancelTunnelManualJob(job, tick);
    }

    private EntityId NextTunnelManualJobId()
    {
        return EntityId.Parse(
            "b" + (_tunnelManualJobSequence++).ToString("x31"));
    }

    private void EnsureTunnelManualRuntime()
    {
        EnsureTunnelInfrastructureRuntime();
        if (_tunnelManualValidation != null)
        {
            return;
        }

        _tunnelManualValidation = new ValidateTunnelManualPlacementHandler(
            _tunnelInfrastructure!,
            _inventoryRepository);
        _tunnelManualCreation = new CreateTunnelManualWorkHandler(
            _tunnelInfrastructure!,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelManualCancellation = new CancelTunnelManualWorkHandler(
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelManualCompletion = new CompleteTunnelManualWorkHandler(
            _tunnelInfrastructure!,
            _inventoryRepository,
            _jobRepository,
            _journal,
            _skillGrants);
    }

    private void ResetTunnelManualRuntimeHandlers()
    {
        _tunnelManualValidation = null;
        _tunnelManualCreation = null;
        _tunnelManualCancellation = null;
        _tunnelManualCompletion = null;
        EnsureTunnelManualRuntime();
    }
}

}
