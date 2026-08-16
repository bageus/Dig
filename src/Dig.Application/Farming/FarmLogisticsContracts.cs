using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.World;

namespace Dig.Application.Farming
{

public interface IFarmLogisticsJobIdSource
{
    EntityId NextJobId();
    EntityId NextStackId();
}

public readonly struct FarmLogisticsSite
{
    public FarmLogisticsSite(EntityId farmId, CellId workCell, CellId outputCell)
    {
        if (farmId.IsEmpty) throw new ArgumentException("Farm id is required.", nameof(farmId));
        FarmId = farmId;
        WorkCell = workCell;
        OutputCell = outputCell;
    }

    public EntityId FarmId { get; }
    public CellId WorkCell { get; }
    public CellId OutputCell { get; }
}

public sealed class SynchronizeFarmOutputsCommand
    : ICommand<Result<FarmLogisticsSynchronizationReport>>
{
    public SynchronizeFarmOutputsCommand(
        IReadOnlyCollection<FarmLogisticsSite> sites,
        int priority,
        int maximumJobs,
        long tick)
    {
        Sites = sites ?? throw new ArgumentNullException(nameof(sites));
        if (maximumJobs <= 0) throw new ArgumentOutOfRangeException(nameof(maximumJobs));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        Priority = priority;
        MaximumJobs = maximumJobs;
        Tick = tick;
    }

    public IReadOnlyCollection<FarmLogisticsSite> Sites { get; }
    public int Priority { get; }
    public int MaximumJobs { get; }
    public long Tick { get; }
}

public sealed class SynchronizeFarmLogisticsCommand
    : ICommand<Result<FarmLogisticsSynchronizationReport>>
{
    public SynchronizeFarmLogisticsCommand(
        IReadOnlyCollection<CellId> reachableCells,
        int priority,
        int maximumJobs,
        long tick)
    {
        ReachableCells = reachableCells
            ?? throw new ArgumentNullException(nameof(reachableCells));
        if (maximumJobs <= 0) throw new ArgumentOutOfRangeException(nameof(maximumJobs));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        Priority = priority;
        MaximumJobs = maximumJobs;
        Tick = tick;
    }

    public IReadOnlyCollection<CellId> ReachableCells { get; }
    public int Priority { get; }
    public int MaximumJobs { get; }
    public long Tick { get; }
}

public sealed class CompleteFarmDeliveryCommand : ICommand<Result>
{
    public CompleteFarmDeliveryCommand(
        EntityId jobId,
        EntityId depositedStackId,
        long tick)
    {
        JobId = jobId;
        DepositedStackId = depositedStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId DepositedStackId { get; }
    public long Tick { get; }
}

public sealed class CompleteFarmOutputCommand : ICommand<Result>
{
    public CompleteFarmOutputCommand(EntityId jobId, EntityId depositedStackId, long tick)
    {
        JobId = jobId;
        DepositedStackId = depositedStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId DepositedStackId { get; }
    public long Tick { get; }
}

public readonly struct FarmLogisticsJobPlan
{
    public FarmLogisticsJobPlan(
        EntityId farmId,
        EntityId jobId,
        EntityId sourceStackId,
        FarmDeliveryKind kind,
        int quantity)
    {
        FarmId = farmId;
        JobId = jobId;
        SourceStackId = sourceStackId;
        Kind = kind;
        Quantity = quantity;
    }

    public EntityId FarmId { get; }
    public EntityId JobId { get; }
    public EntityId SourceStackId { get; }
    public FarmDeliveryKind Kind { get; }
    public int Quantity { get; }
}

public sealed class FarmLogisticsSynchronizationReport
{
    public FarmLogisticsSynchronizationReport(
        IReadOnlyCollection<FarmLogisticsJobPlan> created,
        int releasedReservations)
    {
        Created = created ?? throw new ArgumentNullException(nameof(created));
        ReleasedReservations = releasedReservations;
    }

    public IReadOnlyCollection<FarmLogisticsJobPlan> Created { get; }
    public int ReleasedReservations { get; }
}

public static class FarmLogisticsErrors
{
    public static readonly DomainError JobMismatch = new DomainError(
        "farm.logistics.job_mismatch",
        "The hauling job is not an active delivery for this farm.");

    public static readonly DomainError InvalidStage = new DomainError(
        "farm.logistics.invalid_stage",
        "The farm delivery is not ready to be deposited.");
}

}
