using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private const int ProductionPackageUsePriority = 850;

    internal bool IsProductionPackage(EntityId stackId)
    {
        return _productionRepository?.Get().GetOutputPackage(stackId) != null;
    }

    internal bool IsClosedProductionPackage(EntityId stackId)
    {
        return _productionRepository?.Get().GetOutputPackage(stackId)?.IsClosed == true;
    }

    internal bool CanDirectUseProductionPackage(
        EntityId stackId,
        CellId workerCell,
        out CellId workPosition)
    {
        workPosition = default;
        ProductionOutputPackageSnapshot? package = _productionRepository?.Get()
            .GetOutputPackage(stackId);
        ItemStackSnapshot? stack = _buildingInventoryRepository?.Get().GetStack(stackId);
        return package?.IsClosed == true
            && stack?.Location.Kind == ItemLocationKind.World
            && stack.Location.HasCell
            && TryResolveBarrelWorkPosition(
                stack.Location.CellId,
                workerCell,
                out workPosition);
    }

    internal Result StartDirectProductionPackageUse(
        EntityId stackId,
        EntityId workerId,
        CellId workerCell,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        if (!CanDirectUseProductionPackage(
            stackId,
            workerCell,
            out CellId workPosition))
        {
            return Result.Failure(ProductionErrors.OutputPackageNotUsable);
        }

        Result prepared = PrepareResidentsForDirectCommand(
            new[] { workerId.ToString() },
            tick);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        EntityId jobId = NextProductionEntityId(
            '8',
            ref _nextProductionPackageUseJobSequence);
        return _startProductionPackageUse!.Handle(
            new StartProductionPackageUseCommand(
                jobId,
                stackId,
                workerId,
                workPosition,
                ProductionPackageUsePriority,
                tick));
    }

    internal Result AdvanceProductionPackages(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (_productionRepository == null)
        {
            return Result.Success();
        }

        Dictionary<string, AgentViewModel> byId = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        foreach (JobSnapshot job in _jobRepository.Get().GetAll())
        {
            if (job.Definition is not ProductionPackageUseJobDefinition definition
                || !IsActive(job)
                || !job.AssignedAgentId.HasValue
                || !byId.TryGetValue(
                    job.AssignedAgentId.Value.ToString(),
                    out AgentViewModel? worker)
                || !At(worker, definition.WorkPosition))
            {
                continue;
            }

            Result advanced = AdvanceProductionPackageAtWorkPosition(
                job,
                definition,
                tick);
            if (advanced.IsFailure)
            {
                return advanced;
            }
        }

        return Result.Success();
    }

    private Result AdvanceProductionPackageAtWorkPosition(
        JobSnapshot job,
        ProductionPackageUseJobDefinition definition,
        long tick)
    {
        if (job.Stage == JobStageKind.TravelToTarget)
        {
            Result arrived = _advanceProductionPackageUse!.Handle(
                new AdvanceProductionPackageUseCommand(job.Id, tick));
            if (arrived.IsFailure)
            {
                return arrived;
            }
        }

        JobSnapshot? current = _jobRepository.Get().Get(job.Id);
        if (current?.Stage == JobStageKind.PerformWork)
        {
            if (tick % 2 != 0)
            {
                return Result.Success();
            }

            Result broken = _advanceProductionPackageUse!.Handle(
                new AdvanceProductionPackageUseCommand(job.Id, tick));
            if (broken.IsFailure)
            {
                return broken;
            }
        }

        current = _jobRepository.Get().Get(job.Id);
        if (current?.Stage != JobStageKind.Finalize)
        {
            return Result.Success();
        }

        ProductionOutputPackageSnapshot? package = _productionRepository!.Get()
            .GetOutputPackage(definition.PackageStackId);
        if (package == null || !package.IsClosed)
        {
            return CancelStaleProductionPackageUse(job.Id, tick);
        }

        EntityId[] outputs = Enumerable.Range(0, package.Manifest.Count)
            .Select(_ => NextProductionEntityId(
                '7',
                ref _nextProductionPackageUseOutputSequence))
            .ToArray();
        Result completed = _completeProductionPackageUse!.Handle(
            new CompleteProductionPackageUseCommand(job.Id, outputs, tick));
        _routePlans.Remove(job.Id);
        if (completed.IsSuccess)
        {
            return Result.Success();
        }

        return completed.Error == ProductionErrors.OutputPackageNotUsable
            ? CancelStaleProductionPackageUse(job.Id, tick)
            : completed;
    }

    private Result CancelStaleProductionPackageUse(EntityId jobId, long tick)
    {
        JobSnapshot? job = _jobRepository.Get().Get(jobId);
        if (job == null || job.IsTerminal)
        {
            return Result.Success();
        }

        Result cancelled = _cancelProductionPackageUse!.Handle(
            new CancelProductionPackageUseCommand(
                jobId,
                "production_package_stale_or_already_opened",
                tick));
        return cancelled.IsSuccess ? Result.Success() : cancelled;
    }

    private bool TryPlanProductionPackageMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (job.Definition is not ProductionPackageUseJobDefinition definition)
        {
            return false;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(
                start,
                definition.WorkPosition,
                navigation.NavigationVersion));
        if (!path.Succeeded
            || path.Path == null
            || !IsSupportedBarrelAttackPath(navigation, path.Path))
        {
            return true;
        }

        _routePlans[job.Id] = new TerrainWorkRoutePlan(
            job.Id,
            definition.TargetCell,
            definition.WorkPosition,
            path,
            candidateCount: 1);
        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : definition.WorkPosition;
        return true;
    }
}

}
