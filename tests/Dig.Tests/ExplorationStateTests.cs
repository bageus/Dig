using System;
using System.Collections.Generic;
using Dig.Domain.Exploration;
using Dig.Domain.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{
public sealed class ExplorationStateTests
{
    private static readonly MaterialId Air = new MaterialId("air");
    private static readonly MaterialId Rock = new MaterialId("rock");

    [Fact]
    public void Resident_uses_graph_radius_four()
    {
        WorldState world = CreateOpenWorld(24, 24);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(11, 11, 1)));
        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(15, 11, 1)));
        Assert.Equal(CellVisibility.Unexplored, exploration.GetVisibility(new CellId(16, 11, 1)));
    }

    [Fact]
    public void Resident_vision_reaches_horizontal_vertical_and_xyz_diagonals()
    {
        WorldState world = CreateOpenWorld(16, 16);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(6, 6, 0)));

        Assert.True(exploration.IsVisible(new CellId(10, 6, 0)));
        Assert.True(exploration.IsVisible(new CellId(6, 10, 0)));
        Assert.True(exploration.IsVisible(new CellId(10, 10, 0)));
        Assert.True(exploration.IsVisible(new CellId(9, 9, 3)));
        Assert.False(exploration.IsVisible(new CellId(11, 11, 3)));
    }

    [Fact]
    public void Diagonal_corner_cutting_is_allowed_when_target_is_open()
    {
        WorldState world = CreateOpenWorld(6, 6);
        SetCell(world, new CellId(2, 2, 1), Rock, tick: 1);
        SetCell(world, new CellId(1, 1, 1), Rock, tick: 2);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(1, 1, 1)),
            additionalBlockers: new HashSet<CellId>
            {
                new CellId(2, 1, 1),
                new CellId(1, 2, 1),
            });

        Assert.True(exploration.IsVisible(new CellId(2, 2, 1)));
    }

    [Fact]
    public void Z_edge_and_corner_diagonals_use_one_graph_step()
    {
        WorldState world = CreateOpenWorld(10, 10);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(4, 4, 1)));

        Assert.True(exploration.IsVisible(new CellId(5, 4, 2)));
        Assert.True(exploration.IsVisible(new CellId(5, 5, 2)));
    }

    [Fact]
    public void Solid_target_is_boundary_only_and_does_not_propagate()
    {
        WorldState world = CreateOpenWorld(9, 9);
        SetCell(world, new CellId(4, 4, 1), Rock, tick: 1);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(3, 3, 1)));

        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(4, 4, 1)));
        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(5, 5, 1)));
    }

    [Fact]
    public void Tunnel_reveals_orthogonal_and_diagonal_boundary_rock_without_seeing_through_it()
    {
        WorldState world = CreateOpenWorld(10, 8);
        SetColumn(world, x: 4, Rock, tick: 1, exceptY: -1);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(2, 3, 1)));
        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(4, 3, 1)));
        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(4, 4, 1)));
        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(4, 4, 2)));
        Assert.Equal(CellVisibility.Unexplored, exploration.GetVisibility(new CellId(5, 3, 1)));
    }

    [Fact]
    public void Building_radius_is_combined_from_every_occupied_footprint_cell()
    {
        WorldState world = CreateOpenWorld(12, 10);
        CellId[] footprint = new[]
        {
            new CellId(2, 2, 1), new CellId(2, 2, 2),
            new CellId(2, 3, 1), new CellId(2, 3, 2),
            new CellId(3, 2, 1), new CellId(3, 2, 2),
            new CellId(3, 3, 1), new CellId(3, 3, 2),
        };
        ExplorationState state = new ExplorationState();
        state.Recalculate(world.CreateSnapshot(), new[]
        {
            new VisionSourceSnapshot("building", VisionSourceKind.Building, footprint),
        });
        Assert.True(state.IsVisible(new CellId(8, 3, 1)));
        Assert.False(state.IsVisible(new CellId(9, 3, 1)));

        ExplorationState damaged = new ExplorationState();
        damaged.Recalculate(world.CreateSnapshot(), new[]
        {
            new VisionSourceSnapshot("building", VisionSourceKind.DamagedBuilding, footprint),
        });
        Assert.True(damaged.IsVisible(new CellId(5, 3, 1)));
        Assert.False(damaged.IsVisible(new CellId(6, 3, 1)));
    }

    [Fact]
    public void Building_extreme_footprint_cells_cast_full_three_dimensional_vision()
    {
        WorldState world = CreateOpenWorld(18, 18);
        CellId[] footprint = new[]
        {
            new CellId(4, 4, 0), new CellId(4, 4, 1),
            new CellId(4, 5, 0), new CellId(4, 5, 1),
            new CellId(5, 4, 0), new CellId(5, 4, 1),
            new CellId(5, 5, 0), new CellId(5, 5, 1),
        };
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), new[]
        {
            new VisionSourceSnapshot("building", VisionSourceKind.Building, footprint),
        });

        Assert.True(exploration.IsVisible(new CellId(10, 10, 3)));
        Assert.True(exploration.IsVisible(new CellId(0, 0, 3)));
        Assert.False(exploration.IsVisible(new CellId(11, 11, 3)));
    }

    [Fact]
    public void Solid_ceiling_blocks_upper_cave()
    {
        WorldState world = CreateOpenWorld(7, 7);
        SetLayer(world, y: 3, Rock, tick: 1);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(3, 5, 1)));
        Assert.Equal(CellVisibility.Unexplored, exploration.GetVisibility(new CellId(3, 2, 1)));
    }

    [Fact]
    public void Passage_and_open_door_reveal_but_closed_door_blocks()
    {
        WorldState world = CreateOpenWorld(9, 7);
        SetColumn(world, x: 4, Rock, tick: 1, exceptY: 3);
        CellId doorway = new CellId(4, 3, 1);
        SealOtherDepths(world, doorway, tick: 2);
        ExplorationState open = new ExplorationState();
        open.Recalculate(world.CreateSnapshot(), Sources(new CellId(2, 3, 1)));
        Assert.True(open.IsVisible(new CellId(6, 3, 1)));
        ExplorationState closed = new ExplorationState();
        closed.Recalculate(world.CreateSnapshot(), Sources(new CellId(2, 3, 1)), new HashSet<CellId> { doorway });
        Assert.False(closed.IsVisible(new CellId(6, 3, 1)));
    }

    [Fact]
    public void Removing_source_preserves_explored_history()
    {
        WorldState world = CreateOpenWorld(8, 8);
        ExplorationState state = new ExplorationState();
        CellId observed = new CellId(5, 5, 1);
        state.Recalculate(world.CreateSnapshot(), Sources(observed));
        state.Recalculate(world.CreateSnapshot(), Array.Empty<VisionSourceSnapshot>());
        Assert.Equal(CellVisibility.ExploredNotVisible, state.GetVisibility(observed));
    }

    [Fact]
    public void Lift_origins_cover_full_shaft_and_damaged_radius_is_two()
    {
        WorldState world = CreateOpenWorld(12, 12);
        ExplorationState state = new ExplorationState();
        state.Recalculate(world.CreateSnapshot(), new[]
        {
            new VisionSourceSnapshot("lift", VisionSourceKind.Lift,
                new[] { new CellId(2, 2, 0), new CellId(2, 2, 1), new CellId(2, 2, 2), new CellId(2, 2, 3) }),
            new VisionSourceSnapshot("damaged", VisionSourceKind.DamagedBuilding,
                new[] { new CellId(8, 8, 1) }),
        });
        Assert.True(state.IsVisible(new CellId(2, 4, 3)));
        Assert.True(state.IsVisible(new CellId(10, 8, 1)));
        Assert.False(state.IsVisible(new CellId(11, 8, 1)));
    }

    [Fact]
    public void Save_restores_history_but_recalculates_current_visibility()
    {
        WorldState world = CreateOpenWorld(8, 8);
        ExplorationState state = new ExplorationState();
        CellId cell = new CellId(3, 3, 1);
        state.Recalculate(world.CreateSnapshot(), Sources(cell));
        ExplorationState restored = ExplorationState.Restore(state.CreateSaveSnapshot());
        Assert.Equal(CellVisibility.ExploredNotVisible, restored.GetVisibility(cell));
        restored.Recalculate(world.CreateSnapshot(), Sources(cell));
        Assert.Equal(CellVisibility.Visible, restored.GetVisibility(cell));
    }

    [Fact]
    public void Remembered_item_stays_at_last_visible_position()
    {
        WorldState world = CreateOpenWorld(12, 12);
        ExplorationState state = new ExplorationState();
        CellId observed = new CellId(3, 3, 1);
        EntityId stack = EntityId.New();
        state.Recalculate(world.CreateSnapshot(), Sources(observed));
        state.ObserveMarkers(new[]
        {
            new LastKnownWorldItemMarker(stack, new ItemId("ore.iron"), observed, 7),
        });
        state.Recalculate(world.CreateSnapshot(), Sources(new CellId(11, 11, 1)));
        LastKnownWorldItemMarker marker = Assert.Single(state.Markers);
        Assert.Equal(observed, marker.Cell);
        Assert.Equal(7, marker.ObservedTick);
    }

    [Fact]
    public void Visibility_changes_report_only_affected_chunks()
    {
        WorldState world = CreateOpenWorld(24, 24);
        ExplorationState state = new ExplorationState();
        state.Recalculate(world.CreateSnapshot(), Sources(new CellId(2, 2, 1)));
        Assert.Contains(new ChunkId(0, 0, 1), state.DrainDirtyChunks());
        Assert.Empty(state.DrainDirtyChunks());
        state.Recalculate(world.CreateSnapshot(), Sources(new CellId(20, 20, 1)));
        IReadOnlyList<ChunkId> dirty = state.DrainDirtyChunks();
        Assert.Contains(new ChunkId(0, 0, 1), dirty);
        Assert.Contains(new ChunkId(5, 5, 1), dirty);
    }

    private static VisionSourceSnapshot[] Sources(CellId cell) => new[]
    {
        new VisionSourceSnapshot("resident", VisionSourceKind.Resident, new[] { cell }),
    };

    private static WorldState CreateOpenWorld(int width, int height)
    {
        MaterialCatalog catalog = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, false, 0), new MaterialDefinition(Rock, true, 10),
        });
        return WorldState.CreateFilled(new WorldSize(width, height), 4, catalog, Air, false).Value;
    }

    private static void SetCell(WorldState world, CellId cell, MaterialId material, long tick)
    {
        Assert.True(world.ApplyTerrainChanges(new[]
        {
            new TerrainChange(cell, world.GetCell(cell).Value.State.WithTerrain(material)),
        }, tick).IsSuccess);
    }

    private static void SetLayer(WorldState world, int y, MaterialId material, long tick)
    {
        List<TerrainChange> changes = new List<TerrainChange>();
        for (int z = 0; z < world.Size.Depth; z++)
        for (int x = 0; x < world.Size.Width; x++)
        {
            CellId cell = new CellId(x, y, z);
            changes.Add(new TerrainChange(cell, world.GetCell(cell).Value.State.WithTerrain(material)));
        }
        Assert.True(world.ApplyTerrainChanges(changes, tick).IsSuccess);
    }

    private static void SetColumn(WorldState world, int x, MaterialId material, long tick, int exceptY)
    {
        List<TerrainChange> changes = new List<TerrainChange>();
        for (int z = 0; z < world.Size.Depth; z++)
        for (int y = 0; y < world.Size.Height; y++) if (y != exceptY)
        {
            CellId cell = new CellId(x, y, z);
            changes.Add(new TerrainChange(cell, world.GetCell(cell).Value.State.WithTerrain(material)));
        }
        Assert.True(world.ApplyTerrainChanges(changes, tick).IsSuccess);
    }

    private static void SealOtherDepths(WorldState world, CellId doorway, long tick)
    {
        List<TerrainChange> changes = new List<TerrainChange>();
        for (int z = 0; z < world.Size.Depth; z++) if (z != doorway.Z)
        {
            CellId cell = new CellId(doorway.X, doorway.Y, z);
            changes.Add(new TerrainChange(cell, world.GetCell(cell).Value.State.WithTerrain(Rock)));
        }
        Assert.True(world.ApplyTerrainChanges(changes, tick).IsSuccess);
    }
}
}
