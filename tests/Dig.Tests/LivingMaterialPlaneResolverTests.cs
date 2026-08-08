using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialPlaneResolverTests
{
    [Fact]
    public void WallSplitsSameHeightIntoDifferentMovementRegions()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 6,
            chunkSize: 4,
            corridorY: 2,
            blockedX: 6);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateFreeMover());
        LivingMaterialPlaneResolver resolver = new LivingMaterialPlaneResolver(
            NavigationTestFactory.GetSnapshot(map));

        Assert.True(resolver.TryResolve(new CellId(2, 2, 0), out LivingMaterialPlane left));
        Assert.True(resolver.TryResolve(new CellId(9, 2, 0), out LivingMaterialPlane right));
        Assert.NotEqual(left.Key, right.Key);
    }

    [Fact]
    public void MovementRegionAndCandidatesIncludeDepthAndDiagonalWithoutChangingHeight()
    {
        CellId from = new CellId(3, 2, 0);
        WorldState world = OpenCells(
            from,
            new CellId(2, 2, 0),
            new CellId(4, 2, 0),
            new CellId(2, 2, 1),
            new CellId(3, 2, 1),
            new CellId(4, 2, 1),
            new CellId(3, 3, 0));
        LivingMaterialPlaneResolver resolver = Resolver(world);
        Assert.True(resolver.TryResolve(from, out LivingMaterialPlane plane));
        LivingMaterialSnapshot creature = Creature(from, plane.Key);

        IReadOnlyList<CellId> candidates = resolver.GetMovementCandidates(creature);

        Assert.Contains(new CellId(3, 2, 1), candidates);
        Assert.Contains(new CellId(4, 2, 1), candidates);
        Assert.Contains(new CellId(2, 2, 1), candidates);
        Assert.All(candidates, value => Assert.Equal(from.Y, value.Y));
        Assert.DoesNotContain(new CellId(3, 3, 0), candidates);
        Assert.Contains(new CellId(3, 2, 1), plane.Cells);
    }

    [Fact]
    public void DiagonalCandidateCannotCutABlockedCorner()
    {
        CellId from = new CellId(3, 2, 0);
        CellId sideX = new CellId(4, 2, 0);
        CellId diagonal = new CellId(4, 2, 1);
        WorldState world = OpenCells(from, sideX, diagonal);
        LivingMaterialPlaneResolver resolver = Resolver(world);
        Assert.True(resolver.TryResolve(from, out LivingMaterialPlane plane));

        IReadOnlyList<CellId> candidates = resolver.GetMovementCandidates(
            Creature(from, plane.Key));

        Assert.Contains(sideX, candidates);
        Assert.DoesNotContain(diagonal, candidates);
    }

    [Fact]
    public void MovementCandidatesExcludeVerticalShaftGapAndKeepFloorDetour()
    {
        CellId from = new CellId(2, 2, 0);
        CellId shaftGap = new CellId(3, 2, 0);
        CellId detour = new CellId(2, 2, 1);
        WorldState world = OpenCells(
            from,
            shaftGap,
            new CellId(4, 2, 0),
            detour,
            new CellId(3, 2, 1),
            new CellId(4, 2, 1),
            new CellId(3, 3, 0));
        CellState shaftState = new CellState(
            NavigationTestFactory.Air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20,
            excavationCutPattern: ExcavationCutPattern.HorizontalRows);
        Assert.True(world.ApplyTerrainChanges(
            new[] { new TerrainChange(shaftGap, shaftState) },
            tick: 2).IsSuccess);
        LivingMaterialPlaneResolver resolver = Resolver(world);
        Assert.True(resolver.TryResolve(from, out LivingMaterialPlane plane));

        IReadOnlyList<CellId> candidates = resolver.GetMovementCandidates(
            Creature(from, plane.Key));

        Assert.DoesNotContain(shaftGap, candidates);
        Assert.Contains(detour, candidates);
    }

    private static LivingMaterialSnapshot Creature(
        CellId cell,
        LivingMaterialPlaneKey planeKey)
    {
        EntityId id = EntityId.Parse("30000000000000000000000000000001");
        LivingMaterialEcologyState state = new LivingMaterialEcologyState(1);
        Assert.True(state.Register(
            id,
            id,
            LivingMaterialSpecies.Grub,
            cell,
            planeKey,
            0).IsSuccess);
        return state.Get(id)!;
    }

    private static LivingMaterialPlaneResolver Resolver(WorldState world)
    {
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateFreeMover());
        return new LivingMaterialPlaneResolver(
            NavigationTestFactory.GetSnapshot(map));
    }

    private static WorldState OpenCells(params CellId[] cells)
    {
        WorldState world = NavigationTestFactory.CreateStoneWorld(
            width: 8,
            height: 6,
            chunkSize: 4);
        List<TerrainChange> changes = cells
            .Distinct()
            .Select(value => new TerrainChange(
                value,
                NavigationTestFactory.CreateState(NavigationTestFactory.Air)))
            .ToList();
        Result<WorldMutationResult> changed = world.ApplyTerrainChanges(changes, tick: 1);
        Assert.True(changed.IsSuccess);
        world.DrainDirtyChunks();
        world.DequeueUncommittedEvents();
        return world;
    }
}

}
