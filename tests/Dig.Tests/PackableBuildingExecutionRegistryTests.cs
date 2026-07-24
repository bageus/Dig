using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class PackableBuildingExecutionRegistryTests
{
    private static readonly EntityId OperationA =
        EntityId.Parse("84000000000000000000000000000001");
    private static readonly EntityId OperationB =
        EntityId.Parse("84000000000000000000000000000002");
    private static readonly EntityId Package =
        EntityId.Parse("84000000000000000000000000000003");
    private static readonly EntityId WorkerA =
        EntityId.Parse("84000000000000000000000000000004");
    private static readonly EntityId WorkerB =
        EntityId.Parse("84000000000000000000000000000005");
    private static readonly BuildingDefinitionId Definition =
        new BuildingDefinitionId("building.campfire");

    [Fact]
    public void Same_operation_is_idempotently_reused()
    {
        PackableBuildingExecutionRegistry registry = new PackableBuildingExecutionRegistry();

        Result<PackableBuildingExecutionState> first = registry.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3);
        Result<PackableBuildingExecutionState> second = registry.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Same(first.Value, second.Value);
    }

    [Fact]
    public void Concurrent_operation_for_same_package_is_rejected()
    {
        PackableBuildingExecutionRegistry registry = new PackableBuildingExecutionRegistry();
        Assert.True(registry.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);

        Result<PackableBuildingExecutionState> competing = registry.GetOrCreate(
            OperationB,
            Package,
            Definition,
            PackableBuildingOperationKind.Pack,
            totalIterations: 3);

        Assert.True(competing.IsFailure);
        Assert.Equal(BuildingBoxErrors.JobAlreadyExists, competing.Error);
    }

    [Fact]
    public void Interrupted_operation_resumes_with_new_worker_and_rejects_stale_worker()
    {
        PackableBuildingExecutionRegistry registry = new PackableBuildingExecutionRegistry();
        Assert.True(registry.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);
        Assert.True(registry.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(registry.CompleteIteration(OperationA, WorkerA).IsSuccess);
        Assert.True(registry.Interrupt(OperationA).IsSuccess);
        Assert.True(registry.StartOrResume(OperationA, WorkerB).IsSuccess);

        Result stale = registry.CompleteIteration(OperationA, WorkerA);
        Result resumed = registry.CompleteIteration(OperationA, WorkerB);

        Assert.True(stale.IsFailure);
        Assert.True(resumed.IsSuccess);
        Assert.Equal(2, registry.Get(OperationA)!.CompletedIterations);
    }

    [Fact]
    public void Completed_operation_releases_package_for_next_direction()
    {
        PackableBuildingExecutionRegistry registry = new PackableBuildingExecutionRegistry();
        Assert.True(registry.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 1).IsSuccess);
        Assert.True(registry.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(registry.CompleteIteration(OperationA, WorkerA).IsSuccess);

        Result<PackableBuildingExecutionState> next = registry.GetOrCreate(
            OperationB,
            Package,
            Definition,
            PackableBuildingOperationKind.Pack,
            totalIterations: 3);

        Assert.True(next.IsSuccess);
    }
}

}
