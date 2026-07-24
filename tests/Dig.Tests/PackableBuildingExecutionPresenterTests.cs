using System.Linq;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using Xunit;

namespace Dig.Tests
{

public sealed class PackableBuildingExecutionPresenterTests
{
    private static readonly EntityId OperationA =
        EntityId.Parse("88000000000000000000000000000001");
    private static readonly EntityId OperationB =
        EntityId.Parse("88000000000000000000000000000002");
    private static readonly EntityId PackageA =
        EntityId.Parse("88000000000000000000000000000003");
    private static readonly EntityId PackageB =
        EntityId.Parse("88000000000000000000000000000004");
    private static readonly EntityId WorkerA =
        EntityId.Parse("88000000000000000000000000000005");
    private static readonly EntityId WorkerB =
        EntityId.Parse("88000000000000000000000000000006");

    [Fact]
    public void Active_unpack_projects_partial_visual_and_clamped_progress()
    {
        PackableBuildingExecutionRegistry executions = CreateRegistry(
            OperationA,
            PackageA,
            PackableBuildingOperationKind.Unpack);
        Assert.True(executions.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(executions.CompleteIteration(OperationA, WorkerA).IsSuccess);
        Assert.True(executions.BeginIteration(
            OperationA,
            WorkerA,
            startTick: 100,
            durationSeconds: 400).IsSuccess);

        PackableBuildingExecutionViewModel model = Assert.Single(
            CreatePresenter().Load(executions, tick: 300));

        Assert.Equal("visual.campfire.unpack.partial", model.VisualId);
        Assert.Equal("effect.campfire.iteration", model.IterationEffectId);
        Assert.Equal("building.packable.unpack.active", model.StatusLabelKey);
        Assert.Equal(1, model.CompletedIterations);
        Assert.Equal(2, model.CurrentIteration);
        Assert.Equal(200, model.ElapsedIterationSeconds);
        Assert.Equal(400, model.IterationDurationSeconds);
        Assert.Equal(5_000, model.IterationProgressBasisPoints);
        Assert.Equal(WorkerA.ToString(), model.ActiveWorkerId);
        Assert.True(model.IsIterationActive);
    }

    [Fact]
    public void Interrupted_operation_preserves_progress_without_active_clock()
    {
        PackableBuildingExecutionRegistry executions = CreateRegistry(
            OperationA,
            PackageA,
            PackableBuildingOperationKind.Unpack);
        Assert.True(executions.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(executions.CompleteIteration(OperationA, WorkerA).IsSuccess);
        Assert.True(executions.Interrupt(OperationA).IsSuccess);

        PackableBuildingExecutionViewModel model = Assert.Single(
            CreatePresenter().Load(executions, tick: 500));

        Assert.Equal(PackableBuildingExecutionStatus.Interrupted, model.Status);
        Assert.Equal("visual.campfire.unpack.partial", model.VisualId);
        Assert.Equal("building.packable.unpack.interrupted", model.StatusLabelKey);
        Assert.Equal(1, model.CompletedIterations);
        Assert.Null(model.ActiveWorkerId);
        Assert.False(model.IsIterationActive);
        Assert.True(model.IsInterrupted);
    }

    [Fact]
    public void Completed_pack_projects_world_box_visual()
    {
        PackableBuildingExecutionRegistry executions = CreateRegistry(
            OperationA,
            PackageA,
            PackableBuildingOperationKind.Pack,
            totalIterations: 1);
        Assert.True(executions.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(executions.CompleteIteration(OperationA, WorkerA).IsSuccess);

        PackableBuildingExecutionViewModel model = Assert.Single(
            CreatePresenter().Load(executions, tick: 500));

        Assert.Equal(PackableBuildingExecutionStatus.Completed, model.Status);
        Assert.Equal("visual.campfire.box.world", model.VisualId);
        Assert.Equal("building.packable.pack.completed", model.StatusLabelKey);
        Assert.True(model.IsTerminal);
    }

    [Fact]
    public void Projection_order_is_stable_by_package_then_operation()
    {
        PackableBuildingExecutionRegistry executions = new PackableBuildingExecutionRegistry();
        Assert.True(executions.GetOrCreate(
            OperationB,
            PackageB,
            CampfireBuildingBoxContent.CampfireBuildingId,
            PackableBuildingOperationKind.Pack,
            totalIterations: 3).IsSuccess);
        Assert.True(executions.GetOrCreate(
            OperationA,
            PackageA,
            CampfireBuildingBoxContent.CampfireBuildingId,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);

        string[] operationIds = CreatePresenter().Load(executions, tick: 0)
            .Select(value => value.OperationId)
            .ToArray();

        Assert.Equal(new[] { OperationA.ToString(), OperationB.ToString() }, operationIds);
    }

    private static PackableBuildingExecutionRegistry CreateRegistry(
        EntityId operationId,
        EntityId packageId,
        PackableBuildingOperationKind operation,
        int totalIterations = 3)
    {
        PackableBuildingExecutionRegistry registry =
            new PackableBuildingExecutionRegistry();
        Assert.True(registry.GetOrCreate(
            operationId,
            packageId,
            CampfireBuildingBoxContent.CampfireBuildingId,
            operation,
            totalIterations).IsSuccess);
        return registry;
    }

    private static PackableBuildingExecutionPresenter CreatePresenter()
    {
        return new PackableBuildingExecutionPresenter(
            CampfireBuildingBoxContent.Catalog,
            new PackableBuildingVisualCatalog(new[]
            {
                new PackableBuildingVisualProfile(
                    CampfireBuildingBoxContent.CampfireBuildingId,
                    activeBuildingVisualId: "visual.campfire.active",
                    worldBoxVisualId: "visual.campfire.box.world",
                    inventoryBoxVisualId: "visual.campfire.box.inventory",
                    plannedSiteVisualId: "visual.campfire.site.planned",
                    partialUnpackVisualId: "visual.campfire.unpack.partial",
                    partialPackVisualId: "visual.campfire.pack.partial",
                    iterationEffectId: "effect.campfire.iteration"),
            }));
    }
}

}