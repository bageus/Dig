using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

internal sealed class MushroomSiteState
{
    private MushroomSiteState(
        EntityId siteId,
        MushroomDefinitionId definitionId,
        CellId cell,
        MushroomStage stage,
        long stageStartedTick,
        long? nextStageTick,
        long growthGeneration,
        EntityId? activeChopJobId,
        EntityId? activeWorkerId,
        int requiredSwings,
        int completedSwings,
        long? growthPausedAtTick,
        long version)
    {
        SiteId = siteId;
        DefinitionId = definitionId;
        Cell = cell;
        Stage = stage;
        StageStartedTick = stageStartedTick;
        NextStageTick = nextStageTick;
        GrowthGeneration = growthGeneration;
        ActiveChopJobId = activeChopJobId;
        ActiveWorkerId = activeWorkerId;
        RequiredSwings = requiredSwings;
        CompletedSwings = completedSwings;
        GrowthPausedAtTick = growthPausedAtTick;
        Version = version;
    }

    public EntityId SiteId { get; }
    public MushroomDefinitionId DefinitionId { get; }
    public CellId Cell { get; }
    public MushroomStage Stage { get; private set; }
    public long StageStartedTick { get; private set; }
    public long? NextStageTick { get; private set; }
    public long GrowthGeneration { get; private set; }
    public EntityId? ActiveChopJobId { get; private set; }
    public EntityId? ActiveWorkerId { get; private set; }
    public int RequiredSwings { get; private set; }
    public int CompletedSwings { get; private set; }
    public long? GrowthPausedAtTick { get; private set; }
    public long Version { get; private set; }

    public static MushroomSiteState Create(
        EntityId siteId,
        MushroomDefinitionId definitionId,
        CellId cell,
        MushroomStage stage,
        long tick,
        long duration)
    {
        return new MushroomSiteState(
            siteId,
            definitionId,
            cell,
            stage,
            tick,
            stage == MushroomStage.Large ? null : checked(tick + duration),
            0,
            null,
            null,
            0,
            0,
            null,
            0);
    }

    public static MushroomSiteState Restore(MushroomSiteSnapshot snapshot)
    {
        return new MushroomSiteState(
            snapshot.SiteId,
            snapshot.DefinitionId,
            snapshot.Cell,
            snapshot.Stage,
            snapshot.StageStartedTick,
            snapshot.NextStageTick,
            snapshot.GrowthGeneration,
            snapshot.ActiveChopJobId,
            snapshot.ActiveWorkerId,
            snapshot.RequiredSwings,
            snapshot.CompletedSwings,
            snapshot.GrowthPausedAtTick,
            snapshot.Version);
    }

    public static bool IsValidSnapshot(MushroomSiteSnapshot snapshot)
    {
        bool active = snapshot.ActiveChopJobId.HasValue;
        return snapshot.StageStartedTick >= 0
            && snapshot.GrowthGeneration >= 0
            && snapshot.Version >= 0
            && Enum.IsDefined(typeof(MushroomStage), snapshot.Stage)
            && (snapshot.Stage == MushroomStage.Large
                ? !snapshot.NextStageTick.HasValue
                : snapshot.NextStageTick.HasValue
                    && snapshot.NextStageTick.Value > snapshot.StageStartedTick)
            && active == snapshot.ActiveWorkerId.HasValue
            && active == snapshot.GrowthPausedAtTick.HasValue
            && (!active || (snapshot.RequiredSwings > 0
                && snapshot.CompletedSwings >= 0
                && snapshot.CompletedSwings <= snapshot.RequiredSwings))
            && (active || (snapshot.RequiredSwings == 0 && snapshot.CompletedSwings == 0));
    }

    public void AdvanceStage(long tick, long duration)
    {
        Stage = Stage switch
        {
            MushroomStage.AbsentRegrowing => MushroomStage.Tiny,
            MushroomStage.Tiny => MushroomStage.Small,
            MushroomStage.Small => MushroomStage.Medium,
            MushroomStage.Medium => MushroomStage.Large,
            MushroomStage.Large => MushroomStage.Large,
            _ => throw new InvalidOperationException("Unknown mushroom stage."),
        };
        StageStartedTick = tick;
        NextStageTick = Stage == MushroomStage.Large ? null : checked(tick + duration);
        IncrementVersion();
    }

    public void BeginChop(EntityId jobId, EntityId workerId, int requiredSwings, long tick)
    {
        ActiveChopJobId = jobId;
        ActiveWorkerId = workerId;
        RequiredSwings = requiredSwings;
        CompletedSwings = 0;
        GrowthPausedAtTick = tick;
        IncrementVersion();
    }

    public bool CompleteSwing()
    {
        if (CompletedSwings < RequiredSwings)
        {
            CompletedSwings = checked(CompletedSwings + 1);
            IncrementVersion();
        }

        return CompletedSwings >= RequiredSwings;
    }

    public void ReleaseChop(long tick)
    {
        if (NextStageTick.HasValue && GrowthPausedAtTick.HasValue)
        {
            NextStageTick = checked(NextStageTick.Value + tick - GrowthPausedAtTick.Value);
        }

        ClearChop();
        IncrementVersion();
    }

    public void CommitChop(long tick, long duration)
    {
        GrowthGeneration = checked(GrowthGeneration + 1);
        Stage = MushroomStage.AbsentRegrowing;
        StageStartedTick = tick;
        NextStageTick = checked(tick + duration);
        ClearChop();
        IncrementVersion();
    }

    public MushroomSiteSnapshot Snapshot()
    {
        return new MushroomSiteSnapshot(
            SiteId,
            DefinitionId,
            Cell,
            Stage,
            StageStartedTick,
            NextStageTick,
            GrowthGeneration,
            ActiveChopJobId,
            ActiveWorkerId,
            RequiredSwings,
            CompletedSwings,
            GrowthPausedAtTick,
            Version);
    }

    private void ClearChop()
    {
        ActiveChopJobId = null;
        ActiveWorkerId = null;
        RequiredSwings = 0;
        CompletedSwings = 0;
        GrowthPausedAtTick = null;
    }

    private void IncrementVersion() => Version = checked(Version + 1);
}

}
