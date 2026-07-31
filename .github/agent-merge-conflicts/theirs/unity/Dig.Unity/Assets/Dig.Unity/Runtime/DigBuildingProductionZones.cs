using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Buildings;
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
    private const float ProductionWaitOffset = 0.28f;
    private readonly Dictionary<string, float> _productionWaitOffsets =
        new Dictionary<string, float>(StringComparer.Ordinal);

    private Result AdvanceProductionJob(
        JobSnapshot job,
        ProductionWorkJobDefinition production,
        AgentViewModel worker,
        long tick)
    {
        bool atWorkstation = At(worker, production.WorkPosition);
        if (job.Status == JobStatus.Claimed)
        {
            if (!atWorkstation)
            {
                return Result.Success();
            }

            Result begun = _beginProduction!.Handle(
                new BeginProductionWorkCommand(production.OrderId, job.Id, tick));
            if (begun.IsFailure)
            {
                return begun;
            }

            Result travelled = _advanceHandler.Handle(
                new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
            if (travelled.IsFailure)
            {
                return travelled;
            }
        }
        else if (job.Stage == JobStageKind.TravelToTarget)
        {
            if (!atWorkstation)
            {
                return Result.Success();
            }

            Result travelled = _advanceHandler.Handle(
                new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
            if (travelled.IsFailure)
            {
                return travelled;
            }
        }

        JobSnapshot? current = _jobRepository.Get().Get(job.Id);
        if (current?.Stage == JobStageKind.PerformWork)
        {
            if (!atWorkstation || tick % 2 != 0)
            {
                return Result.Success();
            }

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

        ProductionOrderSnapshot? order = _productionRepository!.Get().Get(
            production.OrderId);
        if (order == null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        int outputUnitCount = order.Recipe.Outputs.Sum(value => value.Quantity);
        Result<IReadOnlyList<CellId>> outputCells = ResolveProductionOutputCells(
            building,
            outputUnitCount);
        if (outputCells.IsFailure || !At(worker, outputCells.Value[0]))
        {
            return Result.Success();
        }

        EntityId[] outputs = Enumerable.Range(0, outputUnitCount)
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
                outputLocations: outputCells.Value
                    .Select(ItemLocation.InWorld)
                    .ToArray()));
        if (completed.IsSuccess)
        {
            _buildingProductionRoutes.Remove(job.Id);
            _productionWaitOffsets[worker.Id] = ProductionWaitOffset;
        }

        return completed;
    }

    internal bool TryPlanBuildingProductionMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (job.Definition is not ProductionWorkJobDefinition production)
        {
            return false;
        }

        EnsureBuildingProductionInitialized();
        CellId target = production.WorkPosition;
        if (job.Stage == JobStageKind.Finalize)
        {
            BuildingSnapshot? building = _buildingsRepository!.Get().Get(
                production.BuildingId);
            if (building == null)
            {
                return true;
            }

            ProductionOrderSnapshot? order = _productionRepository!.Get().Get(
                production.OrderId);
            if (order == null)
            {
                return true;
            }

            Result<IReadOnlyList<CellId>> outputCells = ResolveProductionOutputCells(
                building,
                order.Recipe.Outputs.Sum(value => value.Quantity));
            if (outputCells.IsFailure)
            {
                return true;
            }

            target = outputCells.Value[0];
        }

        return PlanBuildingProductionRoute(
            _buildingProductionRoutes,
            job,
            agent,
            target,
            navigation,
            movement);
    }

    internal IReadOnlyDictionary<string, float> LoadProductionWaitOffsets()
    {
        HashSet<string> active = _jobRepository.Get().GetAll()
            .Where(IsActive)
            .Where(value => value.AssignedAgentId.HasValue)
            .Select(value => value.AssignedAgentId!.Value.ToString())
            .ToHashSet(StringComparer.Ordinal);
        string[] cleared = _productionWaitOffsets.Keys
            .Where(value => active.Contains(value)
                || (_isManualMovementActive?.Invoke(value) ?? false))
            .ToArray();
        for (int index = 0; index < cleared.Length; index++)
        {
            _productionWaitOffsets.Remove(cleared[index]);
        }

        return _productionWaitOffsets.ToDictionary(
            value => value.Key,
            value => value.Value,
            StringComparer.Ordinal);
    }

    private Result<CellId> ResolveProductionOutputCell(BuildingSnapshot building)
    {
        return ProductionOutputPlacement.Resolve(
            building,
            _worldSession.LoadSnapshot(),
            _buildingsRepository!.Get().GetOccupiedCells(),
            _buildingInventoryRepository!.Get().CreateSnapshot().Stacks);
    }

    private Result<IReadOnlyList<CellId>> ResolveProductionOutputCells(
        BuildingSnapshot building,
        int requiredCount)
    {
        return ProductionOutputPlacement.ResolveMany(
            building,
            _worldSession.LoadSnapshot(),
            _buildingsRepository!.Get().GetOccupiedCells(),
            _buildingInventoryRepository!.Get().CreateSnapshot().Stacks,
            requiredCount);
    }
}

}
