using Dig.Application.Buildings;
using Dig.Application.Saving;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class PackableBuildingExecutionSaveDataTests
{
    private static readonly EntityId OperationA =
        EntityId.Parse("86000000000000000000000000000001");
    private static readonly EntityId OperationB =
        EntityId.Parse("86000000000000000000000000000002");
    private static readonly EntityId Package =
        EntityId.Parse("86000000000000000000000000000003");
    private static readonly EntityId WorkerA =
        EntityId.Parse("86000000000000000000000000000004");
    private static readonly EntityId WorkerB =
        EntityId.Parse("86000000000000000000000000000005");
    private static readonly BuildingDefinitionId Definition =
        new BuildingDefinitionId("building.campfire");

    [Fact]
    public void Active_iteration_clock_and_attribution_round_trip()
    {
        PackableBuildingExecutionRegistry original =
            new PackableBuildingExecutionRegistry();
        Assert.True(original.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);
        Assert.True(original.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(original.CompleteIteration(OperationA, WorkerA).IsSuccess);
        Assert.True(original.Interrupt(OperationA).IsSuccess);
        Assert.True(original.StartOrResume(OperationA, WorkerB).IsSuccess);
        Assert.True(original.BeginIteration(
            OperationA,
            WorkerB,
            startTick: 120,
            durationSeconds: 420).IsSuccess);

        PackableBuildingExecutionsSaveData data =
            PackableBuildingExecutionSaveDataAdapter.Encode(original);
        Result<PackableBuildingExecutionRegistry> decoded =
            PackableBuildingExecutionSaveDataAdapter.Decode(data);

        Assert.True(decoded.IsSuccess, decoded.Error?.ToString());
        PackableBuildingExecutionSnapshot restored = Assert.Single(
            decoded.Value.CreateSnapshot());
        Assert.Equal(PackableBuildingExecutionStatus.Active, restored.Status);
        Assert.Equal(1, restored.CompletedIterations);
        Assert.Equal(WorkerB, restored.ActiveWorkerId);
        Assert.Equal(new[] { WorkerA }, restored.CompletedByWorkers);
        Assert.NotNull(restored.IterationClock);
        Assert.Equal(WorkerB, restored.IterationClock!.WorkerId);
        Assert.Equal(120, restored.IterationClock.StartTick);
        Assert.Equal(420, restored.IterationClock.DurationSeconds);
        Assert.False(decoded.Value.IsIterationReady(
            OperationA,
            WorkerB,
            tick: 539).Value);
        Assert.True(decoded.Value.IsIterationReady(
            OperationA,
            WorkerB,
            tick: 540).Value);
    }

    [Fact]
    public void Competing_active_operations_for_one_package_are_rejected()
    {
        PackableBuildingExecutionsSaveData data =
            new PackableBuildingExecutionsSaveData();
        data.Executions.Add(Active(OperationA, WorkerA));
        data.Executions.Add(Active(OperationB, WorkerB));

        Result<PackableBuildingExecutionRegistry> decoded =
            PackableBuildingExecutionSaveDataAdapter.Decode(data);

        Assert.True(decoded.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, decoded.Error);
    }

    [Fact]
    public void Completed_operation_releases_package_after_restore()
    {
        PackableBuildingExecutionRegistry original =
            new PackableBuildingExecutionRegistry();
        Assert.True(original.GetOrCreate(
            OperationA,
            Package,
            Definition,
            PackableBuildingOperationKind.Pack,
            totalIterations: 1).IsSuccess);
        Assert.True(original.StartOrResume(OperationA, WorkerA).IsSuccess);
        Assert.True(original.CompleteIteration(OperationA, WorkerA).IsSuccess);

        Result<PackableBuildingExecutionRegistry> decoded =
            PackableBuildingExecutionSaveDataAdapter.Decode(
                PackableBuildingExecutionSaveDataAdapter.Encode(original));
        Assert.True(decoded.IsSuccess);

        Assert.True(decoded.Value.GetOrCreate(
            OperationB,
            Package,
            Definition,
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);
    }

    private static PackableBuildingExecutionSaveData Active(
        EntityId operationId,
        EntityId workerId)
    {
        return new PackableBuildingExecutionSaveData
        {
            OperationId = operationId.ToString(),
            PackageId = Package.ToString(),
            DefinitionId = Definition.ToString(),
            Operation = (int)PackableBuildingOperationKind.Unpack,
            Status = (int)PackableBuildingExecutionStatus.Active,
            TotalIterations = 3,
            CompletedIterations = 0,
            ActiveWorkerId = workerId.ToString(),
        };
    }
}

}