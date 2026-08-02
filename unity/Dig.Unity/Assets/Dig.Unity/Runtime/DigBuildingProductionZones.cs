using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
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
        if (job.Status == JobStatus.Claimed)
        {
            Result<CellId> packageTarget = ResolveProductionPackagePlacementTarget(
                production);
            if (packageTarget.IsFailure || !At(worker, packageTarget.Value))
            {
                return Result.Success();
            }

            Result packageReady = EnsureProductionOutputPackage(production, tick);
            if (packageReady.IsFailure)
            {
                return packageReady;
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
            Result<CellId> packageTarget = ResolveProductionPackagePlacementTarget(
                production);
            if (packageTarget.IsFailure || !At(worker, packageTarget.Value))
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
            ProductionOrderSnapshot? activeOrder = _productionRepository!.Get().Get(
                production.OrderId);
            if (activeOrder == null)
            {
                return Result.Failure(ProductionErrors.OrderNotFound);
            }

            if (activeOrder.Recipe.UsesMaterialSteps)
            {
                Result materialStep = AdvanceProductionMaterialStep(
                    current,
                    production,
                    activeOrder,
                    worker,
                    tick);
                if (materialStep.IsFailure)
                {
                    return materialStep;
                }

                current = _jobRepository.Get().Get(job.Id);
                if (current?.Stage != JobStageKind.Finalize)
                {
                    return Result.Success();
                }
            }
            else
            {
                if (!At(worker, production.WorkPosition))
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
        }

        if (current?.Stage == JobStageKind.TravelToDestination)
        {
            if (!At(worker, production.WorkPosition))
            {
                return Result.Success();
            }

            Result returned = _advanceHandler.Handle(
                new Dig.Application.Jobs.AdvanceJobCommand(job.Id, tick));
            if (returned.IsSuccess)
            {
                _buildingProductionRoutes.Remove(job.Id);
                _productionWaitOffsets[worker.Id] = ProductionWaitOffset;
                BuildingSupplyState supply = _buildingSupplyRepository!.Get();
                Result turn = supply.SetOperationTurn(
                    production.BuildingId,
                    BuildingOperationTurn.Supply,
                    tick);
                if (turn.IsFailure)
                {
                    return turn;
                }

                _buildingSupplyRepository.Save(supply);
            }

            return returned;
        }

        if (current?.Stage != JobStageKind.Finalize)
        {
            return Result.Success();
        }

        Result<CellId> outputCell = ResolveProductionPackageCell(production.OrderId);
        if (outputCell.IsFailure || !At(worker, outputCell.Value))
        {
            return Result.Success();
        }

        ProductionOrderSnapshot? order = _productionRepository!.Get().Get(
            production.OrderId);
        if (order == null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        ProductionOutputPackageSnapshot package = _productionRepository.Get()
            .GetOutputPackageForOrder(production.OrderId)
            ?? throw new InvalidOperationException(
                "A finalizing production order must retain its output package.");
        ProductionOutputPackageKind outputKind = ResolveProductionOutputKind(order);
        EntityId[] outputs = outputKind == ProductionOutputPackageKind.Building
            ? new[] { package.StackId }
            : Array.Empty<EntityId>();
        Result completed = _completeProduction!.Handle(
            new CompleteProductionOrderCommand(
                production.OrderId,
                job.Id,
                outputs,
                tick,
                ItemLocation.InWorld(outputCell.Value),
                package.StackId));
        return completed;
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


    private Result EnsureProductionOutputPackage(
        ProductionWorkJobDefinition production,
        long tick)
    {
        ProductionOutputPackageSnapshot? existing = _productionRepository!.Get()
            .GetOutputPackageForOrder(production.OrderId);
        if (existing != null)
        {
            return Result.Success();
        }

        BuildingSnapshot? building = _buildingsRepository!.Get().Get(
            production.BuildingId);
        if (building == null)
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        Result<CellId> outputCell = ResolveProductionOutputCell(building);
        if (outputCell.IsFailure)
        {
            return Result.Failure(outputCell.Error!);
        }

        EntityId packageId = NextProductionEntityId(
            '9',
            ref _nextProductionPackageSequence);
        return _createProductionPackage!.Handle(
            new CreateProductionOutputPackageCommand(
                production.OrderId,
                production.Id,
                packageId,
                ItemLocation.InWorld(outputCell.Value),
                tick));
    }

    private Result<CellId> ResolveProductionPackageCell(EntityId orderId)
    {
        ProductionOutputPackageSnapshot? package = _productionRepository!.Get()
            .GetOutputPackageForOrder(orderId);
        ItemStackSnapshot? stack = package == null
            ? null
            : _buildingInventoryRepository!.Get().GetStack(package.StackId);
        return stack?.Location.Kind == ItemLocationKind.World
            && stack.Location.HasCell
                ? Result<CellId>.Success(stack.Location.CellId)
                : Result<CellId>.Failure(ProductionErrors.OutputPackageNotFound);
    }

    private ProductionOutputPackageKind ResolveProductionOutputKind(
        ProductionOrderSnapshot order)
    {
        ProductionOutputPackageKind[] kinds = order.Recipe.Outputs
            .Select(value => ProductionPackageContent.ResolveKind(
                _buildingInventoryRepository!.Get().Catalog.Get(value.ItemId)))
            .Distinct()
            .ToArray();
        return kinds.Length == 1 ? kinds[0] : ProductionOutputPackageKind.Tool;
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
