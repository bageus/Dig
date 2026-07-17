using System;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobActionDispatcherTests
{
    [Fact]
    public void Dispatcher_routes_action_to_registered_handler()
    {
        string? dispatchedJobId = null;
        JobActionViewModel? dispatchedAction = null;
        JobActionViewModel action = new JobActionViewModel(
            JobActionKind.PrepareSuggestedTool,
            "Equip suggested tool",
            isEnabled: false,
            disabledReasonCode: "jobs.tool_reservation_missing",
            disabledReasonMessage: "The suggested tool is no longer reserved.");
        JobActionDispatcher dispatcher = new JobActionDispatcher(new[]
        {
            new JobActionRoute(
                JobActionKind.PrepareSuggestedTool,
                (jobId, value) =>
                {
                    dispatchedJobId = jobId;
                    dispatchedAction = value;
                }),
        });

        dispatcher.Dispatch(" 20000000000000000000000000000001 ", action);

        Assert.Equal("20000000000000000000000000000001", dispatchedJobId);
        Assert.Same(action, dispatchedAction);
        Assert.False(dispatchedAction!.IsEnabled);
    }

    [Fact]
    public void Dispatcher_rejects_duplicate_routes()
    {
        JobActionRoute first = new JobActionRoute(
            JobActionKind.PrepareSuggestedTool,
            (_, _) => { });
        JobActionRoute second = new JobActionRoute(
            JobActionKind.PrepareSuggestedTool,
            (_, _) => { });

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new JobActionDispatcher(new[] { first, second }));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispatcher_rejects_unregistered_action_kind()
    {
        JobActionDispatcher dispatcher = new JobActionDispatcher(
            Array.Empty<JobActionRoute>());
        JobActionViewModel action = new JobActionViewModel(
            JobActionKind.PrepareSuggestedTool,
            "Equip suggested tool",
            isEnabled: true);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => dispatcher.Dispatch("20000000000000000000000000000001", action));

        Assert.Contains(
            JobActionKind.PrepareSuggestedTool.ToString(),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Dispatcher_validates_job_id_before_invoking_handler()
    {
        int callCount = 0;
        JobActionDispatcher dispatcher = new JobActionDispatcher(new[]
        {
            new JobActionRoute(
                JobActionKind.PrepareSuggestedTool,
                (_, _) => callCount++),
        });
        JobActionViewModel action = new JobActionViewModel(
            JobActionKind.PrepareSuggestedTool,
            "Equip suggested tool",
            isEnabled: true);

        Assert.Throws<ArgumentException>(() => dispatcher.Dispatch(" ", action));
        Assert.Equal(0, callCount);
    }
}
}
