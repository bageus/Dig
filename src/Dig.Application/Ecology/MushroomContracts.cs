using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public interface IMushroomRepository
{
    MushroomState Get();
    void Save(MushroomState mushrooms);
}

public interface IMushroomSwingRandom
{
    int SelectRequiredSwings(EntityId siteId, EntityId workerId, int minimum, int maximum);
}

public sealed class AdvanceMushroomGrowthCommand : ICommand<Result>
{
    public AdvanceMushroomGrowthCommand(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
    }

    public long Tick { get; }
}

public sealed class StartDirectMushroomChopCommand
    : ICommand<Result<MushroomChopStartedResult>>
{
    public StartDirectMushroomChopCommand(
        EntityId jobId,
        EntityId siteId,
        EntityId workerId,
        CellId workPosition,
        int priority,
        long tick)
    {
        if (jobId.IsEmpty || siteId.IsEmpty || workerId.IsEmpty)
        {
            throw new ArgumentException("Job, site and worker ids are required.");
        }

        if (priority < 0 || priority > 1000 || tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        JobId = jobId;
        SiteId = siteId;
        WorkerId = workerId;
        WorkPosition = workPosition;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId SiteId { get; }
    public EntityId WorkerId { get; }
    public CellId WorkPosition { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class MushroomChopStartedResult
{
    public MushroomChopStartedResult(
        EntityId jobId,
        EntityId siteId,
        EntityId workerId,
        int requiredSwings,
        EntityId? replacedJobId)
    {
        JobId = jobId;
        SiteId = siteId;
        WorkerId = workerId;
        RequiredSwings = requiredSwings;
        ReplacedJobId = replacedJobId;
    }

    public EntityId JobId { get; }
    public EntityId SiteId { get; }
    public EntityId WorkerId { get; }
    public int RequiredSwings { get; }
    public EntityId? ReplacedJobId { get; }
}

public sealed class ArriveAtMushroomCommand : ICommand<Result>
{
    public ArriveAtMushroomCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CompleteMushroomSwingCommand : ICommand<Result<bool>>
{
    public CompleteMushroomSwingCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CompleteMushroomChopCommand
    : ICommand<Result<MushroomChopCompletionResult>>
{
    public CompleteMushroomChopCommand(EntityId jobId, EntityId outputSeedId, long tick)
    {
        JobId = jobId;
        OutputSeedId = outputSeedId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId OutputSeedId { get; }
    public long Tick { get; }
}

public sealed class CancelMushroomChopCommand : ICommand<Result>
{
    public CancelMushroomChopCommand(EntityId jobId, string reasonCode, long tick)
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

public sealed class MushroomChopCompletionResult
{
    public MushroomChopCompletionResult(
        EntityId jobId,
        EntityId siteId,
        MushroomStage choppedStage,
        IReadOnlyList<EntityId> capUnitIds,
        IReadOnlyList<EntityId> legUnitIds,
        string skillSourceId)
    {
        JobId = jobId;
        SiteId = siteId;
        ChoppedStage = choppedStage;
        CapUnitIds = capUnitIds;
        LegUnitIds = legUnitIds;
        SkillSourceId = skillSourceId;
    }

    public EntityId JobId { get; }
    public EntityId SiteId { get; }
    public MushroomStage ChoppedStage { get; }
    public IReadOnlyList<EntityId> CapUnitIds { get; }
    public IReadOnlyList<EntityId> LegUnitIds { get; }
    public string SkillSourceId { get; }
}

}
