using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Tunnels;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelRuntimeTopologyProjectorTests
{
    private static readonly MaterialId Air = new MaterialId("test.air");

    [Fact]
    public void Vertical_junction_projects_independent_completed_left_and_right_segments()
    {
        CellId junction = new CellId(10, 8, 0);
        CellId[] planned = Enumerable.Range(2, 19)
            .Select(x => new CellId(x, 8, 0))
            .ToArray();
        WorldSnapshot world = CreateWorld(planned);

        IReadOnlyList<TunnelTopologySegmentProvenance> result =
            new TunnelRuntimeTopologyProjector().Project(
                world,
                Array.Empty<CaveRoomPlan>(),
                planned,
                new[] { junction });

        Assert.Equal(2, result.Count);
        TunnelTopologySegmentProvenance left = result.Single(value => value.Direction == -1);
        TunnelTopologySegmentProvenance right = result.Single(value => value.Direction == 1);
        Assert.Equal(TunnelSegmentOriginKind.VerticalJunction, left.OriginKind);
        Assert.Equal(junction, left.OriginCell);
        Assert.Equal(new CellId(9, 8, 0), left.OrderedHorizontalCells[0]);
        Assert.Equal(new CellId(2, 8, 0), left.OrderedHorizontalCells[left.OrderedHorizontalCells.Count - 1]);
        Assert.Equal(new CellId(11, 8, 0), right.OrderedHorizontalCells[0]);
        Assert.Equal(new CellId(20, 8, 0), right.OrderedHorizontalCells[right.OrderedHorizontalCells.Count - 1]);
    }

    [Fact]
    public void Room_exit_owns_corridor_to_junction_without_duplicate_reverse_segment()
    {
        CellId[] planned = Enumerable.Range(2, 19)
            .Select(x => new CellId(x, 8, 0))
            .ToArray();
        CaveRoomPlan room = CreateCompletedRoom();
        CellId junction = new CellId(4, 8, 0);
        WorldSnapshot world = CreateWorld(
            planned.Concat(room.VolumeCells));

        IReadOnlyList<TunnelTopologySegmentProvenance> result =
            new TunnelRuntimeTopologyProjector().Project(
                world,
                new[] { room },
                planned,
                new[] { junction });

        Assert.Equal(3, result.Count);
        TunnelTopologySegmentProvenance roomLeft = result.Single(value =>
            value.OriginKind == TunnelSegmentOriginKind.RoomExit
            && value.Direction == -1);
        Assert.Equal(new CellId(8, 8, 0), roomLeft.OriginCell);
        Assert.Equal(
            new[]
            {
                new CellId(7, 8, 0),
                new CellId(6, 8, 0),
                new CellId(5, 8, 0),
            },
            roomLeft.OrderedHorizontalCells);
        Assert.DoesNotContain(result, value =>
            value.OriginKind == TunnelSegmentOriginKind.VerticalJunction
            && value.OriginCell == junction
            && value.Direction == 1);
    }

    [Fact]
    public void Solid_planned_gap_truncates_segment_and_input_order_does_not_change_identity()
    {
        CellId junction = new CellId(10, 8, 0);
        CellId[] planned = Enumerable.Range(10, 8)
            .Select(x => new CellId(x, 8, 0))
            .ToArray();
        CellId[] completed = planned.Where(cell => cell.X != 15).ToArray();
        WorldSnapshot world = CreateWorld(completed);
        TunnelRuntimeTopologyProjector projector = new TunnelRuntimeTopologyProjector();

        IReadOnlyList<TunnelTopologySegmentProvenance> forward = projector.Project(
            world,
            Array.Empty<CaveRoomPlan>(),
            planned,
            new[] { junction });
        IReadOnlyList<TunnelTopologySegmentProvenance> reverse = projector.Project(
            world,
            Array.Empty<CaveRoomPlan>(),
            planned.Reverse(),
            new[] { junction });

        TunnelTopologySegmentProvenance first = Assert.Single(forward);
        TunnelTopologySegmentProvenance second = Assert.Single(reverse);
        Assert.Equal(first.SegmentId, second.SegmentId);
        Assert.Equal(
            new[]
            {
                new CellId(11, 8, 0),
                new CellId(12, 8, 0),
                new CellId(13, 8, 0),
                new CellId(14, 8, 0),
            },
            first.OrderedHorizontalCells);
    }

    private static CaveRoomPlan CreateCompletedRoom()
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Small);
        CellId[] baseCells = Enumerable.Range(8, 5)
            .Select(x => new CellId(x, 8, 0))
            .ToArray();
        CellId[] upper = new[]
        {
            new CellId(9, 7, 0),
            new CellId(10, 7, 0),
            new CellId(11, 7, 0),
        };
        CellId[] volume = baseCells.Concat(upper).ToArray();
        return CaveRoomPlan.CreateSnapshot(
            preset,
            new CellId(10, 8, 0),
            upper,
            upper,
            baseCells,
            volume,
            Array.Empty<CellId>());
    }

    private static WorldSnapshot CreateWorld(IEnumerable<CellId> openCells)
    {
        MaterialId rock = DefaultTerrainMaterials.StoneRock;
        MaterialCatalog defaults = DefaultTerrainMaterials.CreateCatalog();
        MaterialCatalog materials = new MaterialCatalog(
            defaults.Definitions.Concat(new[]
            {
                new MaterialDefinition(Air, isSolid: false, hardness: 0),
            }));
        WorldState world = WorldState.CreateFilled(
            new WorldSize(30, 20),
            chunkSize: 5,
            materials,
            rock,
            explored: true).Value;
        CellState empty = new CellState(
            Air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
        TerrainChange[] changes = openCells
            .Distinct()
            .OrderBy(cell => cell)
            .Select(cell => new TerrainChange(cell, empty))
            .ToArray();
        Assert.True(world.ApplyTerrainChanges(changes, tick: 1).IsSuccess);
        return world.CreateSnapshot();
    }
}
}
