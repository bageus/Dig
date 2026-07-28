using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.Technology;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Production;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result AdvanceProductionJob(
        JobSnapshot job,
        ProductionWorkJobDefinition production,
        AgentViewModel worker,
        long tick)
    {
        if (!At(worker, production.WorkPosition))
        {
            return Result.Success();
        }

        if (job.Status == JobStatus.Claimed)
        {
            Result begun = _beginProduction!.Handle(
                new BeginProductionWorkCommand(production.OrderId, job.Id, tick));
            if (begun.IsFailure)
            {
                return begun;
            }

            Result travelled = _advanceHandler.Handle(new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
            if (travelled.IsFailure)
            {
                return travelled;
            }
        }
        else if (job.Stage == JobStageKind.TravelToTarget)
        {
            Result travelled = _advanceHandler.Handle(new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
            if (travelled.IsFailure)
            {
                return travelled;
            }
        }

        JobSnapshot? current = _jobRepository.Get().Get(job.Id);
        if (current?.Stage == JobStageKind.PerformWork && tick % 2 == 0)
        {
            Result worked = _applyProductionWork!.Handle(
                new ApplyProductionWorkCommand(
                    production.OrderId,
                    job.Id,
                    baseWork: 1,
                    conditionEfficiencyBasisPoints: 10_000,
                    tick));
            if (worked.IsFailure)
            {
                return worked;
            }

            current = _jobRepository.Get().Get(job.Id);
        }

        if (current?.Stage != JobStageKind.Finalize)
        {
            return Result.Success();
        }

        BuildingSnapshot? building = _buildingsRepository!.Get().Get(
            production.BuildingId);
        if (building == null)
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        Result<CellId> outputCell = ProductionOutputPlacement.Resolve(
            building,
            _worldSession.LoadSnapshot(),
            _buildingsRepository.Get().GetOccupiedCells(),
            _buildingInventoryRepository!.Get().CreateSnapshot().Stacks);
        if (outputCell.IsFailure)
        {
            return Result.Success();
        }

        ProductionOrderSnapshot? order = _productionRepository!.Get().Get(
            production.OrderId);
        if (order == null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        EntityId[] outputs = order.Recipe.Outputs
            .Select(_ => NextProductionEntityId(
                'a',
                ref _nextProductionOutputSequence))
            .ToArray();
        Result completed = _completeProduction!.Handle(
            new CompleteProductionOrderCommand(
                production.OrderId,
                job.Id,
                outputs,
                tick,
                ItemLocation.InWorld(outputCell.Value)));
        if (completed.IsSuccess)
        {
            _buildingProductionRoutes.Remove(job.Id);
        }

        return completed;
    }

    private Result AdvanceSupplyJob(
        JobSnapshot job,
        BuildingSupplyJobDefinition supply,
        AgentViewModel worker,
        long tick)
    {
        Result checkedStock = AdvanceSupplyWorkstationCheck(
            job,
            supply,
            worker,
            tick,
            out JobSnapshot currentJob);
        if (checkedStock.IsFailure)
        {
            return checkedStock;
        }

        job = currentJob;

        if (job.Stage == JobStageKind.AcquireItem)
        {
            ItemReservationAllocation? pending = FindPendingSupplyAllocation(job.Id, supply);
            if (!pending.HasValue)
            {
                return Result.Success();
            }

            ItemStackSnapshot? source = _buildingInventoryRepository!.Get().GetStack(
                pending.Value.StackId);
            if (source?.Location.Kind != ItemLocationKind.World
                || !source.Location.HasCell)
            {
                return _cancelBuildingSupply!.Handle(
                    new CancelBuildingSupplyCommand(
                        job.Id,
                        "source_unavailable",
                        tick));
            }

            if (!At(worker, source.Location.CellId))
            {
                return Result.Success();
            }

            return _acquireBuildingSupplySource!.Handle(
                new AcquireBuildingSupplySourceCommand(
                    job.Id,
                    source.StackId,
                    tick));
        }

        if (!At(worker, supply.WorkPosition))
        {
            return Result.Success();
        }

        if (job.Stage == JobStageKind.TravelToDestination)
        {
            Result advanced = _advanceHandler.Handle(new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
            if (advanced.IsFailure)
            {
                return advanced;
            }
        }

        JobSnapshot? current = _jobRepository.Get().Get(job.Id);
        if (current?.Stage == JobStageKind.DepositItem)
        {
            Result deposited = _depositBuildingSupply!.Handle(
                new DepositBuildingSupplyCommand(job.Id, tick));
            if (deposited.IsSuccess)
            {
                _buildingSupplyRoutes.Remove(job.Id);
            }

            return deposited;
        }

        return Result.Success();
    }

    private bool PlanBuildingProductionRoute(
        IDictionary<EntityId, BuildingProductionRoutePlan> routes,
        JobSnapshot job,
        AgentViewModel agent,
        CellId target,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = _productionPathfinder!.FindPath(
            navigation,
            new PathRequest(start, target, navigation.NavigationVersion));
        routes[job.Id] = new BuildingProductionRoutePlan(target, path);
        if (path.Succeeded)
        {
            movement[agent.Id] = path.Path!.Cells.Count > 1
                ? path.Path.Cells[1]
                : target;
        }

        return true;
    }

    private IReadOnlyList<RouteViewModel> PresentBuildingProductionRoutes(
        IReadOnlyDictionary<EntityId, BuildingProductionRoutePlan> routes,
        string label)
    {
        List<RouteViewModel> values = new List<RouteViewModel>();
        foreach (KeyValuePair<EntityId, BuildingProductionRoutePlan> pair in routes
            .OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            JobSnapshot? job = _jobRepository.Get().Get(pair.Key);
            if (job == null || !job.AssignedAgentId.HasValue)
            {
                continue;
            }

            PathResult path = pair.Value.Path;
            RouteCellViewModel[] cells = path.Path == null
                ? Array.Empty<RouteCellViewModel>()
                : path.Path.Cells
                    .Select(cell => new RouteCellViewModel(cell.X, cell.Y, cell.Z))
                    .ToArray();
            values.Add(new RouteViewModel(
                pair.Key.ToString(),
                job.AssignedAgentId.Value.ToString(),
                pair.Value.Target.X,
                pair.Value.Target.Y,
                path.Succeeded,
                label + ": " + path.Diagnostics.Detail,
                path.Path?.TotalCost ?? 0,
                path.Diagnostics.SnapshotVersion,
                cells));
        }

        return values;
    }

    private CellId[] GetProductionRevealedCells()
    {
        return _worldSession.LoadSnapshot().Chunks
            .SelectMany(value => value.Cells)
            .Where(value => value.State.IsExplored)
            .Select(value => value.Id)
            .OrderBy(value => value)
            .ToArray();
    }

    private IReadOnlyCollection<CellId> GetProductionReachableCells()
    {
        return _worldSession.LoadSnapshot().Chunks
            .SelectMany(value => value.Cells)
            .Where(value => value.State.IsExplored && !value.IsSolid)
            .Select(value => value.Id)
            .OrderBy(value => value)
            .ToArray();
    }

    private static int Distance(AgentViewModel agent, CellId target)
    {
        return Math.Abs(agent.CellX - target.X)
            + Math.Abs(agent.CellY - target.Y)
            + Math.Abs(agent.CellZ - target.Z);
    }

    private static bool At(AgentViewModel agent, CellId cell)
    {
        return agent.CellX == cell.X
            && agent.CellY == cell.Y
            && agent.CellZ == cell.Z;
    }

    private static EntityId NextProductionEntityId(char prefix, ref long sequence)
    {
        sequence = checked(sequence + 1);
        return DemoId(prefix, checked(10_000_000L + sequence));
    }

    private void EnsureBuildingProductionInitialized()
    {
        if (_productionContent == null
            || _productionRepository == null
            || _buildingSupplyRepository == null
            || _buildingProductionPresenter == null
            || _enqueueProduction == null
            || _prepareProduction == null
            || _beginProduction == null
            || _applyProductionWork == null
            || _completeProduction == null
            || _cancelProduction == null
            || _createBuildingSupply == null
            || _acquireBuildingSupplySource == null
            || _depositBuildingSupply == null
            || _setBuildingStockDelivery == null
            || _productionCandidates == null
            || _productionAssignment == null
            || _productionPathfinder == null)
        {
            throw new InvalidOperationException(
                "Building production execution is not initialized.");
        }
    }

    private sealed class BuildingProductionRoutePlan
    {
        internal BuildingProductionRoutePlan(CellId target, PathResult path)
        {
            Target = target;
            Path = path;
        }

        internal CellId Target { get; }
        internal PathResult Path { get; }
    }
}

}
