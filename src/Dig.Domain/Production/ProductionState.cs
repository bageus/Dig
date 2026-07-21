using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public sealed class ProductionState : AggregateRoot
{
    private readonly Dictionary<EntityId, ProductionOrderState> _orders =
        new Dictionary<EntityId, ProductionOrderState>();
    private long _nextSequence;

    public Result Enqueue(
        EntityId orderId,
        RecipeDefinition recipe,
        EntityId buildingId,
        long tick)
    {
        ValidateTick(tick);
        if (orderId.IsEmpty || buildingId.IsEmpty)
        {
            throw new ArgumentException("Order and building ids cannot be empty.");
        }

        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        if (_orders.ContainsKey(orderId))
        {
            return Result.Failure(ProductionErrors.OrderAlreadyExists);
        }

        _orders.Add(
            orderId,
            new ProductionOrderState(orderId, recipe, buildingId, _nextSequence++));
        return Result.Success();
    }

    public Result ReserveInputs(
        EntityId orderId,
        IReadOnlyCollection<ItemReservationAllocation> allocations,
        long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        if (order.Status != ProductionOrderStatus.Queued)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        if (!IsNextOrder(order))
        {
            return Result.Failure(ProductionErrors.QueueBlocked);
        }

        ProductionOrderStatus previous = order.Status;
        order.ReserveInputs(allocations);
        RaiseStatusChanged(tick, order, previous, null);
        return Result.Success();
    }

    public Result Start(EntityId orderId, long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        if (order.Status != ProductionOrderStatus.InputsReserved)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionOrderStatus previous = order.Status;
        order.Start();
        RaiseStatusChanged(tick, order, previous, null);
        return Result.Success();
    }

    public Result AddWork(EntityId orderId, int effectiveWork, long tick)
    {
        ValidateTick(tick);
        if (effectiveWork <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveWork));
        }

        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        if (order.Status != ProductionOrderStatus.InProgress)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionOrderStatus previous = order.Status;
        order.AddWork(effectiveWork);
        Raise(new ProductionWorkApplied(
            tick,
            order.Id,
            order.BuildingId,
            effectiveWork,
            order.CompletedWork,
            order.Recipe.RequiredWork));
        if (previous != order.Status)
        {
            RaiseStatusChanged(tick, order, previous, null);
        }

        return Result.Success();
    }

    public Result Complete(EntityId orderId, long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        if (order.Status != ProductionOrderStatus.ReadyToComplete)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionOrderStatus previous = order.Status;
        order.Complete();
        RaiseStatusChanged(tick, order, previous, null);
        return Result.Success();
    }

    public Result Cancel(EntityId orderId, string reason, long tick)
    {
        return End(orderId, reason, tick, failed: false);
    }

    public Result Fail(EntityId orderId, string reason, long tick)
    {
        return End(orderId, reason, tick, failed: true);
    }

    public ProductionOrderSnapshot? Get(EntityId orderId)
    {
        return Find(orderId)?.CreateSnapshot();
    }

    public ProductionOrderSnapshot? GetNextQueued(EntityId buildingId)
    {
        return _orders.Values
            .Where(order => order.BuildingId == buildingId
                && order.Status == ProductionOrderStatus.Queued)
            .OrderBy(order => order.Sequence)
            .Select(order => order.CreateSnapshot())
            .FirstOrDefault();
    }

    public IReadOnlyList<ProductionOrderSnapshot> GetAll()
    {
        return new ReadOnlyCollection<ProductionOrderSnapshot>(_orders.Values
            .OrderBy(order => order.Sequence)
            .Select(order => order.CreateSnapshot())
            .ToArray());
    }

    private Result End(EntityId orderId, string reason, long tick, bool failed)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Order end reason is required.", nameof(reason));
        }

        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        if (order.CreateSnapshot().IsTerminal)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        ProductionOrderStatus previous = order.Status;
        if (failed)
        {
            order.Fail(reason.Trim());
        }
        else
        {
            order.Cancel(reason.Trim());
        }

        RaiseStatusChanged(tick, order, previous, reason.Trim());
        return Result.Success();
    }

    private bool IsNextOrder(ProductionOrderState candidate)
    {
        bool activeExists = _orders.Values.Any(order => order.BuildingId == candidate.BuildingId
            && order.Id != candidate.Id
            && order.Status is ProductionOrderStatus.InputsReserved
                or ProductionOrderStatus.InProgress
                or ProductionOrderStatus.ReadyToComplete);
        if (activeExists)
        {
            return false;
        }

        long firstSequence = _orders.Values
            .Where(order => order.BuildingId == candidate.BuildingId
                && order.Status == ProductionOrderStatus.Queued)
            .Min(order => order.Sequence);
        return candidate.Sequence == firstSequence;
    }

    private ProductionOrderState? Find(EntityId orderId)
    {
        if (orderId.IsEmpty)
        {
            throw new ArgumentException("Order id cannot be empty.", nameof(orderId));
        }

        return _orders.TryGetValue(orderId, out ProductionOrderState? order)
            ? order
            : null;
    }

    private void RaiseStatusChanged(
        long tick,
        ProductionOrderState order,
        ProductionOrderStatus previous,
        string? reason)
    {
        Raise(new ProductionOrderStatusChanged(
            tick,
            order.Id,
            order.BuildingId,
            previous,
            order.Status,
            reason));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
}
