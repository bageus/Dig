using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;

namespace Dig.Application.WorldObjects
{

public interface IBarrelRepository
{
    BarrelState Get();
    void Save(BarrelState barrels);
}

public sealed class StartDirectBarrelAttackCommand
    : ICommand<Result<BarrelAttackStartedResult>>
{
    public StartDirectBarrelAttackCommand(
        EntityId jobId,
        EntityId barrelId,
        EntityId workerId,
        CellId workPosition,
        int priority,
        long tick)
    {
        if (jobId.IsEmpty || barrelId.IsEmpty || workerId.IsEmpty)
        {
            throw new ArgumentException("Job, barrel and worker ids are required.");
        }

        if (priority < 0 || priority > 1000 || tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        JobId = jobId;
        BarrelId = barrelId;
        WorkerId = workerId;
        WorkPosition = workPosition;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId BarrelId { get; }
    public EntityId WorkerId { get; }
    public CellId WorkPosition { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class BarrelAttackStartedResult
{
    public BarrelAttackStartedResult(
        EntityId jobId,
        EntityId barrelId,
        EntityId workerId,
        long barrelVersion,
        long contentsGeneration)
    {
        JobId = jobId;
        BarrelId = barrelId;
        WorkerId = workerId;
        BarrelVersion = barrelVersion;
        ContentsGeneration = contentsGeneration;
    }

    public EntityId JobId { get; }
    public EntityId BarrelId { get; }
    public EntityId WorkerId { get; }
    public long BarrelVersion { get; }
    public long ContentsGeneration { get; }
}

public sealed class ArriveAtBarrelCommand : ICommand<Result>
{
    public ArriveAtBarrelCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CompleteBarrelHitCommand : ICommand<Result>
{
    public CompleteBarrelHitCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CompleteBarrelDestructionCommand
    : ICommand<Result<BarrelDestructionResult>>
{
    public CompleteBarrelDestructionCommand(EntityId jobId, EntityId outputUnitId, long tick)
    {
        JobId = jobId;
        OutputUnitId = outputUnitId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId OutputUnitId { get; }
    public long Tick { get; }
}

public sealed class BarrelDestructionResult
{
    public BarrelDestructionResult(
        EntityId jobId,
        EntityId barrelId,
        EntityId outputUnitId,
        long contentsGeneration)
    {
        JobId = jobId;
        BarrelId = barrelId;
        OutputUnitId = outputUnitId;
        ContentsGeneration = contentsGeneration;
    }

    public EntityId JobId { get; }
    public EntityId BarrelId { get; }
    public EntityId OutputUnitId { get; }
    public long ContentsGeneration { get; }
}

public sealed class CancelBarrelAttackCommand : ICommand<Result>
{
    public CancelBarrelAttackCommand(EntityId jobId, string reasonCode, long tick)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reasonCode));
        }

        JobId = jobId;
        ReasonCode = reasonCode.Trim();
        Tick = tick;
    }

    public EntityId JobId { get; }
    public string ReasonCode { get; }
    public long Tick { get; }
}

public sealed class SettleBarrelAfterSupportLossCommand : ICommand<Result>
{
    public SettleBarrelAfterSupportLossCommand(
        EntityId barrelId,
        CellId landingCell,
        long tick)
    {
        BarrelId = barrelId;
        LandingCell = landingCell;
        Tick = tick;
    }

    public EntityId BarrelId { get; }
    public CellId LandingCell { get; }
    public long Tick { get; }
}

}