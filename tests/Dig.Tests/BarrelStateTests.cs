using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Xunit;

namespace Dig.Tests
{

public sealed class BarrelStateTests
{
    private static readonly BarrelDefinitionId DefinitionId =
        new BarrelDefinitionId("world.barrel.wooden");
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly ItemId Ore = new ItemId("material.ore");
    private static readonly EntityId BarrelId =
        EntityId.Parse("b1000000000000000000000000000001");
    private static readonly EntityId JobOne =
        EntityId.Parse("b2000000000000000000000000000001");
    private static readonly EntityId JobTwo =
        EntityId.Parse("b2000000000000000000000000000002");
    private static readonly EntityId WorkerOne =
        EntityId.Parse("b3000000000000000000000000000001");
    private static readonly EntityId WorkerTwo =
        EntityId.Parse("b3000000000000000000000000000002");

    [Fact]
    public void First_hit_destroys_barrel_and_materializes_saved_contents_once()
    {
        BarrelState barrels = CreateState();
        CellId cell = new CellId(4, 5, 1);
        Assert.True(barrels.Add(BarrelId, DefinitionId, cell, Stone, tick: 0).IsSuccess);
        BarrelSnapshot before = barrels.Get(BarrelId)!;

        Result<BarrelDestructionCommit> first = barrels.Destroy(
            BarrelId,
            before.Version,
            JobOne,
            WorkerOne,
            tick: 1);
        Result<BarrelDestructionCommit> duplicate = barrels.Destroy(
            BarrelId,
            before.Version,
            JobTwo,
            WorkerTwo,
            tick: 1);

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(Stone, first.Value.ContentsItemId);
        Assert.Equal(cell, first.Value.Cell);
        Assert.Equal(BarrelLifecycle.Destroyed, barrels.Get(BarrelId)!.Lifecycle);
        Assert.True(barrels.Get(BarrelId)!.ContentsMaterialized);
        Assert.Equal(BarrelErrors.NotAttackable, duplicate.Error);
        Assert.Empty(barrels.GetBuildingBlockedCells());
    }

    [Fact]
    public void Concurrent_jobs_do_not_reserve_the_barrel_target()
    {
        CellId target = new CellId(5, 5, 0);
        CellId firstWork = new CellId(4, 5, 0);
        CellId secondWork = new CellId(6, 5, 0);
        BarrelAttackJobDefinition first = CreateJob(JobOne, firstWork);
        BarrelAttackJobDefinition second = CreateJob(JobTwo, secondWork);

        ReservationKey[] firstReservations = first.CreateReservationKeys().ToArray();
        ReservationKey[] secondReservations = second.CreateReservationKeys().ToArray();

        Assert.Single(firstReservations);
        Assert.Single(secondReservations);
        Assert.Contains(ReservationKey.ForPosition(firstWork), firstReservations);
        Assert.Contains(ReservationKey.ForPosition(secondWork), secondReservations);
        Assert.DoesNotContain(firstReservations, value => value == ReservationKey.ForEcologyTarget(BarrelId));
        Assert.Equal(new[]
        {
            JobStageKind.TravelToTarget,
            JobStageKind.PerformWork,
            JobStageKind.Finalize,
        }, first.Stages);
    }

    [Fact]
    public void Support_loss_relocates_barrel_without_damage_or_contents_output()
    {
        BarrelState barrels = CreateState();
        CellId source = new CellId(3, 2, 0);
        CellId landing = new CellId(3, 8, 0);
        Assert.True(barrels.Add(BarrelId, DefinitionId, source, Ore, tick: 0).IsSuccess);

        Assert.True(barrels.BeginFall(BarrelId, landing, tick: 2).IsSuccess);
        Assert.Equal(BarrelLifecycle.Falling, barrels.Get(BarrelId)!.Lifecycle);
        Assert.True(barrels.Land(BarrelId, tick: 2).IsSuccess);

        BarrelSnapshot landed = barrels.Get(BarrelId)!;
        Assert.Equal(BarrelLifecycle.Supported, landed.Lifecycle);
        Assert.Equal(landing, landed.Cell);
        Assert.Equal(Ore, landed.ContentsItemId);
        Assert.False(landed.ContentsMaterialized);
        Assert.Equal(landing, Assert.Single(barrels.GetBuildingBlockedCells()));
    }

    [Fact]
    public void Supported_barrel_blocks_buildings_but_job_has_no_movement_occupancy()
    {
        BarrelState barrels = CreateState();
        CellId cell = new CellId(8, 4, 2);
        Assert.True(barrels.Add(BarrelId, DefinitionId, cell, Stone, tick: 0).IsSuccess);

        Assert.Equal(cell, Assert.Single(barrels.GetBuildingBlockedCells()));
        Assert.True(barrels.Get(BarrelId)!.IsAttackable);
    }

    private static BarrelState CreateState()
    {
        return new BarrelState(new BarrelCatalog(new[]
        {
            new BarrelDefinition(DefinitionId, new[] { Stone, Ore }),
        }));
    }

    private static BarrelAttackJobDefinition CreateJob(EntityId jobId, CellId workPosition)
    {
        return new BarrelAttackJobDefinition(
            jobId,
            BarrelId,
            new CellId(5, 5, 0),
            workPosition,
            barrelVersion: 0,
            contentsGeneration: 0,
            priority: 900,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);
    }
}

}