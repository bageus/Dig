using System;
using Dig.Domain.Core;

namespace Dig.Domain.Production
{

public sealed partial class ProductionState
{
    public Result<ProductionMaterialWorkResult> PreviewMaterialWork(
        EntityId orderId,
        long elapsedTicks)
    {
        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result<ProductionMaterialWorkResult>.Failure(
                ProductionErrors.OrderNotFound);
        }

        if (order.Status != ProductionOrderStatus.InProgress
            || !order.Recipe.UsesMaterialSteps)
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
        if (order is null)
        {
            return Result<ProductionMaterialWorkResult>.Failure(
                ProductionErrors.OrderNotFound);
        }

        if (order.Status != ProductionOrderStatus.InProgress
            || !order.Recipe.UsesMaterialSteps)
        {
            return Result<ProductionMaterialWorkResult>.Failure(
                ProductionErrors.InvalidStatus);
        }

        ProductionOrderStatus previous = order.Status;
        ProductionMaterialWorkResult result = order.AddMaterialWork(elapsedTicks);
        Raise(new ProductionWorkApplied(
            tick,
            order.Id,
            order.BuildingId,
            checked((int)Math.Min(int.MaxValue, elapsedTicks)),
            order.CompletedWork,
            order.Recipe.MaterialSteps.Count));
        if (previous != order.Status)
        {
            RaiseStatusChanged(tick, order, previous, null);
        }

        return Result<ProductionMaterialWorkResult>.Success(result);
    }


}

}
