using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
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
            Result advanced = _advanceHandler.Handle(
                new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
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
                pair.Value.Target.Z,
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
            || _createDeferredBuildingSupply == null
            || _resolveDeferredBuildingSupply == null
            || _cancelDeferredBuildingSupply == null
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
