using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class PackableBuildingExecutionStateTests
{
    private static readonly EntityId OperationId =
        EntityId.Parse("83000000000000000000000000000001");
    private static readonly EntityId PackageId =
        EntityId.Parse("83000000000000000000000000000002");
    private static readonly EntityId WorkerA =
        EntityId.Parse("83000000000000000000000000000003");
    private static readonly EntityId WorkerB =
        EntityId.Parse("83000000000000000000000000000004");

    [Fact]
    public void Interrupted_execution_resumes_with_another_worker_without_resetting_iterations()
    {
        PackableBuildingExecutionState state = Create();

        Assert.True(state.Start(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);
        Assert.True(state.Interrupt().IsSuccess);
        Assert.True(state.Start(WorkerB).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerB).IsSuccess);

        PackableBuildingExecutionSnapshot snapshot = state.CreateSnapshot();
        Assert.Equal(PackableBuildingExecutionStatus.Completed, snapshot.Status);
        Assert.Equal(3, snapshot.CompletedIterations);
        Assert.Equal(new[] { WorkerA, WorkerA, WorkerB }, snapshot.CompletedByWorkers);
        Assert.Null(snapshot.ActiveWorkerId);
    }

    [Fact]
    public void Stale_worker_cannot_complete_after_resume_by_another_worker()
    {
        PackableBuildingExecutionState state = Create();
        Assert.True(state.Start(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);
        Assert.True(state.Interrupt().IsSuccess);
        Assert.True(state.Start(WorkerB).IsSuccess);

        Result stale = state.CompleteIteration(WorkerA);

        Assert.True(stale.IsFailure);
        Assert.Equal(1, state.CompletedIterations);
        Assert.Equal(WorkerB, state.ActiveWorkerId);
    }

    [Fact]
    public void Completion_is_exactly_once_and_cannot_be_replayed()
    {
        PackableBuildingExecutionState state = Create();
        Assert.True(state.Start(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);

        Result replay = state.CompleteIteration(WorkerA);

        Assert.True(replay.IsFailure);
        Assert.Equal(3, state.CompletedIterations);
        Assert.Equal(PackableBuildingExecutionStatus.Completed, state.Status);
    }

    [Fact]
    public void Cancelled_execution_preserves_completed_iteration_history()
    {
        PackableBuildingExecutionState state = Create();
        Assert.True(state.Start(WorkerA).IsSuccess);
        Assert.True(state.CompleteIteration(WorkerA).IsSuccess);

        Assert.True(state.Cancel().IsSuccess);

        PackableBuildingExecutionSnapshot snapshot = state.CreateSnapshot();
        Assert.Equal(PackableBuildingExecutionStatus.Cancelled, snapshot.Status);
        Assert.Equal(1, snapshot.CompletedIterations);
        Assert.Equal(new[] { WorkerA }, snapshot.CompletedByWorkers);
    }

    private static PackableBuildingExecutionState Create()
    {
        return new PackableBuildingExecutionState(
            OperationId,
            PackageId,
            new BuildingDefinitionId("building.campfire"),
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3);
    }
}

}
