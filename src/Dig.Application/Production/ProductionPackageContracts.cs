using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Production;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Production
{

public sealed class CreateProductionOutputPackageCommand : ICommand<Result>
{
    public CreateProductionOutputPackageCommand(
        EntityId orderId,
        EntityId jobId,
        EntityId packageStackId,
        ItemLocation location,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        PackageStackId = packageStackId;
        Location = location;
        Tick = tick;
    }

    public EntityId OrderId { get; }
    public EntityId JobId { get; }
    public EntityId PackageStackId { get; }
    public ItemLocation Location { get; }
    public long Tick { get; }
}

public sealed class InterruptProductionOrderCommand : ICommand<Result>
{
    public InterruptProductionOrderCommand(
        EntityId orderId,
        EntityId jobId,
        string reason,
        long tick,
        CellId? recoveryCell = null)
    {
        OrderId = orderId;
        JobId = jobId;
        Reason = reason;
        Tick = tick;
        RecoveryCell = recoveryCell;
    }

    public EntityId OrderId { get; }
    public EntityId JobId { get; }
    public string Reason { get; }
    public long Tick { get; }
    public CellId? RecoveryCell { get; }
}

public sealed class StartProductionPackageUseCommand : ICommand<Result>
{
    public StartProductionPackageUseCommand(
        EntityId jobId,
        EntityId packageStackId,
        EntityId workerId,
        CellId workPosition,
        int priority,
        long tick)
    {
        JobId = jobId;
        PackageStackId = packageStackId;
        WorkerId = workerId;
        WorkPosition = workPosition;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId PackageStackId { get; }
    public EntityId WorkerId { get; }
    public CellId WorkPosition { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class AdvanceProductionPackageUseCommand : ICommand<Result>
{
    public AdvanceProductionPackageUseCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CompleteProductionPackageUseCommand : ICommand<Result>
{
    public CompleteProductionPackageUseCommand(
        EntityId jobId,
        IReadOnlyCollection<EntityId> outputStackIds,
        long tick)
    {
        JobId = jobId;
        OutputStackIds = outputStackIds
            ?? throw new ArgumentNullException(nameof(outputStackIds));
        Tick = tick;
    }

    public EntityId JobId { get; }
    public IReadOnlyCollection<EntityId> OutputStackIds { get; }
    public long Tick { get; }
}

public sealed class CancelProductionPackageUseCommand : ICommand<Result>
{
    public CancelProductionPackageUseCommand(EntityId jobId, string reason, long tick)
    {
        JobId = jobId;
        Reason = reason;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public string Reason { get; }
    public long Tick { get; }
}

}
