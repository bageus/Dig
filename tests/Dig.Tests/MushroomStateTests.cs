using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class MushroomStateTests
{
    private static readonly MushroomDefinitionId DefinitionId =
        new MushroomDefinitionId("ecology.mushroom.common");
    private static readonly ItemId Cap = new ItemId("material.mushroom_cap");
    private static readonly ItemId Leg = new ItemId("material.mushroom_leg");
    private static readonly EntityId SiteId =
        EntityId.Parse("a1000000000000000000000000000001");
    private static readonly EntityId JobId =
        EntityId.Parse("a2000000000000000000000000000001");
    private static readonly EntityId WorkerId =
        EntityId.Parse("a3000000000000000000000000000001");

    [Fact]
    public void Growth_reaches_large_and_stops_there()
    {
        MushroomState mushrooms = CreateState(stageDurationTicks: 10);
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            new CellId(4, 5, 0),
            MushroomStage.Tiny,
            tick: 0).IsSuccess);

        Assert.True(mushrooms.AdvanceGrowth(tick: 30).IsSuccess);
        MushroomSiteSnapshot site = mushrooms.Get(SiteId)!;

        Assert.Equal(MushroomStage.Large, site.Stage);
        Assert.Null(site.NextStageTick);
        Assert.Equal(30, site.StageStartedTick);
        Assert.True(mushrooms.AdvanceGrowth(tick: 1000).IsSuccess);
        Assert.Equal(MushroomStage.Large, mushrooms.Get(SiteId)!.Stage);
    }

    [Fact]
    public void Active_chop_pauses_growth_and_release_preserves_remaining_duration()
    {
        MushroomState mushrooms = CreateState(stageDurationTicks: 10);
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            new CellId(1, 2, 0),
            MushroomStage.Tiny,
            tick: 0).IsSuccess);
        Assert.True(mushrooms.BeginChop(SiteId, JobId, WorkerId, 3, tick: 4).IsSuccess);

        Assert.True(mushrooms.AdvanceGrowth(tick: 100).IsSuccess);
        Assert.Equal(MushroomStage.Tiny, mushrooms.Get(SiteId)!.Stage);

        Assert.True(mushrooms.ReleaseChop(SiteId, JobId, WorkerId, tick: 20).IsSuccess);
        MushroomSiteSnapshot released = mushrooms.Get(SiteId)!;
        Assert.Equal(26, released.NextStageTick);
        Assert.Equal(0, released.CompletedSwings);
        Assert.Null(released.ActiveChopJobId);

        Assert.True(mushrooms.AdvanceGrowth(tick: 25).IsSuccess);
        Assert.Equal(MushroomStage.Tiny, mushrooms.Get(SiteId)!.Stage);
        Assert.True(mushrooms.AdvanceGrowth(tick: 26).IsSuccess);
        Assert.Equal(MushroomStage.Small, mushrooms.Get(SiteId)!.Stage);
    }

    [Fact]
    public void Release_resets_swings_and_new_worker_gets_full_attempt()
    {
        MushroomState mushrooms = CreateState(stageDurationTicks: 10);
        EntityId newJob = EntityId.Parse("a2000000000000000000000000000002");
        EntityId newWorker = EntityId.Parse("a3000000000000000000000000000002");
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            new CellId(2, 2, 0),
            MushroomStage.Medium,
            tick: 0).IsSuccess);
        Assert.True(mushrooms.BeginChop(SiteId, JobId, WorkerId, 5, tick: 1).IsSuccess);
        Assert.False(mushrooms.CompleteSwing(SiteId, JobId, WorkerId, tick: 2).Value);
        Assert.False(mushrooms.CompleteSwing(SiteId, JobId, WorkerId, tick: 3).Value);

        Assert.True(mushrooms.ReleaseChop(SiteId, JobId, WorkerId, tick: 4).IsSuccess);
        Assert.True(mushrooms.BeginChop(SiteId, newJob, newWorker, 3, tick: 4).IsSuccess);
        MushroomSiteSnapshot replacement = mushrooms.Get(SiteId)!;

        Assert.Equal(0, replacement.CompletedSwings);
        Assert.Equal(3, replacement.RequiredSwings);
        Assert.Equal(newWorker, replacement.ActiveWorkerId);
    }

    [Theory]
    [InlineData(MushroomStage.Tiny, 1, 0)]
    [InlineData(MushroomStage.Small, 1, 0)]
    [InlineData(MushroomStage.Medium, 2, 0)]
    [InlineData(MushroomStage.Large, 2, 1)]
    public void Completion_uses_exact_stage_drop_table(
        MushroomStage stage,
        int expectedCaps,
        int expectedLegs)
    {
        MushroomState mushrooms = CreateState(stageDurationTicks: 10);
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            new CellId(3, 4, 1),
            stage,
            tick: 0).IsSuccess);
        Assert.True(mushrooms.BeginChop(SiteId, JobId, WorkerId, 1, tick: 1).IsSuccess);
        Assert.True(mushrooms.CompleteSwing(SiteId, JobId, WorkerId, tick: 2).Value);

        Result<MushroomChopCommit> result = mushrooms.CommitChop(
            SiteId,
            JobId,
            WorkerId,
            tick: 3);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(expectedCaps, result.Value.Drops.CapCount);
        Assert.Equal(expectedLegs, result.Value.Drops.LegCount);
        Assert.Equal(MushroomStage.AbsentRegrowing, mushrooms.Get(SiteId)!.Stage);
        Assert.Equal(1, mushrooms.Get(SiteId)!.GrowthGeneration);
        Assert.Equal($"mushroom:{SiteId}:1", result.Value.SkillSourceId);
    }

    [Fact]
    public void Absent_stage_is_not_choppable_and_regrows_to_tiny()
    {
        MushroomState mushrooms = CreateState(stageDurationTicks: 10);
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            new CellId(3, 4, 0),
            MushroomStage.AbsentRegrowing,
            tick: 0).IsSuccess);

        Result rejected = mushrooms.BeginChop(SiteId, JobId, WorkerId, 1, tick: 1);
        Assert.Equal(MushroomErrors.NotVisible, rejected.Error);
        Assert.True(mushrooms.AdvanceGrowth(tick: 10).IsSuccess);
        Assert.Equal(MushroomStage.Tiny, mushrooms.Get(SiteId)!.Stage);
    }

    [Theory]
    [InlineData(0, 6, 8)]
    [InlineData(10, 6, 8)]
    [InlineData(11, 5, 6)]
    [InlineData(20, 5, 6)]
    [InlineData(21, 3, 5)]
    [InlineData(40, 3, 5)]
    [InlineData(41, 2, 3)]
    [InlineData(60, 2, 3)]
    [InlineData(61, 1, 2)]
    [InlineData(80, 1, 2)]
    [InlineData(81, 1, 1)]
    [InlineData(100, 1, 1)]
    public void Woodworking_bands_match_approved_required_swings(
        int points,
        int expectedMinimum,
        int expectedMaximum)
    {
        (int actualMinimum, int actualMaximum) = MushroomDefinition.GetRequiredSwingBand(
            points * AgentSkillCatalog.UnitsPerPoint);

        Assert.Equal(expectedMinimum, actualMinimum);
        Assert.Equal(expectedMaximum, actualMaximum);
    }

    [Fact]
    public void Site_cell_is_permanently_blocked_for_buildings_even_when_absent()
    {
        MushroomState mushrooms = CreateState(stageDurationTicks: 10);
        CellId cell = new CellId(7, 8, 2);
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            cell,
            MushroomStage.AbsentRegrowing,
            tick: 0).IsSuccess);

        Assert.Equal(cell, Assert.Single(mushrooms.GetBuildingBlockedCells()));
    }

    [Fact]
    public void Mushroom_job_reserves_site_and_work_position()
    {
        CellId target = new CellId(4, 4, 0);
        CellId work = new CellId(3, 4, 0);
        MushroomChopJobDefinition definition = new MushroomChopJobDefinition(
            JobId,
            SiteId,
            target,
            work,
            growthGeneration: 0,
            requiredSwings: 6,
            priority: 900,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);

        ReservationKey[] reservations = definition.CreateReservationKeys().ToArray();

        Assert.Contains(ReservationKey.ForEcologyTarget(SiteId), reservations);
        Assert.Contains(ReservationKey.ForPosition(work), reservations);
        Assert.Equal(new[]
        {
            JobStageKind.TravelToTarget,
            JobStageKind.PerformWork,
            JobStageKind.Finalize,
        }, definition.Stages);
    }

    private static MushroomState CreateState(long stageDurationTicks)
    {
        return new MushroomState(new MushroomCatalog(new[]
        {
            new MushroomDefinition(DefinitionId, stageDurationTicks, Cap, Leg),
        }));
    }
}

}
