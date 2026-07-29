using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveRoomResumePlannerTests
{
    [Fact]
    public void Paused_room_reapply_designates_only_unfinished_targets()
    {
        Fixture fixture = CreateFixture();
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);
        CaveRoomPlan original = new CaveRoomPlanner().Plan(
            fixture.State.CreateSnapshot(),
            fixture.Materials,
            boundary,
            CaveRoomPresetKind.Medium,
            new CellId(10, 9)).Plan!;
        CaveRoomExcavationTarget completed = original.ExcavationTargets
            .First(value => value.IsFullCell);
        Assert.True(fixture.State.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(
                    completed.Cell,
                    new CellState(
                        fixture.Air,
                        CellDesignation.None,
                        isExplored: true,
                        damage: 0,
                        temperature: 20)),
            },
            tick: 2).IsSuccess);

        CaveRoomPlanResult resumed = new CaveRoomResumePlanner().Plan(
            fixture.State.CreateSnapshot(),
            fixture.Materials,
            boundary,
            original);

        Assert.True(resumed.Succeeded, resumed.Detail);
        Assert.DoesNotContain(completed.Cell, resumed.Plan!.ExcavationCells);
        Assert.Equal(
            original.ExcavationTargets.Count - 1,
            resumed.Plan.ExcavationTargets.Count);
        Assert.Equal(original.VolumeCells, resumed.Plan.VolumeCells);
        Assert.Equal(original.BaseTunnelCells, resumed.Plan.BaseTunnelCells);
    }

    [Fact]
    public void Resume_rejects_missing_base_tunnel_instead_of_upgrading_arbitrary_air()
    {
        Fixture fixture = CreateFixture();
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);
        CaveRoomPlan original = new CaveRoomPlanner().Plan(
            fixture.State.CreateSnapshot(),
            fixture.Materials,
            boundary,
            CaveRoomPresetKind.Small,
            new CellId(10, 9)).Plan!;
        CellId baseCell = original.BaseTunnelCells[0];
        Assert.True(fixture.State.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(
                    baseCell,
                    new CellState(
                        fixture.Rock,
                        CellDesignation.None,
                        isExplored: true,
                        damage: 0,
                        temperature: 20)),
            },
            tick: 2).IsSuccess);

        CaveRoomPlanResult resumed = new CaveRoomResumePlanner().Plan(
            fixture.State.CreateSnapshot(),
            fixture.Materials,
            boundary,
            original);

        Assert.False(resumed.Succeeded);
        Assert.Equal(
            CaveRoomPlanFailureReason.BaseTunnelMissing,
            resumed.FailureReason);
    }

    private static Fixture CreateFixture()
    {
        MaterialId rock = new MaterialId("test.rock");
        MaterialId air = new MaterialId("test.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState state = WorldState.CreateFilled(
            new WorldSize(20, 14),
            chunkSize: 5,
            materials,
            rock,
            explored: true).Value;
        CellState empty = new CellState(
            air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
        List<TerrainChange> tunnel = Enumerable.Range(1, 18)
            .Select(x => new TerrainChange(new CellId(x, 9), empty))
            .ToList();
        Assert.True(state.ApplyTerrainChanges(tunnel, tick: 1).IsSuccess);
        return new Fixture(state, materials, rock, air);
    }

    private sealed class Fixture
    {
        public Fixture(
            WorldState state,
            MaterialCatalog materials,
            MaterialId rock,
            MaterialId air)
        {
            State = state;
            Materials = materials;
            Rock = rock;
            Air = air;
        }

        public WorldState State { get; }
        public MaterialCatalog Materials { get; }
        public MaterialId Rock { get; }
        public MaterialId Air { get; }
    }
}

}
