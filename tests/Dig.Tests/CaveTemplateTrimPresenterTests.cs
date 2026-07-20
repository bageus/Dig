using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveTemplateTrimPresenterTests
{
    [Fact]
    public void Free_excavation_without_completed_plans_has_no_template_trim()
    {
        CaveTemplateTrimVolumeViewModel result =
            new CaveTemplateTrimPresenter().Present(Array.Empty<CaveRoomPlan>());

        Assert.Empty(result.Instances);
        Assert.Equal(0L, result.Version);
    }

    [Theory]
    [InlineData(CaveRoomPresetKind.Small, "cave.template.small")]
    [InlineData(CaveRoomPresetKind.Medium, "cave.template.medium")]
    [InlineData(CaveRoomPresetKind.Large, "cave.template.large")]
    [InlineData(CaveRoomPresetKind.Tall, "cave.template.tall")]
    public void Completed_plan_projects_stable_template_provenance_and_layout(
        CaveRoomPresetKind kind,
        string expectedTemplateId)
    {
        CaveRoomPlan plan = CreatePlan(kind, entranceX: 16);

        CaveTemplateTrimInstanceViewModel instance = Assert.Single(
            new CaveTemplateTrimPresenter().Present(new[] { plan }).Instances);

        Assert.Equal(expectedTemplateId, instance.TemplateId);
        Assert.Equal(kind, instance.Kind);
        Assert.Equal(plan.Entrance, instance.Entrance);
        Assert.Equal(plan.Preset.Depth, instance.Depth);
        Assert.True(instance.HasBackWall);
        Assert.InRange(instance.Variant, (byte)0, (byte)3);
        Assert.Equal(plan.Preset.Height, instance.Rows.Count);
        Assert.Equal(
            Enumerable.Range(1, plan.Preset.Depth - 1),
            instance.ArchDepths);
        Assert.Equal(
            Enumerable.Range(0, plan.Preset.Height)
                .Select(level => CaveRoomPlanner.InterpolateWidth(plan.Preset, level)),
            instance.Rows.Select(row => row.Width));
    }

    [Fact]
    public void Projection_is_deterministic_for_reordered_completed_plans()
    {
        CaveRoomPlan left = CreatePlan(CaveRoomPresetKind.Small, entranceX: 9);
        CaveRoomPlan right = CreatePlan(CaveRoomPresetKind.Tall, entranceX: 23);
        CaveTemplateTrimPresenter presenter = new CaveTemplateTrimPresenter();

        CaveTemplateTrimVolumeViewModel ordered = presenter.Present(
            new[] { left, right });
        CaveTemplateTrimVolumeViewModel reversed = presenter.Present(
            new[] { right, left });

        Assert.Equal(ordered.Version, reversed.Version);
        Assert.Equal(
            ordered.Instances.Select(instance => instance.InstanceId),
            reversed.Instances.Select(instance => instance.InstanceId));
        Assert.Equal(
            ordered.Instances.Select(instance => instance.Variant),
            reversed.Instances.Select(instance => instance.Variant));
    }

    [Fact]
    public void Duplicate_completed_plan_is_rejected()
    {
        CaveRoomPlan plan = CreatePlan(CaveRoomPresetKind.Medium, entranceX: 16);

        Assert.Throws<ArgumentException>(() =>
            new CaveTemplateTrimPresenter().Present(new[] { plan, plan }));
    }

    [Fact]
    public void Row_coordinates_follow_the_completed_room_trapezoid()
    {
        CaveRoomPlan plan = CreatePlan(CaveRoomPresetKind.Small, entranceX: 16);
        CaveTemplateTrimInstanceViewModel instance = Assert.Single(
            new CaveTemplateTrimPresenter().Present(new[] { plan }).Instances);

        Assert.Equal(new[] { 5, 4, 3 }, instance.Rows.Select(row => row.Width));
        Assert.Equal(new[] { 14, 13, 12 }, instance.Rows.Select(row => row.Y));
        Assert.Equal(new[] { 14, 15, 15 }, instance.Rows.Select(row => row.MinX));
    }

    private static CaveRoomPlan CreatePlan(
        CaveRoomPresetKind kind,
        int entranceX)
    {
        const int entranceY = 14;
        WorldSnapshot world = CreateWorld(entranceY);
        CaveRoomPlanResult result = new CaveRoomPlanner().Plan(
            world,
            new ExcavationBoundaryPolicy(32, 22, 2),
            kind,
            new CellId(entranceX, entranceY));
        Assert.True(result.Succeeded, result.Detail);
        return result.Plan!;
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
            new WorldSize(32, 22),
            chunkSize: 4,
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
        for (int x = 1; x < 31; x++)
        {
            changes.Add(new TerrainChange(
                new CellId(x, horizontalTunnelY),
                empty));
        }

        world.ApplyTerrainChanges(changes, tick: 1);
        return world.CreateSnapshot();
    }
}

}
