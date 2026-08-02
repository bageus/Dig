using System.Collections.Generic;
using Dig.Application.Production;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Result AdvanceProductionMaterialStep(
        JobSnapshot job,
        ProductionWorkJobDefinition production,
        ProductionOrderSnapshot order,
        AgentViewModel worker,
        long tick)
    {
        if (!TryResolveCurrentProductionMaterialStep(
                order,
                out ProductionMaterialStepSnapshot step)
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        EntityId workerId = job.AssignedAgentId.Value;
        switch (step.Phase)
        {
            case ProductionMaterialStepPhase.AwaitingMaterial:
                if (!HasCarriedProductionMaterial(
                        production.OrderId,
                        workerId,
                        step.ItemId))
                {
                    BuildingSnapshot? building = _buildingsRepository!.Get().Get(
                        production.BuildingId);
                    if (building == null)
                    {
                        return Result.Failure(ProductionErrors.WorkstationMismatch);
                    }

                    CellId stockCell = ResolveBuildingInternalStockCell(building);
                    if (!At(worker, stockCell))
                    {
                        return Result.Success();
                    }

                    EntityId transitStackId = NextProductionEntityId(
                        'a',
                        ref _nextProductionMaterialTransitSequence);
                    return _acquireProductionMaterial!.Handle(
                        new AcquireProductionMaterialCommand(
                            production.OrderId,
                            job.Id,
                            transitStackId,
                            tick));
                }

                if (!At(worker, production.WorkPosition))
                {
                    return Result.Success();
                }

                return _stageProductionMaterial!.Handle(
                    new StageProductionMaterialCommand(
                        production.OrderId,
                        job.Id,
                        tick));

            case ProductionMaterialStepPhase.StagedOnWorkbench:
            case ProductionMaterialStepPhase.Processing:
                if (!At(worker, production.WorkPosition))
                {
                    return Result.Success();
                }

                return _applyProductionWork!.Handle(
                    new ApplyProductionWorkCommand(
                        production.OrderId,
                        job.Id,
                        baseWork: 1,
                        conditionEfficiencyBasisPoints: 10_000,
                        tick));

            case ProductionMaterialStepPhase.ProcessedAwaitingPackage:
                Result<CellId> packageCell = ResolveProductionPackageCell(
                    production.OrderId);
                if (packageCell.IsFailure || !At(worker, packageCell.Value))
                {
                    return Result.Success();
                }

                ProductionOutputPackageSnapshot? package = _productionRepository!.Get()
                    .GetOutputPackageForOrder(production.OrderId);
                if (package == null)
                {
                    return Result.Failure(ProductionErrors.OutputPackageNotFound);
                }

                return _depositProductionMaterial!.Handle(
                    new DepositProductionMaterialCommand(
                        production.OrderId,
                        job.Id,
                        package.StackId,
                        tick));

            default:
                return Result.Failure(ProductionErrors.InvalidStatus);
        }
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
        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.TravelToTarget)
        {
            Result<CellId> packageTarget = ResolveProductionPackagePlacementTarget(
                production);
            if (packageTarget.IsFailure)
            {
                return true;
            }

            target = packageTarget.Value;
        }
        else if (job.Stage == JobStageKind.PerformWork)
        {
            ProductionOrderSnapshot? order = _productionRepository!.Get().Get(
                production.OrderId);
            if (order != null
                && order.Recipe.UsesMaterialSteps
                && TryResolveCurrentProductionMaterialStep(
                    order,
                    out ProductionMaterialStepSnapshot step))
            {
                if (step.Phase == ProductionMaterialStepPhase.AwaitingMaterial
                    && job.AssignedAgentId.HasValue
                    && !HasCarriedProductionMaterial(
                        production.OrderId,
                        job.AssignedAgentId.Value,
                        step.ItemId))
                {
                    BuildingSnapshot? building = _buildingsRepository!.Get().Get(
                        production.BuildingId);
                    if (building == null)
                    {
                        return true;
                    }

                    target = ResolveBuildingInternalStockCell(building);
                }
                else if (step.Phase
                    == ProductionMaterialStepPhase.ProcessedAwaitingPackage)
                {
                    Result<CellId> packageCell = ResolveProductionPackageCell(
                        production.OrderId);
                    if (packageCell.IsFailure)
                    {
                        return true;
                    }

                    target = packageCell.Value;
                }
            }
        }
        else if (job.Stage == JobStageKind.Finalize)
        {
            Result<CellId> outputCell = ResolveProductionPackageCell(
                production.OrderId);
            if (outputCell.IsFailure)
            {
                return true;
            }

            target = outputCell.Value;
        }

        return PlanBuildingProductionRoute(
            _buildingProductionRoutes,
            job,
            agent,
            target,
            navigation,
            movement);
    }

    private Result<CellId> ResolveProductionPackagePlacementTarget(
        ProductionWorkJobDefinition production)
    {
        ProductionOutputPackageSnapshot? package = _productionRepository!.Get()
            .GetOutputPackageForOrder(production.OrderId);
        if (package != null)
        {
            return ResolveProductionPackageCell(production.OrderId);
        }

        BuildingSnapshot? building = _buildingsRepository!.Get().Get(
            production.BuildingId);
        return building == null
            ? Result<CellId>.Failure(ProductionErrors.WorkstationMismatch)
            : ResolveProductionOutputCell(building);
    }


}

}
