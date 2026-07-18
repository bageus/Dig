using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveRoomPlanningTests
{
    [Theory]
    [InlineData(CaveRoomPresetKind.Small, 5, 3, 3, 3)]
    [InlineData(CaveRoomPresetKind.Medium, 7, 3, 4, 3)]
    [InlineData(CaveRoomPresetKind.Large, 9, 5, 4, 5)]
    [InlineData(CaveRoomPresetKind.Tall, 8, 4, 4, 6)]
    public void Catalog_preserves_documented_room_dimensions(
        CaveRoomPresetKind kind,
        int baseWidth,
        int topWidth,
        int depth,
        int height)
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);

        Assert.Equal(baseWidth, preset.BaseWidth);
        Assert.Equal(topWidth, preset.TopWidth);
        Assert.Equal(depth, preset.Depth);
        Assert.Equal(height, preset.Height);
    }

    [Fact]
    public void Small_room_is_a_three_deep_trapezoid_anchored_to_tunnel_row()
    {
        WorldSnapshot world = CreateWorld(horizontalTunnelY: 9);
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);

        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            world,
            boundary,
            CaveRoomPresetKind.Small,
            new CellId(10, 9));

        Assert.True(result.Succeeded, result.Detail);
        CaveRoomPlan plan = result.Plan!;
        Assert.Contains(new SpatialCellId(10, 9, 0), plan.VolumeCells);
        Assert.Contains(new SpatialCellId(10, 9, 2), plan.VolumeCells);
        Assert.Equal(36, plan.VolumeCells.Count);
        Assert.Equal(new[] { 5, 4, 3 }, RowWidths(plan));
        Assert.Equal(3, plan.RoofCells.Count);
        Assert.DoesNotContain(new CellId(10, 9), plan.FrontExcavationCells);
    }

    [Fact]
    public void Tall_room_leaves_the_protected_upper_rock_row_as_its_roof()
    {
        WorldSnapshot world = CreateWorld(horizontalTunnelY: 8);
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);

        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            world,
            boundary,
            CaveRoomPresetKind.Tall,
            new CellId(10, 8));

        Assert.True(result.Succeeded, result.Detail);
        Assert.All(result.Plan!.RoofCells, cell => Assert.Equal(2, cell.Y));
        Assert.Equal(new[] { 8, 7, 6, 6, 5, 4 }, RowWidths(result.Plan));
    }

    [Fact]
    public void Room_requires_an_open_horizontal_tunnel_entrance()
    {
        WorldSnapshot world = CreateWorld(horizontalTunnelY: null, verticalTunnelX: 10);
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);

        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            world,
            boundary,
            CaveRoomPresetKind.Small,
            new CellId(10, 9));

        Assert.False(result.Succeeded);
        Assert.Equal(
            CaveRoomPlanFailureReason.EntranceNotHorizontalTunnel,
            result.FailureReason);
    }

    [Fact]
    public void Room_rejects_missing_roof_rock()
    {
        WorldSnapshot world = CreateWorld(
            horizontalTunnelY: 9,
            additionalAir: new[] { new CellId(10, 6) });
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);

        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            world,
            boundary,
            CaveRoomPresetKind.Small,
            new CellId(10, 9));

        Assert.False(result.Succeeded);
        Assert.Equal(CaveRoomPlanFailureReason.MissingRoof, result.FailureReason);
    }

    [Fact]
    public void Room_rejects_protected_edge_cells()
    {
        WorldSnapshot world = CreateWorld(horizontalTunnelY: 9);
        ExcavationBoundaryPolicy boundary = new ExcavationBoundaryPolicy(20, 14, 2);

        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            world,
            boundary,
            CaveRoomPresetKind.Small,
            new CellId(2, 9));

        Assert.False(result.Succeeded);
        Assert.Equal(CaveRoomPlanFailureReason.ProtectedRock, result.FailureReason);
    }

    private static int[] RowWidths(CaveRoomPlan plan)
    {
        return plan.VolumeCells
            .Where(cell => cell.Z == 0)
            .GroupBy(cell => cell.Y)
            .OrderByDescending(group => group.Key)
            .Select(group => group.Count())
            .ToArray();
    }

    private static WorldSnapshot CreateWorld(
        int? horizontalTunnelY,
        int? verticalTunnelX = null,
        IReadOnlyCollection<CellId>? additionalAir = null)
    {
        MaterialId rock = new MaterialId("test.rock");
        MaterialId air = new MaterialId("test.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
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
        List<TerrainChange> changes = new List<TerrainChange>();
        if (horizontalTunnelY.HasValue)
        {
            for (int x = 1; x < 19; x++)
            {
                changes.Add(new TerrainChange(
                    new CellId(x, horizontalTunnelY.Value),
                    empty));
            }
        }

        if (verticalTunnelX.HasValue)
        {
            for (int y = 4; y <= 10; y++)
            {
                changes.Add(new TerrainChange(
                    new CellId(verticalTunnelX.Value, y),
                    empty));
            }
        }

        if (additionalAir != null)
        {
            foreach (CellId cell in additionalAir)
            {
                changes.Add(new TerrainChange(cell, empty));
            }
        }

        if (changes.Count > 0)
        {
            world.ApplyTerrainChanges(changes, tick: 1);
        }

        return world.CreateSnapshot();
    }
}

}
