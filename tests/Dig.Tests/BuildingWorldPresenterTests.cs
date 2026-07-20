using System;
using Dig.Application.Buildings;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingWorldPresenterTests
{
    private static readonly ItemId BoxItemId = new ItemId("building_box.world_presenter");

    [Fact]
    public void Active_buildings_are_sorted_and_removed_buildings_are_not_rendered()
    {
        BuildingWorldPresenter presenter = new BuildingWorldPresenter(
            new BuildingFunctionsPresenter());
        BuildingSnapshot later = Snapshot(Id(2), BuildingStatus.Completed);
        BuildingSnapshot removed = Snapshot(Id(3), BuildingStatus.Removed);
        BuildingSnapshot first = Snapshot(Id(1), BuildingStatus.Completed);

        var models = presenter.Load(new[] { later, removed, first });

        Assert.Equal(2, models.Count);
        Assert.Equal(Id(1).ToString(), models[0].Id);
        Assert.Equal(Id(2).ToString(), models[1].Id);
        Assert.All(models, model => Assert.True(model.IsSelectable));
        Assert.Equal("Box Workshop", models[0].Name);
        Assert.Single(models[0].Footprint);
        Assert.True(models[0].Functions.Actions[0].IsEnabled);
    }

    [Fact]
    public void Projection_preserves_orientation_work_position_and_assembly_progress()
    {
        BuildingWorldPresenter presenter = new BuildingWorldPresenter(
            new BuildingFunctionsPresenter());
        BuildingSnapshot source = Snapshot(
            Id(1),
            BuildingStatus.UnderConstruction,
            orientation: BuildingOrientation.East,
            completedWork: 1);

        BuildingWorldViewModel model = Assert.Single(presenter.Load(new[] { source }));

        Assert.Equal(BuildingOrientation.East, model.Orientation);
        Assert.Equal(source.WorkPosition.X, model.WorkPositionX);
        Assert.Equal(source.WorkPosition.Y, model.WorkPositionY);
        Assert.Equal(1d / 3d, model.AssemblyProgress, precision: 6);
        Assert.Equal(BuildingVisualState.Assembly, model.VisualState);
    }

    [Fact]
    public void Packing_state_overrides_completed_visual_state()
    {
        BuildingPackingPlanSnapshot active = new BuildingPackingPlanSnapshot(
            Id(20),
            Id(21),
            completedWork: 1,
            commitState: BuildingPackingCommitState.Active);
        BuildingWorldPresenter presenter = new BuildingWorldPresenter(
            new BuildingFunctionsPresenter());

        BuildingWorldViewModel model = Assert.Single(presenter.Load(new[]
        {
            Snapshot(Id(1), BuildingStatus.Completed, active),
        }));

        Assert.Equal(BuildingVisualState.Packing, model.VisualState);
        Assert.Equal(0.5d, model.Functions.PackingProgress, precision: 6);
    }

    [Theory]
    [InlineData(BuildingStatus.AwaitingBox, BuildingVisualState.BuildingBox)]
    [InlineData(BuildingStatus.Damaged, BuildingVisualState.Damaged)]
    [InlineData(BuildingStatus.Completed, BuildingVisualState.Completed)]
    public void Status_maps_to_stable_visual_state(
        BuildingStatus status,
        BuildingVisualState expected)
    {
        BuildingWorldPresenter presenter = new BuildingWorldPresenter(
            new BuildingFunctionsPresenter());

        BuildingWorldViewModel model = Assert.Single(presenter.Load(new[]
        {
            Snapshot(Id(1), status),
        }));

        Assert.Equal(expected, model.VisualState);
    }

    [Fact]
    public void Enabled_pack_action_dispatches_exactly_one_typed_command()
    {
        RecordingPackingHandler handler = new RecordingPackingHandler();
        BuildingFunctionsCommandAdapter adapter = new BuildingFunctionsCommandAdapter(
            new BuildingFunctionsPresenter(),
            handler);
        EntityId jobId = Id(10);
        EntityId outputId = Id(11);

        Result result = adapter.StartPacking(
            Snapshot(Id(1), BuildingStatus.Completed),
            jobId,
            outputId,
            priority: 640,
            tick: 77);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(handler.LastCommand);
        Assert.Equal(Id(1), handler.LastCommand!.BuildingId);
        Assert.Equal(jobId, handler.LastCommand.JobId);
        Assert.Equal(outputId, handler.LastCommand.OutputStackId);
        Assert.Equal(640, handler.LastCommand.Priority);
        Assert.Equal(77, handler.LastCommand.Tick);
    }

    [Fact]
    public void Disabled_pack_action_emits_no_application_command()
    {
        RecordingPackingHandler handler = new RecordingPackingHandler();
        BuildingFunctionsCommandAdapter adapter = new BuildingFunctionsCommandAdapter(
            new BuildingFunctionsPresenter(),
            handler);
        BuildingPackingPlanSnapshot active = new BuildingPackingPlanSnapshot(
            Id(20),
            Id(21),
            completedWork: 0,
            commitState: BuildingPackingCommitState.Active);

        Result result = adapter.StartPacking(
            Snapshot(Id(1), BuildingStatus.Completed, active),
            Id(22),
            Id(23),
            priority: 500,
            tick: 8);

        Assert.True(result.IsFailure);
        Assert.Equal(BuildingFunctionsCommandErrors.ActionUnavailable, result.Error);
        Assert.Equal(0, handler.CallCount);
    }

    private static BuildingSnapshot Snapshot(
        EntityId id,
        BuildingStatus status,
        BuildingPackingPlanSnapshot? packingPlan = null,
        BuildingOrientation orientation = BuildingOrientation.North,
        int? completedWork = null)
    {
        BuildingDefinition definition = new BuildingDefinition(
            new BuildingDefinitionId("box.world_presenter"),
            "Box Workshop",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 100,
            boxPolicy: new BuildingBoxPolicy(BoxItemId, packingWork: 2));
        CellId origin = new CellId(id == Id(1) ? 2 : 5, 4);
        return new BuildingSnapshot(
            id,
            definition,
            origin,
            orientation,
            definition.ResolveFootprint(origin, orientation),
            definition.ResolveWorkPositions(origin, orientation)[0],
            status,
            completedWork ?? definition.RequiredWork,
            durability: definition.MaximumDurability,
            version: 1,
            diagnosticReason: null,
            boxPlan: new BuildingBoxPlanSnapshot(
                Id(30 + origin.X),
                Id(40 + origin.X),
                BuildingBoxCommitState.Consumed),
            packingPlan: packingPlan);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private sealed class RecordingPackingHandler
        : ICommandHandler<StartBuildingBoxPackingCommand, Result>
    {
        public int CallCount { get; private set; }

        public StartBuildingBoxPackingCommand? LastCommand { get; private set; }

        public Result Handle(StartBuildingBoxPackingCommand command)
        {
            CallCount++;
            LastCommand = command;
            return Result.Success();
        }
    }
}
}
