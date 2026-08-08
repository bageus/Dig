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
    public void Resident_uses_graph_radius_ten()
    {
        WorldState world = CreateOpenWorld(24, 24);
        ExplorationState exploration = new ExplorationState();
        exploration.Recalculate(world.CreateSnapshot(), Sources(new CellId(11, 11, 1)));
        Assert.Equal(CellVisibility.Visible, exploration.GetVisibility(new CellId(21, 11, 1)));
        Assert.Equal(CellVisibility.Unexplored, exploration.GetVisibility(new CellId(22, 11, 1)));
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
}
}
