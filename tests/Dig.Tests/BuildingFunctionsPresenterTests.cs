using System;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingFunctionsPresenterTests
{
    private static readonly EntityId BuildingId = Id(1);
    private static readonly EntityId AssemblyStackId = Id(2);
    private static readonly EntityId AssemblyJobId = Id(3);
    private static readonly EntityId PackingJobId = Id(4);
    private static readonly EntityId OutputStackId = Id(5);
    private static readonly ItemId BoxItemId = new ItemId("building_box.presenter");

    [Fact]
    public void Completed_box_building_enables_pack_and_creates_typed_command()
    {
        BuildingFunctionsPresenter presenter = new BuildingFunctionsPresenter();
        BuildingFunctionsViewModel model = presenter.Present(
            Snapshot(BuildingStatus.Completed));

        BuildingFunctionActionViewModel action = Assert.Single(model.Actions);
        Assert.Equal(BuildingFunctionActionKind.Pack, action.Kind);
        Assert.Equal(BuildingFunctionsPresenter.PackLabelKey, action.LabelKey);
        Assert.True(action.IsEnabled);
        Assert.Null(action.DisabledReasonCode);
        Assert.False(model.IsPacking);
        Assert.Equal(0d, model.PackingProgress);

        Assert.True(presenter.TryCreatePackingDraft(
            model,
            PackingJobId,
            OutputStackId,
            priority: 650,
            tick: 42,
            out BuildingPackingCommandDraft? draft));
        StartBuildingBoxPackingCommand command = presenter.CreateCommand(draft!);
        Assert.Equal(BuildingId, command.BuildingId);
        Assert.Equal(PackingJobId, command.JobId);
        Assert.Equal(OutputStackId, command.OutputStackId);
        Assert.Equal(650, command.Priority);
        Assert.Equal(42, command.Tick);
    }

    [Fact]
    public void Active_packing_disables_duplicate_action_and_exposes_progress()
    {
        BuildingFunctionsPresenter presenter = new BuildingFunctionsPresenter();
        BuildingPackingPlanSnapshot packing = new BuildingPackingPlanSnapshot(
            PackingJobId,
            OutputStackId,
            completedWork: 1,
            BuildingPackingCommitState.Active);

        BuildingFunctionsViewModel model = presenter.Present(
            Snapshot(BuildingStatus.Completed, packingPlan: packing));

        BuildingFunctionActionViewModel action = Assert.Single(model.Actions);
        Assert.False(action.IsEnabled);
        Assert.Equal(BuildingFunctionsPresenter.PackingActiveReason, action.DisabledReasonCode);
        Assert.True(model.IsPacking);
        Assert.Equal(1, model.PackingCompletedWork);
        Assert.Equal(2, model.PackingRequiredWork);
        Assert.Equal(0.5d, model.PackingProgress);
        Assert.False(presenter.TryCreatePackingDraft(
            model,
            Id(6),
            Id(7),
            priority: 1,
            tick: 1,
            out BuildingPackingCommandDraft? draft));
        Assert.Null(draft);
    }

    [Theory]
    [InlineData(BuildingStatus.AwaitingBox, BuildingFunctionsPresenter.BuildingIncompleteReason)]
    [InlineData(BuildingStatus.ReadyToBuild, BuildingFunctionsPresenter.BuildingIncompleteReason)]
    [InlineData(BuildingStatus.UnderConstruction, BuildingFunctionsPresenter.BuildingIncompleteReason)]
    [InlineData(BuildingStatus.ReadyToComplete, BuildingFunctionsPresenter.BuildingIncompleteReason)]
    [InlineData(BuildingStatus.Damaged, BuildingFunctionsPresenter.BuildingDamagedReason)]
    [InlineData(BuildingStatus.Cancelled, BuildingFunctionsPresenter.BuildingInactiveReason)]
    [InlineData(BuildingStatus.Removed, BuildingFunctionsPresenter.BuildingInactiveReason)]
    public void Non_completed_building_reports_typed_pack_reason(
        BuildingStatus status,
        string reason)
    {
        BuildingFunctionsViewModel model = new BuildingFunctionsPresenter().Present(
            Snapshot(status));

        BuildingFunctionActionViewModel action = Assert.Single(model.Actions);
        Assert.False(action.IsEnabled);
        Assert.Equal(reason, action.DisabledReasonCode);
    }

    [Fact]
    public void Legacy_material_building_has_no_pack_action_contract()
    {
        BuildingDefinition definition = new BuildingDefinition(
            new BuildingDefinitionId("legacy.presenter"),
            "Legacy",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            new[] { new BuildingMaterialRequirement(new ItemId("wood"), 1) },
            requiredWork: 3,
            maximumDurability: 100);
        BuildingSnapshot snapshot = CreateSnapshot(
            definition,
            BuildingStatus.Completed,
            durability: 100,
            packingPlan: null);

        BuildingFunctionsViewModel model = new BuildingFunctionsPresenter().Present(snapshot);

        BuildingFunctionActionViewModel action = Assert.Single(model.Actions);
        Assert.False(action.IsEnabled);
        Assert.Equal(BuildingFunctionsPresenter.PackingDisabledReason, action.DisabledReasonCode);
        Assert.Equal(0, model.PackingRequiredWork);
    }

    [Fact]
    public void Cancelled_packing_history_does_not_look_active_and_allows_retry()
    {
        BuildingPackingPlanSnapshot cancelled = new BuildingPackingPlanSnapshot(
            PackingJobId,
            OutputStackId,
            completedWork: 1,
            BuildingPackingCommitState.Cancelled);

        BuildingFunctionsViewModel model = new BuildingFunctionsPresenter().Present(
            Snapshot(BuildingStatus.Completed, packingPlan: cancelled));

        Assert.False(model.IsPacking);
        Assert.Equal(0, model.PackingCompletedWork);
        Assert.Equal(0d, model.PackingProgress);
        Assert.True(Assert.Single(model.Actions).IsEnabled);
    }

    private static BuildingSnapshot Snapshot(
        BuildingStatus status,
        BuildingPackingPlanSnapshot? packingPlan = null)
    {
        BuildingDefinition definition = new BuildingDefinition(
            new BuildingDefinitionId("box.presenter"),
            "Box Building",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 100,
            new BuildingBoxPolicy(BoxItemId, packingWork: 2));
        int durability = status switch
        {
            BuildingStatus.Completed => 100,
            BuildingStatus.Damaged => 50,
            _ => 0,
        };
        return CreateSnapshot(definition, status, durability, packingPlan);
    }

    private static BuildingSnapshot CreateSnapshot(
        BuildingDefinition definition,
        BuildingStatus status,
        int durability,
        BuildingPackingPlanSnapshot? packingPlan)
    {
        CellId origin = new CellId(3, 3);
        return new BuildingSnapshot(
            BuildingId,
            definition,
            origin,
            BuildingOrientation.North,
            definition.ResolveFootprint(origin, BuildingOrientation.North),
            new CellId(3, 2),
            status,
            completedWork: status is BuildingStatus.ReadyToComplete
                or BuildingStatus.Completed
                or BuildingStatus.Damaged
                or BuildingStatus.Removed
                ? definition.RequiredWork
                : 0,
            durability,
            version: 1,
            diagnosticReason: null,
            definition.BoxPolicy is null
                ? null
                : new BuildingBoxPlanSnapshot(
                    AssemblyStackId,
                    AssemblyJobId,
                    BuildingBoxCommitState.Consumed),
            packingPlan);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
