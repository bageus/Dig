using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{
public sealed class ExcavationRuntimeRegressionContractTests
{
    [Fact]
    public void Small_room_even_row_uses_right_biased_symmetric_tie_break()
    {
        CaveRoomPlan plan = Plan(CaveRoomPresetKind.Small);
        CellId[][] rows = FrontRows(plan);

        Assert.Equal(new[] { 5, 4, 3 }, rows.Select(row => row.Length));
        Assert.Equal(Enumerable.Range(8, 5), rows[0].Select(cell => cell.X));
        Assert.Equal(Enumerable.Range(9, 4), rows[1].Select(cell => cell.X));
        Assert.Equal(Enumerable.Range(9, 3), rows[2].Select(cell => cell.X));
        Assert.Equal(9, CaveRoomPlanner.ResolveRowMinX(plan.Preset, 10, level: 1));
    }

    [Fact]
    public void Medium_room_plans_and_projects_all_rows()
    {
        CaveRoomPlan plan = Plan(CaveRoomPresetKind.Medium);
        CellId[][] rows = FrontRows(plan);

        Assert.Equal(new[] { 8, 7, 6 }, rows.Select(row => row.Length));
        CaveTemplateTrimVolumeViewModel trims = new CaveTemplateTrimPresenter().Present(
            new[] { plan });
        CaveTemplateTrimInstanceViewModel instance = Assert.Single(trims.Instances);
        Assert.Equal("cave.template.medium", instance.TemplateId);
        Assert.Equal(new[] { 8, 7, 6 }, instance.Rows.Select(row => row.Width));
        Assert.Equal(new[] { 7, 7, 8 }, instance.Rows.Select(row => row.MinX));
    }

    [Fact]
    public void Unity_quarters_split_world_horizontal_and_vertical_axes_not_depth()
    {
        string runtime = RuntimeRoot();
        string visual = File.ReadAllText(Path.Combine(runtime, "DigCellVisual.cs"));

        Assert.Contains("ExcavationQuarterOffsets[index].x,\n                    0f,\n                    ExcavationQuarterOffsets[index].y", visual);
        Assert.Contains("new Vector3(0.486f, 1f, 0.486f)", visual);
        Assert.DoesNotContain("ExcavationQuarterOffsets[index].x,\n                    ExcavationQuarterOffsets[index].y,\n                    0f", visual);
        Assert.DoesNotContain("new Vector3(0.486f, 0.486f, 1f)", visual);
    }

    [Fact]
    public void Unsupported_mining_enters_climbing_without_provenance_gate()
    {
        string runtime = RuntimeRoot();
        string renderer = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentRenderer.WorkFacing.cs"));
        string visual = File.ReadAllText(Path.Combine(
            runtime,
            "DigAgentVisual.WorkFacing.cs"));

        Assert.Contains("RequiresClimbingWorkPose(", renderer);
        Assert.Contains("return hasToolWork && !isNonClimbingWork && !hasFullSupport;", renderer);
        Assert.DoesNotContain("tunnelVolume.IsVerticalTunnel(current)", renderer);
        Assert.DoesNotContain("targetRemovedSupport", renderer);

        int climbing = visual.IndexOf("if (_climbingWorkPose)", StringComparison.Ordinal);
        int movementWait = visual.IndexOf("if (_duration > 0f)", StringComparison.Ordinal);
        Assert.True(climbing >= 0 && movementWait > climbing);
    }

    private static CaveRoomPlan Plan(CaveRoomPresetKind kind)
    {
        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            CreateWorld(horizontalTunnelY: 9),
            new ExcavationBoundaryPolicy(20, 14, 2),
            kind,
            new CellId(10, 9));
        Assert.True(result.Succeeded, result.Detail);
        return result.Plan!;
    }

    private static CellId[][] FrontRows(CaveRoomPlan plan)
    {
        return plan.VolumeCells
            .Where(cell => cell.Z == 0)
            .GroupBy(cell => cell.Y)
            .OrderByDescending(group => group.Key)
            .Select(group => group.OrderBy(cell => cell.X).ToArray())
            .ToArray();
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
        world.ApplyTerrainChanges(changes, tick: 1);
        return world.CreateSnapshot();
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
