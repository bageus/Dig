using System;
using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Domain.Production
{

public sealed partial class ProductionState
{
    public Result StageMaterial(EntityId orderId, long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (!CanMutateMaterial(order))
        {
            return Result.Failure(order is null
                ? ProductionErrors.OrderNotFound
                : ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot previous = order!.GetCurrentMaterialStep();
        if (previous.Phase != ProductionMaterialStepPhase.AwaitingMaterial)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        order.StageCurrentMaterial();
        RaiseMaterialPhaseChanged(tick, order, previous);
        return Result.Success();
    }

    public Result<ProductionMaterialWorkResult> PreviewMaterialWork(
        EntityId orderId,
        long elapsedTicks)
    {
        ProductionOrderState? order = Find(orderId);
        if (!CanMutateMaterial(order))
        {
            return Result<ProductionMaterialWorkResult>.Failure(order is null
                ? ProductionErrors.OrderNotFound
                : ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot current = order!.GetCurrentMaterialStep();
        if (current.Phase is not ProductionMaterialStepPhase.StagedOnWorkbench
            and not ProductionMaterialStepPhase.Processing)
        {
            return Result<ProductionMaterialWorkResult>.Failure(
                ProductionErrors.InvalidStatus);
        }

        return Result<ProductionMaterialWorkResult>.Success(
            order.PreviewMaterialWork(elapsedTicks));
    }

    public Result<ProductionMaterialWorkResult> AddMaterialWork(
        EntityId orderId,
        long elapsedTicks,
        long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (!CanMutateMaterial(order))
        {
            return Result<ProductionMaterialWorkResult>.Failure(order is null
                ? ProductionErrors.OrderNotFound
                : ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot previous = order!.GetCurrentMaterialStep();
        if (previous.Phase is not ProductionMaterialStepPhase.StagedOnWorkbench
            and not ProductionMaterialStepPhase.Processing)
        {
            return Result<ProductionMaterialWorkResult>.Failure(
                ProductionErrors.InvalidStatus);
        }

        ProductionMaterialWorkResult result = order.AddMaterialWork(elapsedTicks);
        Raise(new ProductionWorkApplied(
            tick,
            order.Id,
            order.BuildingId,
            checked((int)Math.Min(int.MaxValue, result.AppliedTicks)),
            order.CompletedWork,
            order.Recipe.MaterialSteps.Count));
        ProductionMaterialStepSnapshot current = order.GetCurrentMaterialStep();
        if (current.Phase != previous.Phase)
        {
            RaiseMaterialPhaseChanged(tick, order, previous);
        }

        return Result<ProductionMaterialWorkResult>.Success(result);
    }

    public Result DepositProcessedMaterial(EntityId orderId, long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (!CanMutateMaterial(order))
        {
            return Result.Failure(order is null
                ? ProductionErrors.OrderNotFound
                : ProductionErrors.InvalidStatus);
        }

        ProductionMaterialStepSnapshot previous = order!.GetCurrentMaterialStep();
        if (previous.Phase
            != ProductionMaterialStepPhase.ProcessedAwaitingPackage)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionOrderStatus previousStatus = order.Status;
        order.DepositProcessedMaterial();
        Raise(new ProductionMaterialStepPhaseChanged(
            tick,
            order.Id,
            order.BuildingId,
            previous.Index,
            previous.ItemId,
            previous.Phase,
            ProductionMaterialStepPhase.Deposited));
        if (previousStatus != order.Status)
        {
            RaiseStatusChanged(tick, order, previousStatus, null);
        }

        return Result.Success();
    }

    public Result RestoreMaterialProgress(
        EntityId orderId,
        IReadOnlyCollection<ProductionMaterialStepSnapshot> materialSteps,
        long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (!CanMutateMaterial(order))
        {
            return Result.Failure(order is null
                ? ProductionErrors.OrderNotFound
                : ProductionErrors.InvalidStatus);
        }

        order!.RestoreMaterialProgress(materialSteps);
        return Result.Success();
    }

    private static bool CanMutateMaterial(ProductionOrderState? order)
    {
        return order != null
            && order.Status == ProductionOrderStatus.InProgress
            && order.Recipe.UsesMaterialSteps;
    }

    private void RaiseMaterialPhaseChanged(
        long tick,
        ProductionOrderState order,
        ProductionMaterialStepSnapshot previous)
    {
        ProductionMaterialStepSnapshot current = order.GetCurrentMaterialStep();
        Raise(new ProductionMaterialStepPhaseChanged(
            tick,
            order.Id,
            order.BuildingId,
            previous.Index,
            previous.ItemId,
            previous.Phase,
            current.Phase));
    }
}

}
