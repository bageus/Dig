using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveRoomCenteredProfileTests
{
    [Fact]
    public void Small_room_is_a_three_deep_centered_trapezoid()
    {
        CaveRoomPlanResult result = Plan(CaveRoomPresetKind.Small);

        Assert.True(result.Succeeded, result.Detail);
        CaveRoomPlan plan = result.Plan!;
        Assert.Contains(new CellId(10, 9, 0), plan.VolumeCells);
        Assert.Contains(new CellId(10, 9, 1), plan.VolumeCells);
        Assert.Contains(new CellId(10, 9, 2), plan.VolumeCells);
        Assert.Equal(39, plan.VolumeCells.Count);
        Assert.Equal(34, plan.ExcavationCells.Count);
        Assert.Equal(5, plan.BaseTunnelCells.Count);
        Assert.Equal(new[] { 5, 4, 3 }, ProfileWidths(plan));
        Assert.Equal(
            ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight,
            Required(plan, new CellId(8, 8, 0)));
        Assert.Equal(
            ExcavationQuarter.UpperLeft | ExcavationQuarter.LowerLeft,
            Required(plan, new CellId(12, 8, 0)));
        Assert.Equal(3, plan.RoofCells.Count);
        Assert.DoesNotContain(new CellId(10, 9), plan.FrontExcavationCells);
        Assert.DoesNotContain(plan.BaseTunnelCells, plan.ExcavationCells.Contains);
    }

    [Fact]
    public void Small_room_protects_both_remaining_half_cell_side_shells()
    {
        CaveRoomPlan plan = Plan(CaveRoomPresetKind.Small).Plan!;

        IReadOnlyList<CellId> shell = new CaveRoomShellProtectionPolicy().Resolve(
            plan,
            new WorldSize(20, 14));

        Assert.Contains(new CellId(8, 8), shell);
        Assert.Contains(new CellId(12, 8), shell);
        Assert.DoesNotContain(new CellId(13, 8), shell);
    }

    [Fact]
    public void Medium_room_uses_centered_seven_wide_middle_profile()
    {
        CaveRoomPlan plan = Plan(CaveRoomPresetKind.Medium).Plan!;
        CaveRoomRowProfile middle = CaveRoomPlanner.ResolveRowProfile(
            plan.Preset,
            plan.Entrance.X,
            level: 1);

        Assert.Equal(7, middle.Width);
        Assert.Equal(7, middle.MinCellX);
        Assert.Equal(14, middle.MaxCellX);
        Assert.Equal(
            ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight,
            middle.RequiredQuarters(7));
        Assert.Equal(
            ExcavationQuarter.UpperLeft | ExcavationQuarter.LowerLeft,
            middle.RequiredQuarters(14));
    }

    private static CaveRoomPlanResult Plan(CaveRoomPresetKind kind)
    {
        return new CaveRoomPlanner().Plan(
            CreateWorld(horizontalTunnelY: 9),
            new ExcavationBoundaryPolicy(20, 14, 2),
            kind,
            new CellId(10, 9));
    }

    private static int[] ProfileWidths(CaveRoomPlan plan)
    {
        return Enumerable.Range(0, plan.Preset.Height)
            .Select(level => CaveRoomPlanner.ResolveRowProfile(
                plan.Preset,
                plan.Entrance.X,
                level).Width)
            .ToArray();
    }

    private static ExcavationQuarter Required(CaveRoomPlan plan, CellId cell)
    {
        Assert.True(plan.TryGetExcavationTarget(
            cell,
            out CaveRoomExcavationTarget target));
        return target.RequiredQuarters;
    }

    private static WorldSnapshot CreateWorld(int horizontalTunnelY)
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
        List<TerrainChange> changes = Enumerable.Range(1, 18)
            .Select(x => new TerrainChange(
                new CellId(x, horizontalTunnelY),
                empty))
            .ToList();
        Assert.True(world.ApplyTerrainChanges(changes, tick: 1).IsSuccess);
        return world.CreateSnapshot();
    }
}

}
