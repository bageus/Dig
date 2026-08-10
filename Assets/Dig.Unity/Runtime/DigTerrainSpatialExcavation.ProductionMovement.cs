using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private bool TryResolveProductionWorkPose(
        JobSnapshot job,
        ProductionWorkJobDefinition production,
        out SurfacePose pose)
    {
        if (!TryResolveProductionWorkCell(job, production, out CellId target))
        {
            pose = default;
            return false;
        }

        pose = WorkSurfacePositioning.Resolve(target, target);
        return true;
    }

    private bool TryResolveProductionWorkCell(
        JobSnapshot job,
        ProductionWorkJobDefinition production,
        out CellId target)
    {
        target = production.WorkPosition;
        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.TravelToTarget)
        {
            Result<CellId> packageTarget = ResolveProductionPackagePlacementTarget(
                production);
            if (packageTarget.IsFailure)
            {
                return false;
            }

            target = packageTarget.Value;
        }
        else if (job.Stage == JobStageKind.PerformWork)
        {
            ProductionOrderSnapshot? order = _productionRepository?.Get().Get(
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
                    BuildingSnapshot? building = _buildingsRepository?.Get().Get(
                        production.BuildingId);
                    if (building == null)
                    {
                        return false;
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
                        return false;
                    }

                    target = packageCell.Value;
                }
            }
        }
        else if (job.Stage == JobStageKind.Finalize)
        {
            Result<CellId> packageCell = ResolveProductionPackageCell(
                production.OrderId);
            if (packageCell.IsFailure)
            {
                return false;
            }

            target = packageCell.Value;
        }

        return true;
    }

    private bool TryResolveBuildingSupplyPose(
        JobSnapshot job,
        BuildingSupplyJobDefinition supply,
        out SurfacePose pose)
    {
        TryResolveBuildingSupplyWorkCell(job, supply, out CellId target);
        pose = WorkSurfacePositioning.Resolve(target, target);
        return true;
    }

    private bool TryResolveBuildingSupplyWorkCell(
        JobSnapshot job,
        BuildingSupplyJobDefinition supply,
        out CellId target)
    {
        target = supply.WorkPosition;
        if (job.Stage == JobStageKind.AcquireItem)
        {
            ItemReservationAllocation? pending = FindPendingSupplyAllocation(
                job.Id,
                supply);
            if (pending.HasValue)
            {
                ItemStackSnapshot? source = _buildingInventoryRepository?.Get()
                    .GetStack(pending.Value.StackId);
                if (source?.Location.Kind == ItemLocationKind.World
                    && source.Location.HasCell)
                {
                    target = source.Location.CellId;
                }
            }
        }

        return true;
    }
}

}
