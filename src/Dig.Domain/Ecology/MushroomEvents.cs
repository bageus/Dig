using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed class MushroomStageChanged : IDomainEvent
{
    public MushroomStageChanged(
        long tick,
        EntityId siteId,
        MushroomStage previousStage,
        MushroomStage currentStage,
        long growthGeneration)
    {
        Tick = tick;
        SiteId = siteId;
        PreviousStage = previousStage;
        CurrentStage = currentStage;
        GrowthGeneration = growthGeneration;
    }

    public long Tick { get; }
    public EntityId SiteId { get; }
    public MushroomStage PreviousStage { get; }
    public MushroomStage CurrentStage { get; }
    public long GrowthGeneration { get; }
}

public sealed class MushroomChopStarted : IDomainEvent
{
    public MushroomChopStarted(
        long tick,
        EntityId siteId,
        EntityId jobId,
        EntityId workerId,
        int requiredSwings)
    {
        Tick = tick;
        SiteId = siteId;
        JobId = jobId;
        WorkerId = workerId;
        RequiredSwings = requiredSwings;
    }

    public long Tick { get; }
    public EntityId SiteId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
    public int RequiredSwings { get; }
}

public sealed class MushroomChopReleased : IDomainEvent
{
    public MushroomChopReleased(long tick, EntityId siteId, EntityId jobId, EntityId workerId)
    {
        Tick = tick;
        SiteId = siteId;
        JobId = jobId;
        WorkerId = workerId;
    }

    public long Tick { get; }
    public EntityId SiteId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
}

public sealed class MushroomChopSwingCompleted : IDomainEvent
{
    public MushroomChopSwingCompleted(
        long tick,
        EntityId siteId,
        EntityId jobId,
        EntityId workerId,
        int completedSwings,
        int requiredSwings)
    {
        Tick = tick;
        SiteId = siteId;
        JobId = jobId;
        WorkerId = workerId;
        CompletedSwings = completedSwings;
        RequiredSwings = requiredSwings;
    }

    public long Tick { get; }
    public EntityId SiteId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
    public int CompletedSwings { get; }
    public int RequiredSwings { get; }
}

public sealed class MushroomChopped : IDomainEvent
{
    public MushroomChopped(
        long tick,
        EntityId siteId,
        EntityId jobId,
        EntityId workerId,
        CellId cell,
        MushroomStage choppedStage,
        long growthGeneration)
    {
        Tick = tick;
        SiteId = siteId;
        JobId = jobId;
        WorkerId = workerId;
        Cell = cell;
        ChoppedStage = choppedStage;
        GrowthGeneration = growthGeneration;
    }

    public long Tick { get; }
    public EntityId SiteId { get; }
    public EntityId JobId { get; }
    public EntityId WorkerId { get; }
    public CellId Cell { get; }
    public MushroomStage ChoppedStage { get; }
    public long GrowthGeneration { get; }
}

}
